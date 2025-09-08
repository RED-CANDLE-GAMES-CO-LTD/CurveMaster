using UnityEngine;
using System.Collections.Generic;
using CurveMaster.Core;
using CurveMaster.Splines;

namespace CurveMaster.Components
{
    /// <summary>
    /// 曲線管理器元件
    /// </summary>
    [ExecuteInEditMode]
    public class SplineManager : MonoBehaviour
    {
        [SerializeField] private SplineType splineType = SplineType.CatmullRom;
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private Color splineColor = Color.green;
        [SerializeField] private int resolution = 50;
        [SerializeField] private bool autoDetectControlPoints = true;
        [SerializeField] private bool autoUpdateCursors = true;
        
        private List<Transform> controlPointTransforms = new List<Transform>();
        private ISpline currentSpline;
        private Vector3[] cachedControlPoints;
        private SplineType lastSplineType;

        public ISpline Spline => currentSpline;
        public SplineType CurrentType => splineType;
        public int Resolution => resolution;
        public Color SplineColor => splineColor;
        public List<Transform> ControlPointTransforms => controlPointTransforms;
        public bool AutoDetectControlPoints => autoDetectControlPoints;
        public bool AutoUpdateCursors => autoUpdateCursors;

        private void Awake()
        {
            // 立即初始化曲線
            ForceInitializeSpline();
        }

        private void OnEnable()
        {
            // 確保曲線已初始化
            if (currentSpline == null)
            {
                ForceInitializeSpline();
            }
        }

        private void Update()
        {
            if (autoUpdate)
            {
                if (autoDetectControlPoints)
                {
                    RefreshControlPointsList();
                }
                UpdateSpline();
            }
        }

        private void InitializeSpline()
        {
            // 確保曲線實例存在
            if (currentSpline == null)
            {
                CreateSpline(splineType);
            }
            
            if (autoDetectControlPoints)
            {
                RefreshControlPointsList();
            }
            else
            {
                UpdateControlPoints();
            }
        }

        private void UpdateSpline()
        {
            // 確保有曲線實例
            if (currentSpline == null)
            {
                InitializeSpline();
                return;
            }

            if (lastSplineType != splineType)
            {
                SwitchSplineType(splineType);
                lastSplineType = splineType;
            }

            if (HasControlPointsChanged())
            {
                UpdateControlPoints();
            }
        }

        private void CreateSpline(SplineType type)
        {
            switch (type)
            {
                case SplineType.BSpline:
                    currentSpline = new BSplineInterpolating();
                    break;
                case SplineType.CatmullRom:
                    currentSpline = new CatmullRomSpline();
                    break;
                case SplineType.CubicSpline:
                    currentSpline = new CubicSpline();
                    break;
                case SplineType.BezierSpline:
                    currentSpline = new BezierSpline();
                    break;
            }
            lastSplineType = type;
        }

        public void SwitchSplineType(SplineType newType)
        {
            Vector3[] points = currentSpline?.GetControlPoints();
            splineType = newType;
            CreateSpline(newType);
            
            if (points != null)
            {
                currentSpline.SetControlPoints(points);
            }
        }

        private bool HasControlPointsChanged()
        {
            if (controlPointTransforms.Count == 0)
                return false;

            if (cachedControlPoints == null || cachedControlPoints.Length != controlPointTransforms.Count)
                return true;

            for (int i = 0; i < controlPointTransforms.Count; i++)
            {
                if (controlPointTransforms[i] == null)
                    continue;
                    
                Vector3 worldPoint = transform.InverseTransformPoint(controlPointTransforms[i].position);
                if (cachedControlPoints[i] != worldPoint)
                    return true;
            }

            return false;
        }

        private void UpdateControlPoints()
        {
            List<Vector3> points = new List<Vector3>();
            
            foreach (Transform t in controlPointTransforms)
            {
                if (t != null)
                {
                    points.Add(transform.InverseTransformPoint(t.position));
                }
            }

            cachedControlPoints = points.ToArray();
            currentSpline?.SetControlPoints(cachedControlPoints);
            
            // 如果啟用了自動更新 Cursor，通知所有 Cursor 更新
            if (autoUpdateCursors)
            {
                UpdateAllCursors();
            }
        }
        
        /// <summary>
        /// 更新所有相關的 SplineCursor
        /// </summary>
        private void UpdateAllCursors()
        {
            // 取得所有子物件的 SplineCursor
            SplineCursor[] cursors = GetComponentsInChildren<SplineCursor>();
            foreach (var cursor in cursors)
            {
                if (cursor != null && cursor.enabled)
                {
                    cursor.UpdateTransform();
                }
            }
            
            // 編輯器模式下也支援更新
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // 在編輯器中，只更新子物件的 Cursor
                foreach (var cursor in cursors)
                {
                    if (cursor != null)
                    {
                        UnityEditor.EditorUtility.SetDirty(cursor);
                    }
                }
            }
            #endif
            
            // 執行時檢查場景中其他引用此 SplineManager 的 Cursor
            if (Application.isPlaying)
            {
                SplineCursor[] allCursors = FindObjectsOfType<SplineCursor>();
                foreach (var cursor in allCursors)
                {
                    if (cursor != null && cursor.enabled && !System.Array.Exists(cursors, c => c == cursor))
                    {
                        var cursorSplineManager = cursor.GetComponent<SplineManager>();
                        if (cursorSplineManager == null)
                        {
                            cursorSplineManager = cursor.GetComponentInParent<SplineManager>();
                        }
                        
                        if (cursorSplineManager == this)
                        {
                            cursor.UpdateTransform();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 自動偵測並更新控制點清單
        /// </summary>
        public void RefreshControlPointsList()
        {
            if (!autoDetectControlPoints)
                return;
                
            // 取得所有子物件的控制點元件
            SplineControlPoint[] childControlPoints = GetComponentsInChildren<SplineControlPoint>();
            
            // 根據子物件順序排序
            System.Array.Sort(childControlPoints, (a, b) => 
            {
                int indexA = a.transform.GetSiblingIndex();
                int indexB = b.transform.GetSiblingIndex();
                return indexA.CompareTo(indexB);
            });
            
            // 更新清單
            controlPointTransforms.Clear();
            foreach (var cp in childControlPoints)
            {
                if (cp != null && cp.transform != null)
                {
                    controlPointTransforms.Add(cp.transform);
                    cp.SetIndex(controlPointTransforms.Count - 1);
                }
            }
            
            // 如果清單有變化，更新控制點
            if (HasControlPointsChanged())
            {
                UpdateControlPoints();
            }
        }
        
        /// <summary>
        /// 通知控制點變更（由 SplineControlPoint 呼叫）
        /// </summary>
        public void NotifyControlPointsChanged()
        {
            if (autoDetectControlPoints)
            {
                RefreshControlPointsList();
            }
        }

        public void AddControlPoint(Transform point)
        {
            if (!autoDetectControlPoints && point != null && !controlPointTransforms.Contains(point))
            {
                controlPointTransforms.Add(point);
                UpdateControlPoints();
            }
            else if (autoDetectControlPoints)
            {
                RefreshControlPointsList();
            }
        }

        public void RemoveControlPoint(Transform point)
        {
            if (!autoDetectControlPoints && controlPointTransforms.Remove(point))
            {
                UpdateControlPoints();
            }
            else if (autoDetectControlPoints)
            {
                RefreshControlPointsList();
            }
        }

        public void ClearControlPoints()
        {
            if (!autoDetectControlPoints)
            {
                controlPointTransforms.Clear();
                UpdateControlPoints();
            }
            else
            {
                RefreshControlPointsList();
            }
        }

        public Vector3 GetWorldPoint(float t)
        {
            if (currentSpline == null)
                return transform.position;
                
            Vector3 localPoint = currentSpline.GetPoint(t);
            return transform.TransformPoint(localPoint);
        }

        public Vector3 GetWorldTangent(float t)
        {
            if (currentSpline == null)
                return transform.forward;
                
            Vector3 localTangent = currentSpline.GetTangent(t);
            return transform.TransformDirection(localTangent);
        }

        public float GetLength()
        {
            if (currentSpline == null)
                return 0f;
                
            float localLength = currentSpline.GetLength();
            Vector3 scale = transform.lossyScale;
            float avgScale = (scale.x + scale.y + scale.z) / 3f;
            return localLength * avgScale;
        }
        
        /// <summary>
        /// 手動觸發所有 Cursor 更新
        /// </summary>
        public void ForceUpdateAllCursors()
        {
            UpdateAllCursors();
        }
        
        /// <summary>
        /// 強制初始化曲線（編輯器用）
        /// </summary>
        public void ForceInitializeSpline()
        {
            // 確保曲線實例存在
            if (currentSpline == null)
            {
                CreateSpline(splineType);
            }
            
            // 重新整理控制點
            if (autoDetectControlPoints)
            {
                RefreshControlPointsList();
            }
            
            // 強制更新控制點到曲線
            UpdateControlPoints();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawSpline(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawSpline(true);
        }

        private void DrawSpline(bool selected)
        {
            // 在編輯器模式下確保初始化
            if (!Application.isPlaying && currentSpline == null)
            {
                ForceInitializeSpline();
            }

            if (currentSpline == null || cachedControlPoints == null || cachedControlPoints.Length < 2)
                return;

            // 繪製曲線
            Color drawColor = selected ? splineColor : splineColor * 0.7f;
            Gizmos.color = drawColor;
            
            Vector3 prevPoint = GetWorldPoint(0);
            int drawResolution = selected ? resolution * 2 : resolution;

            for (int i = 1; i <= drawResolution; i++)
            {
                float t = i / (float)drawResolution;
                Vector3 point = GetWorldPoint(t);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            // 繪製控制點
            for (int i = 0; i < controlPointTransforms.Count; i++)
            {
                Transform t = controlPointTransforms[i];
                if (t != null)
                {
                    // 控制點球體
                    Gizmos.color = selected ? Color.yellow : Color.red;
                    float size = selected ? 0.15f : 0.1f;
                    Gizmos.DrawWireSphere(t.position, size);
                }
            }

            // 顯示曲線資訊
            if (selected && controlPointTransforms.Count > 0)
            {
                Vector3 midPoint = GetWorldPoint(0.5f);
                UnityEditor.Handles.Label(midPoint + Vector3.up * 0.5f, 
                    $"[{splineType}] Length: {GetLength():F2}");
            }
        }
#endif
    }
}