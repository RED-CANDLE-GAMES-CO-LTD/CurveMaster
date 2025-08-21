using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CurveMaster.Components
{
    /// <summary>
    /// 曲線形狀維持元件 - 維持控制點之間的相對關係
    /// </summary>
    [RequireComponent(typeof(SplineManager))]
    public class SplineShapeKeeper : MonoBehaviour
    {
        public enum ShapeMode
        {
            Rigid,           // 剛體模式 - 保持固定的相對位置
            Elastic          // 彈性模式 - 允許一定程度的變形
        }
        
        public enum ShapePreservation
        {
            Scaled,          // 等比例縮放（原始方式）
            Absolute,        // 保持絕對偏移大小
            ElasticBend,     // 彈性補償（壓縮時增大彎曲）
            ArcLength        // 保持弧長（維持曲線總長度）
        }
        
        [Header("形狀維持設定")]
        [SerializeField] private ShapeMode shapeMode = ShapeMode.Rigid;
        [SerializeField] private ShapePreservation preservationMode = ShapePreservation.Absolute;
        
        [Header("形狀參數")]
        [SerializeField] private float elasticity = 0.5f;  // 彈性係數
        [SerializeField] private float smoothness = 0.95f;  // 平滑度
        [SerializeField] private float shapeFidelity = 1.0f;  // 形狀保真度 (0-1)
        [SerializeField] private float compressionResponse = 1.5f;  // 壓縮響應係數 (>1 增大彎曲)
        
        [Header("進階設定")]
        [Tooltip("停用自動偵測以手動設定控制點角色")]
        [SerializeField] private bool manualMode = false;
        [SerializeField] private float updateRate = 60f;
        
        [Header("Initialization")]
        [Tooltip("Instantly snap to stable shape when GameObject is enabled")]
        [SerializeField] private bool snapOnEnable = true;
        
        [Header("控制點角色 (自動偵測中)")]
        [SerializeField, HideInInspector] 
        private List<ControlPointConfig> controlPointConfigs = new List<ControlPointConfig>();
        
        // 內部變數
        private SplineManager splineManager;
        private float lastUpdateTime;
        private List<Transform> controlPoints;
        private Dictionary<Transform, ControlPointRole> pointRoles;
        private bool justEnabled;
        
        // 相對位置記錄
        private Dictionary<Transform, RelativePositionData> relativePositions;
        private bool hasRecordedInitialShape = false;
        private Transform firstFixedPoint;  // 第一個固定點
        private Transform lastFixedPoint;   // 最後一個固定點
        private Vector3 originalStartToEnd; // 原始首尾向量
        
        [System.Serializable]
        public class ControlPointConfig
        {
            public Transform point;
            public ControlPointRole role = ControlPointRole.Auto;
            public float weight = 1f;
            
            public ControlPointConfig(Transform p, ControlPointRole r, float w = 1f)
            {
                point = p;
                role = r;
                weight = w;
            }
        }
        
        public enum ControlPointRole
        {
            Fixed,      // 固定點（由追蹤器控制）
            Auto,       // 自動調整點（跟隨相對位置）
            Manual      // 手動控制點（不調整）
        }
        
        // 相對位置資料
        private class RelativePositionData
        {
            // 形狀參數
            public float curveParameter;       // 在曲線上的參數位置 (0-1)
            public Vector3 perpOffset;         // 垂直於首尾連線的偏移
            public float bendAmount;           // 彎曲程度
            public Vector3 tangentDirection;   // 切線方向
            
            // 原始資料
            public Vector3 localOffset;        // 相對於父物件的局部偏移
            public Vector3 relativeToFirst;    // 相對於第一個固定點的偏移
            public Vector3 relativeToLast;     // 相對於最後一個固定點的偏移
            public float distanceToNext;       // 到下一個點的距離
            public float distanceToPrev;       // 到上一個點的距離
            public Quaternion localRotation;   // 局部旋轉
            
            public RelativePositionData()
            {
                curveParameter = 0f;
                perpOffset = Vector3.zero;
                bendAmount = 0f;
                tangentDirection = Vector3.forward;
                localOffset = Vector3.zero;
                relativeToFirst = Vector3.zero;
                relativeToLast = Vector3.zero;
                distanceToNext = 0f;
                distanceToPrev = 0f;
                localRotation = Quaternion.identity;
            }
        }
        
        private void Awake()
        {
            splineManager = GetComponent<SplineManager>();
            pointRoles = new Dictionary<Transform, ControlPointRole>();
            relativePositions = new Dictionary<Transform, RelativePositionData>();
        }
        
        private void Start()
        {
            if (!manualMode)
            {
                DetectAndConfigureTrackers();
            }
            RefreshControlPoints();
            RecordInitialShape();
        }
        
        private void OnEnable()
        {
            // 重新記錄形狀，避免破壾現有曲線
            if (!hasRecordedInitialShape)
            {
                RecordInitialShape();
            }
            
            // Mark as just enabled for instant snap
            justEnabled = snapOnEnable;
        }
        
        private void LateUpdate()
        {
            if (!hasRecordedInitialShape)
                return;
            
            // 限制更新頻率
            if (Time.time - lastUpdateTime < 1f / updateRate)
                return;
            
            lastUpdateTime = Time.time;
            
            // On first frame after enable, force instant update
            if (justEnabled && snapOnEnable)
            {
                // Force all trackers to update first
                foreach (var point in controlPoints)
                {
                    if (point == null) continue;
                    var tracker = point.GetComponent<SplineTargetTracker>();
                    if (tracker != null && tracker.EnableTracking && tracker.TargetObject != null)
                    {
                        // Force tracker to its target position immediately
                        tracker.ForceToTarget();
                    }
                }
                
                // Now update shape instantly
                UpdateShapeInstant();
                justEnabled = false;
            }
            else
            {
                UpdateShape();
            }
            
            // Clear flag after first update
            if (justEnabled)
            {
                justEnabled = false;
            }
        }
        
        /// <summary>
        /// 記錄初始形狀
        /// </summary>
        public void RecordInitialShape()
        {
            if (controlPoints == null || controlPoints.Count < 2)
            {
                RefreshControlPoints();
            }
            
            if (controlPoints == null || controlPoints.Count < 2)
                return;
            
            relativePositions.Clear();
            
            // 找出第一個和最後一個固定點
            firstFixedPoint = null;
            lastFixedPoint = null;
            
            for (int i = 0; i < controlPoints.Count; i++)
            {
                if (controlPoints[i] == null) continue;
                
                ControlPointRole role = GetPointRole(controlPoints[i]);
                if (role == ControlPointRole.Fixed)
                {
                    if (firstFixedPoint == null)
                        firstFixedPoint = controlPoints[i];
                    lastFixedPoint = controlPoints[i];
                }
            }
            
            // 如果沒有固定點，使用首尾點
            if (firstFixedPoint == null)
            {
                firstFixedPoint = controlPoints[0];
                lastFixedPoint = controlPoints[controlPoints.Count - 1];
            }
            
            // 記錄原始首尾向量
            if (firstFixedPoint != null && lastFixedPoint != null)
            {
                originalStartToEnd = lastFixedPoint.position - firstFixedPoint.position;
            }
            
            // 記錄每個點的相對位置
            for (int i = 0; i < controlPoints.Count; i++)
            {
                var point = controlPoints[i];
                if (point == null) continue;
                
                var data = new RelativePositionData();
                
                // 記錄曲線參數（在首尾之間的位置）
                if (firstFixedPoint != null && lastFixedPoint != null && firstFixedPoint != lastFixedPoint)
                {
                    // 計算點在首尾連線上的投影位置
                    Vector3 startToEnd = lastFixedPoint.position - firstFixedPoint.position;
                    Vector3 startToPoint = point.position - firstFixedPoint.position;
                    
                    if (startToEnd.magnitude > 0.001f)
                    {
                        // 計算參數 t (0-1)
                        data.curveParameter = Vector3.Dot(startToPoint, startToEnd.normalized) / startToEnd.magnitude;
                        data.curveParameter = Mathf.Clamp01(data.curveParameter);
                        
                        // 計算垂直偏移
                        Vector3 projectedPoint = firstFixedPoint.position + startToEnd * data.curveParameter;
                        data.perpOffset = point.position - projectedPoint;
                        
                        // 計算彎曲程度
                        data.bendAmount = data.perpOffset.magnitude;
                        
                        // 記錄切線方向（用於保持曲線平滑）
                        if (i > 0 && i < controlPoints.Count - 1)
                        {
                            Vector3 toPrev = controlPoints[i - 1].position - point.position;
                            Vector3 toNext = controlPoints[i + 1].position - point.position;
                            data.tangentDirection = (toNext - toPrev).normalized;
                        }
                    }
                }
                
                // 記錄相對於父物件的局部位置
                data.localOffset = transform.InverseTransformPoint(point.position);
                
                // 記錄相對於首尾點的位置
                if (firstFixedPoint != null)
                {
                    data.relativeToFirst = point.position - firstFixedPoint.position;
                }
                
                if (lastFixedPoint != null)
                {
                    data.relativeToLast = point.position - lastFixedPoint.position;
                }
                
                // 記錄到相鄰點的距離
                if (i > 0 && controlPoints[i - 1] != null)
                {
                    data.distanceToPrev = Vector3.Distance(point.position, controlPoints[i - 1].position);
                }
                
                if (i < controlPoints.Count - 1 && controlPoints[i + 1] != null)
                {
                    data.distanceToNext = Vector3.Distance(point.position, controlPoints[i + 1].position);
                }
                
                // 記錄局部旋轉
                data.localRotation = Quaternion.Inverse(transform.rotation) * point.rotation;
                
                relativePositions[point] = data;
            }
            
            hasRecordedInitialShape = true;
        }
        
        /// <summary>
        /// 自動偵測追蹤器並設定角色
        /// </summary>
        public void DetectAndConfigureTrackers()
        {
            controlPointConfigs.Clear();
            
            var allControlPoints = splineManager.ControlPointTransforms;
            
            foreach (var point in allControlPoints)
            {
                if (point == null) continue;
                
                var tracker = point.GetComponent<SplineTargetTracker>();
                ControlPointRole role = ControlPointRole.Auto;
                
                if (tracker != null && tracker.EnableTracking)
                {
                    role = ControlPointRole.Fixed;
                }
                
                controlPointConfigs.Add(new ControlPointConfig(point, role));
            }
        }
        
        /// <summary>
        /// 更新控制點清單
        /// </summary>
        private void RefreshControlPoints()
        {
            controlPoints = splineManager.ControlPointTransforms;
            
            pointRoles.Clear();
            foreach (var config in controlPointConfigs)
            {
                if (config.point != null)
                {
                    pointRoles[config.point] = config.role;
                }
            }
        }
        
        /// <summary>
        /// 取得控制點角色
        /// </summary>
        private ControlPointRole GetPointRole(Transform point)
        {
            if (pointRoles.ContainsKey(point))
            {
                return pointRoles[point];
            }
            return ControlPointRole.Auto;
        }
        
        /// <summary>
        /// 更新曲線形狀
        /// </summary>
        private void UpdateShape()
        {
            if (controlPoints == null || controlPoints.Count < 2)
                return;
            
            switch (shapeMode)
            {
                case ShapeMode.Rigid:
                    UpdateRigidShape();
                    break;
                case ShapeMode.Elastic:
                    UpdateElasticShape();
                    break;
            }
            
            // Clear just enabled flag after first update
            if (justEnabled)
            {
                justEnabled = false;
            }
        }
        
        /// <summary>
        /// Instantly update shape without smoothing (for initialization)
        /// </summary>
        private void UpdateShapeInstant()
        {
            if (controlPoints == null || controlPoints.Count < 2)
                return;
            
            // Temporarily set justEnabled to force instant positioning
            bool wasJustEnabled = justEnabled;
            justEnabled = true;
            
            // Update shape based on mode
            switch (shapeMode)
            {
                case ShapeMode.Rigid:
                    UpdateRigidShape();
                    break;
                case ShapeMode.Elastic:
                    UpdateElasticShape();
                    break;
            }
            
            // Restore justEnabled state
            justEnabled = wasJustEnabled;
        }
        
        /// <summary>
        /// 剛體模式 - 保持固定的相對位置（改進版）
        /// </summary>
        private void UpdateRigidShape()
        {
            // 找出所有固定點和自動點
            List<Transform> fixedPoints = new List<Transform>();
            List<Transform> autoPoints = new List<Transform>();
            
            foreach (var point in controlPoints)
            {
                if (point == null) continue;
                
                ControlPointRole role = GetPointRole(point);
                if (role == ControlPointRole.Fixed)
                {
                    fixedPoints.Add(point);
                }
                else if (role == ControlPointRole.Auto)
                {
                    autoPoints.Add(point);
                }
            }
            
            // 如果沒有需要調整的點，直接返回
            if (autoPoints.Count == 0)
                return;
            
            // 根據固定點數量選擇不同策略
            if (fixedPoints.Count >= 2)
            {
                // 有兩個或以上固定點：使用雙端點插值
                UpdateWithMultipleFixedPoints(fixedPoints, autoPoints);
            }
            else if (fixedPoints.Count == 1)
            {
                // 只有一個固定點：使用單點偏移
                UpdateWithSingleFixedPoint(fixedPoints[0], autoPoints);
            }
            // 沒有固定點的情況下不做任何調整
        }
        
        /// <summary>
        /// 使用多個固定點更新形狀
        /// </summary>
        private void UpdateWithMultipleFixedPoints(List<Transform> fixedPoints, List<Transform> autoPoints)
        {
            // 找出首尾固定點
            Transform currentFirst = fixedPoints[0];
            Transform currentLast = fixedPoints[fixedPoints.Count - 1];
            
            // 計算新的首尾向量
            Vector3 currentStartToEnd = currentLast.position - currentFirst.position;
            
            // 計算變換
            Quaternion rotation = Quaternion.identity;
            float stretchFactor = 1f;
            
            if (originalStartToEnd.magnitude > 0.001f && currentStartToEnd.magnitude > 0.001f)
            {
                // 計算旋轉
                rotation = Quaternion.FromToRotation(originalStartToEnd, currentStartToEnd);
                
                // 計算拉伸/壓縮係數
                stretchFactor = currentStartToEnd.magnitude / originalStartToEnd.magnitude;
            }
            
            // 更新每個自動點
            foreach (var autoPoint in autoPoints)
            {
                if (!relativePositions.ContainsKey(autoPoint))
                    continue;
                
                var data = relativePositions[autoPoint];
                Vector3 targetPos;
                
                // 使用參數化位置重建點
                if (currentStartToEnd.magnitude > 0.001f)
                {
                    // 在新的首尾連線上找到對應位置
                    Vector3 basePos = currentFirst.position + currentStartToEnd * data.curveParameter;
                    
                    // 根據不同的保持模式計算垂直偏移
                    Vector3 transformedOffset = Vector3.zero;
                    
                    switch (preservationMode)
                    {
                        case ShapePreservation.Scaled:
                            // 原始方式：等比例縮放
                            transformedOffset = rotation * data.perpOffset * stretchFactor;
                            break;
                            
                        case ShapePreservation.Absolute:
                            // 保持絕對偏移大小
                            transformedOffset = rotation * data.perpOffset.normalized * data.bendAmount;
                            break;
                            
                        case ShapePreservation.ElasticBend:
                            // 彈性補償：壓縮時增大彎曲
                            float bendingCompensation = 1f;
                            if (stretchFactor < 1f) // 壓縮狀態
                            {
                                // 使用指數曲線或平方根來計算補償
                                bendingCompensation = Mathf.Pow(1f / stretchFactor, compressionResponse);
                            }
                            else if (stretchFactor > 1f) // 拉伸狀態
                            {
                                // 拉伸時稍微減少彎曲，但不要太多
                                bendingCompensation = Mathf.Pow(stretchFactor, 0.5f / compressionResponse);
                            }
                            transformedOffset = rotation * data.perpOffset * bendingCompensation;
                            break;
                            
                        case ShapePreservation.ArcLength:
                            // 嘗試保持弧長：根據壓縮程度增加彎曲
                            if (stretchFactor < 1f)
                            {
                                // 計算需要的額外彎曲來補償長度
                                float arcCompensation = 1f + (1f - stretchFactor) * 2f;
                                transformedOffset = rotation * data.perpOffset * arcCompensation;
                            }
                            else
                            {
                                // 拉伸時保持原始偏移
                                transformedOffset = rotation * data.perpOffset;
                            }
                            break;
                    }
                    
                    // 根據形狀保真度混合偏移
                    transformedOffset *= shapeFidelity;
                    
                    targetPos = basePos + transformedOffset;
                }
                else
                {
                    // 首尾重合的特殊情況
                    targetPos = currentFirst.position + data.relativeToFirst * stretchFactor;
                }
                
                // 平滑移動到目標位置（除非剛啟用）
                if (justEnabled)
                {
                    autoPoint.position = targetPos;
                }
                else
                {
                    autoPoint.position = Vector3.Lerp(autoPoint.position, targetPos, smoothness);
                }
            }
        }
        
        /// <summary>
        /// 使用單個固定點更新形狀
        /// </summary>
        private void UpdateWithSingleFixedPoint(Transform fixedPoint, List<Transform> autoPoints)
        {
            if (!relativePositions.ContainsKey(fixedPoint))
                return;
            
            var fixedData = relativePositions[fixedPoint];
            Vector3 movement = fixedPoint.position - transform.TransformPoint(fixedData.localOffset);
            
            foreach (var autoPoint in autoPoints)
            {
                if (!relativePositions.ContainsKey(autoPoint))
                    continue;
                
                var data = relativePositions[autoPoint];
                
                // 簡單地跟隨固定點移動
                Vector3 targetPos = transform.TransformPoint(data.localOffset) + movement;
                
                // Apply position with or without smoothing
                if (justEnabled)
                {
                    autoPoint.position = targetPos;
                }
                else
                {
                    autoPoint.position = Vector3.Lerp(autoPoint.position, targetPos, smoothness);
                }
            }
        }
        
        /// <summary>
        /// 彈性模式 - 允許一定程度的變形
        /// </summary>
        private void UpdateElasticShape()
        {
            for (int i = 0; i < controlPoints.Count; i++)
            {
                var point = controlPoints[i];
                if (point == null) continue;
                
                ControlPointRole role = GetPointRole(point);
                if (role != ControlPointRole.Auto)
                    continue;
                
                if (!relativePositions.ContainsKey(point))
                    continue;
                
                var data = relativePositions[point];
                Vector3 force = Vector3.zero;
                int forceCount = 0;
                
                // 計算來自相鄰點的彈性力
                if (i > 0 && controlPoints[i - 1] != null)
                {
                    Vector3 toPrev = controlPoints[i - 1].position - point.position;
                    float currentDist = toPrev.magnitude;
                    float targetDist = data.distanceToPrev;
                    
                    if (currentDist > 0 && targetDist > 0)
                    {
                        float stretch = (currentDist - targetDist) / targetDist;
                        force += toPrev.normalized * stretch * elasticity;
                        forceCount++;
                    }
                }
                
                if (i < controlPoints.Count - 1 && controlPoints[i + 1] != null)
                {
                    Vector3 toNext = controlPoints[i + 1].position - point.position;
                    float currentDist = toNext.magnitude;
                    float targetDist = data.distanceToNext;
                    
                    if (currentDist > 0 && targetDist > 0)
                    {
                        float stretch = (currentDist - targetDist) / targetDist;
                        force += toNext.normalized * stretch * elasticity;
                        forceCount++;
                    }
                }
                
                // 應用彈性力
                if (forceCount > 0)
                {
                    force /= forceCount;
                    Vector3 targetPos = point.position + force;
                    
                    // Apply position with or without smoothing
                    if (justEnabled)
                    {
                        point.position = targetPos;
                    }
                    else
                    {
                        point.position = Vector3.Lerp(point.position, targetPos, smoothness * Time.deltaTime * 10f);
                    }
                }
            }
        }
        
        /// <summary>
        /// 設定控制點角色
        /// </summary>
        public void SetControlPointRole(Transform point, ControlPointRole role, float weight = 1f)
        {
            var config = controlPointConfigs.Find(c => c.point == point);
            if (config != null)
            {
                config.role = role;
                config.weight = weight;
            }
            else
            {
                controlPointConfigs.Add(new ControlPointConfig(point, role, weight));
            }
            
            RefreshControlPoints();
        }
        
        /// <summary>
        /// 重設形狀記錄
        /// </summary>
        public void ResetShapeRecord()
        {
            hasRecordedInitialShape = false;
            RecordInitialShape();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (controlPoints == null)
                return;
            
            // 顯示不同角色的控制點
            foreach (var config in controlPointConfigs)
            {
                if (config.point == null) continue;
                
                switch (config.role)
                {
                    case ControlPointRole.Fixed:
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(config.point.position, Vector3.one * 0.2f);
                        break;
                    case ControlPointRole.Auto:
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(config.point.position, 0.15f);
                        break;
                    case ControlPointRole.Manual:
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireSphere(config.point.position, 0.1f);
                        break;
                }
            }
            
            // 顯示首尾固定點
            if (firstFixedPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(firstFixedPoint.position, 0.25f);
            }
            
            if (lastFixedPoint != null && lastFixedPoint != firstFixedPoint)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(lastFixedPoint.position, 0.25f);
                
                // 繪製首尾連線
                if (firstFixedPoint != null)
                {
                    Gizmos.color = new Color(1, 1, 0, 0.3f);
                    Gizmos.DrawLine(firstFixedPoint.position, lastFixedPoint.position);
                }
            }
        }
    }
}