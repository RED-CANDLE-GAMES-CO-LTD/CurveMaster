using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CurveMaster.Components
{
    /// <summary>
    /// 曲線追蹤管理器 - 統一管理所有追蹤行為
    /// </summary>
    [RequireComponent(typeof(SplineManager))]
    public class SplineTrackerManager : MonoBehaviour
    {
        [Header("追蹤設定")]
        [SerializeField] private bool enableGlobalTracking = true;
        [SerializeField] private float globalSpeedMultiplier = 1f;
        
        [Header("追蹤配置")]
        [SerializeField] private List<TrackingSetup> trackingSetups = new List<TrackingSetup>();
        
        [Header("形狀維持")]
        [SerializeField] private bool autoEnableShapeKeeper = true;
        [SerializeField] private SplineShapeKeeper.ShapeMode defaultShapeMode = SplineShapeKeeper.ShapeMode.Rigid;
        
        [Header("偵錯")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool visualizeConnections = true;
        
        private SplineManager splineManager;
        private SplineShapeKeeper shapeKeeper;
        private Dictionary<Transform, SplineTargetTracker> trackers;
        
        [System.Serializable]
        public class TrackingSetup
        {
            public string name = "新追蹤設定";
            public Transform controlPoint;
            public Transform targetObject;
            public SplineTargetTracker.TrackingMode trackingMode = SplineTargetTracker.TrackingMode.Smooth;
            public float trackingSpeed = 5f;
            public Vector3 offset = Vector3.zero;
            public bool enabled = true;
            
            public TrackingSetup() { }
            
            public TrackingSetup(Transform point, Transform target)
            {
                controlPoint = point;
                targetObject = target;
                name = $"{(point != null ? point.name : "?")} → {(target != null ? target.name : "?")}";
            }
        }
        
        private void Awake()
        {
            splineManager = GetComponent<SplineManager>();
            shapeKeeper = GetComponent<SplineShapeKeeper>();
            trackers = new Dictionary<Transform, SplineTargetTracker>();
        }
        
        private void Start()
        {
            InitializeTrackers();
            
            if (autoEnableShapeKeeper && shapeKeeper == null)
            {
                SetupShapeKeeper();
            }
        }
        
        private void OnEnable()
        {
            ApplyAllSetups();
        }
        
        /// <summary>
        /// 初始化所有追蹤器
        /// </summary>
        private void InitializeTrackers()
        {
            // 清空追蹤器字典
            trackers.Clear();
            
            // 取得所有控制點
            var controlPoints = splineManager.ControlPointTransforms;
            
            foreach (var point in controlPoints)
            {
                if (point == null) continue;
                
                var tracker = point.GetComponent<SplineTargetTracker>();
                if (tracker == null)
                {
                    tracker = point.gameObject.AddComponent<SplineTargetTracker>();
                    tracker.enabled = false; // 預設關閉
                }
                
                trackers[point] = tracker;
            }
        }
        
        /// <summary>
        /// 設定形狀維持器
        /// </summary>
        private void SetupShapeKeeper()
        {
            shapeKeeper = gameObject.AddComponent<SplineShapeKeeper>();
            shapeKeeper.enabled = autoEnableShapeKeeper;
            
            // 設定預設形狀模式
            var shapeKeeperType = shapeKeeper.GetType();
            var shapeModeField = shapeKeeperType.GetField("shapeMode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (shapeModeField != null)
            {
                shapeModeField.SetValue(shapeKeeper, defaultShapeMode);
            }
            
            // 自動偵測追蹤器
            shapeKeeper.DetectAndConfigureTrackers();
        }
        
        /// <summary>
        /// 套用所有追蹤設定
        /// </summary>
        public void ApplyAllSetups()
        {
            if (!enableGlobalTracking)
            {
                DisableAllTrackers();
                return;
            }
            
            foreach (var setup in trackingSetups)
            {
                ApplySetup(setup);
            }
            
            // 更新形狀維持器
            if (shapeKeeper != null)
            {
                shapeKeeper.DetectAndConfigureTrackers();
            }
        }
        
        /// <summary>
        /// 套用單一追蹤設定
        /// </summary>
        private void ApplySetup(TrackingSetup setup)
        {
            if (setup.controlPoint == null || !setup.enabled)
                return;
            
            SplineTargetTracker tracker;
            if (!trackers.TryGetValue(setup.controlPoint, out tracker))
            {
                tracker = setup.controlPoint.GetComponent<SplineTargetTracker>();
                if (tracker == null)
                {
                    tracker = setup.controlPoint.gameObject.AddComponent<SplineTargetTracker>();
                }
                trackers[setup.controlPoint] = tracker;
            }
            
            if (tracker != null)
            {
                tracker.TargetObject = setup.targetObject;
                tracker.TrackingSpeed = setup.trackingSpeed * globalSpeedMultiplier;
                tracker.SetupTracking(setup.targetObject, setup.trackingMode, setup.trackingSpeed * globalSpeedMultiplier);
                tracker.enabled = setup.targetObject != null && setup.enabled && enableGlobalTracking;
                
                // 設定偏移
                var trackerType = tracker.GetType();
                var offsetField = trackerType.GetField("targetOffset", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (offsetField != null)
                {
                    offsetField.SetValue(tracker, setup.offset);
                }
            }
        }
        
        /// <summary>
        /// 停用所有追蹤器
        /// </summary>
        private void DisableAllTrackers()
        {
            foreach (var tracker in trackers.Values)
            {
                if (tracker != null)
                {
                    tracker.enabled = false;
                }
            }
        }
        
        /// <summary>
        /// 新增追蹤設定
        /// </summary>
        public void AddTrackingSetup(Transform controlPoint, Transform target)
        {
            var setup = new TrackingSetup(controlPoint, target);
            trackingSetups.Add(setup);
            ApplySetup(setup);
        }
        
        /// <summary>
        /// 移除追蹤設定
        /// </summary>
        public void RemoveTrackingSetup(TrackingSetup setup)
        {
            if (trackingSetups.Remove(setup))
            {
                // 停用對應的追蹤器
                if (setup.controlPoint != null && trackers.ContainsKey(setup.controlPoint))
                {
                    var tracker = trackers[setup.controlPoint];
                    if (tracker != null)
                    {
                        tracker.enabled = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// 清除所有追蹤設定
        /// </summary>
        public void ClearAllSetups()
        {
            trackingSetups.Clear();
            DisableAllTrackers();
        }
        
        /// <summary>
        /// 自動配對控制點和目標
        /// </summary>
        public void AutoMatchTargets(GameObject[] targets)
        {
            if (targets == null || targets.Length == 0)
                return;
            
            var controlPoints = splineManager.ControlPointTransforms;
            if (controlPoints.Count == 0)
                return;
            
            // 清除現有設定
            trackingSetups.Clear();
            
            // 根據不同策略進行配對
            if (targets.Length == 2 && controlPoints.Count > 2)
            {
                // 兩個目標：第一個和最後一個控制點
                AddTrackingSetup(controlPoints[0], targets[0].transform);
                AddTrackingSetup(controlPoints[controlPoints.Count - 1], targets[1].transform);
            }
            else if (targets.Length == controlPoints.Count)
            {
                // 一對一配對
                for (int i = 0; i < targets.Length; i++)
                {
                    AddTrackingSetup(controlPoints[i], targets[i].transform);
                }
            }
            else
            {
                // 平均分配
                float step = (float)(controlPoints.Count - 1) / (targets.Length - 1);
                for (int i = 0; i < targets.Length; i++)
                {
                    int pointIndex = Mathf.RoundToInt(i * step);
                    pointIndex = Mathf.Clamp(pointIndex, 0, controlPoints.Count - 1);
                    AddTrackingSetup(controlPoints[pointIndex], targets[i].transform);
                }
            }
            
            ApplyAllSetups();
        }
        
        /// <summary>
        /// 取得追蹤狀態資訊
        /// </summary>
        public string GetTrackingStatus()
        {
            int activeCount = trackingSetups.Count(s => s.enabled && s.targetObject != null);
            int totalCount = trackingSetups.Count;
            
            return $"追蹤狀態: {activeCount}/{totalCount} 啟用\n" +
                   $"全域速度倍率: {globalSpeedMultiplier:F2}\n" +
                   $"形狀維持: {(shapeKeeper != null && shapeKeeper.enabled ? "啟用" : "停用")}";
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!visualizeConnections)
                return;
            
            // 繪製追蹤連線
            foreach (var setup in trackingSetups)
            {
                if (setup.controlPoint == null || setup.targetObject == null || !setup.enabled)
                    continue;
                
                Gizmos.color = new Color(0, 1, 1, 0.5f);
                Gizmos.DrawLine(setup.controlPoint.position, setup.targetObject.position);
                
                // 繪製偏移位置
                if (setup.offset != Vector3.zero)
                {
                    Vector3 offsetPos = setup.targetObject.position + setup.offset;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(offsetPos, 0.05f);
                    Gizmos.DrawLine(setup.targetObject.position, offsetPos);
                }
            }
            
            // 顯示偵錯資訊
            if (showDebugInfo)
            {
                #if UNITY_EDITOR
                Vector3 labelPos = transform.position + Vector3.up * 2f;
                UnityEditor.Handles.Label(labelPos, GetTrackingStatus());
                #endif
            }
        }
        
        /// <summary>
        /// 批次設定追蹤速度
        /// </summary>
        public void SetAllTrackingSpeeds(float speed)
        {
            foreach (var setup in trackingSetups)
            {
                setup.trackingSpeed = speed;
            }
            ApplyAllSetups();
        }
        
        /// <summary>
        /// 批次設定追蹤模式
        /// </summary>
        public void SetAllTrackingModes(SplineTargetTracker.TrackingMode mode)
        {
            foreach (var setup in trackingSetups)
            {
                setup.trackingMode = mode;
            }
            ApplyAllSetups();
        }
    }
}