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
            Gizmos.color = pointColor;
            Gizmos.DrawWireSphere(transform.position, gizmoSize);
        }

        private void OnDrawGizmosSelected()
        {
            if (parentSpline == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, gizmoSize * 1.5f);
        }
    }
}