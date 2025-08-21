using UnityEngine;

namespace CurveMaster.Components
{
    /// <summary>
    /// 控制點元件
    /// </summary>
    [ExecuteInEditMode]
    public class SplineControlPoint : MonoBehaviour
    {
        [SerializeField] private SplineManager parentSpline;
        [SerializeField] private int index = -1;
        [SerializeField] private Color pointColor = Color.red;
        [SerializeField] private float gizmoSize = 0.1f;

        public SplineManager ParentSpline => parentSpline;
        public int Index => index;

        private void Awake()
        {
            if (parentSpline == null)
            {
                FindParentSpline();
            }
            NotifySplineManager();
        }

        private void OnEnable()
        {
            if (parentSpline == null)
            {
                FindParentSpline();
            }
            NotifySplineManager();
        }

        private void OnDisable()
        {
            NotifySplineManager();
        }
        
        private void OnDestroy()
        {
            NotifySplineManager();
        }
        
        private void OnTransformParentChanged()
        {
            FindParentSpline();
            NotifySplineManager();
        }
        
        private void NotifySplineManager()
        {
            if (parentSpline != null)
            {
                parentSpline.NotifyControlPointsChanged();
            }
        }

        private void FindParentSpline()
        {
            Transform current = transform.parent;
            while (current != null)
            {
                parentSpline = current.GetComponent<SplineManager>();
                if (parentSpline != null)
                    break;
                current = current.parent;
            }
        }

        public void SetParentSpline(SplineManager spline)
        {
            parentSpline = spline;
            NotifySplineManager();
        }

        public void SetIndex(int newIndex)
        {
            index = newIndex;
        }

        private void OnDrawGizmos()
        {
            // 檢查是否有追蹤器
            var tracker = GetComponent<SplineTargetTracker>();
            bool hasTracker = tracker != null && tracker.EnableTracking;
            
            if (hasTracker)
            {
                // 有追蹤器時顯示不同的 Gizmo
                Gizmos.color = new Color(0, 1, 1, 0.8f); // 青色
                Gizmos.DrawWireCube(transform.position, Vector3.one * gizmoSize * 2f);
                
                // 畫一個小的內圈表示是控制點
                Gizmos.color = pointColor * 0.5f;
                Gizmos.DrawWireSphere(transform.position, gizmoSize * 0.5f);
            }
            else
            {
                // 一般控制點
                Gizmos.color = pointColor;
                Gizmos.DrawWireSphere(transform.position, gizmoSize);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 檢查是否有追蹤器
            var tracker = GetComponent<SplineTargetTracker>();
            bool hasTracker = tracker != null && tracker.EnableTracking;
            
            if (hasTracker)
            {
                // 選中時顯示更明顯的標記
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, Vector3.one * gizmoSize * 2.5f);
                
                // 顯示追蹤狀態
                if (tracker.TargetObject != null)
                {
                    Gizmos.color = new Color(1, 1, 0, 0.3f);
                    Gizmos.DrawLine(transform.position, tracker.TargetObject.position);
                }
            }
            else
            {
                // 一般選中狀態
                if (parentSpline != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position, gizmoSize * 1.5f);
                }
            }
        }
    }
}