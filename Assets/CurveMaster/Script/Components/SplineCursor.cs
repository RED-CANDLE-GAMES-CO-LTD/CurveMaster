using UnityEngine;
using CurveMaster.Core;

namespace CurveMaster.Components
{
    /// <summary>
    /// 曲線游標元件
    /// </summary>
    [ExecuteInEditMode]
    public class SplineCursor : MonoBehaviour, ISplineFollower
    {
        [SerializeField] private SplineManager splineManager;
        [SerializeField, Range(0f, 1f)] private float position = 0f;
        [SerializeField] private bool alignToTangent = true;
        [SerializeField] private bool autoUpdate = true;
        
        private ISpline currentSpline;
        private float lastPosition;

        public float Position
        {
            get => position;
            set
            {
                position = Mathf.Clamp01(value);
                if (autoUpdate)
                {
                    UpdateTransform();
                }
            }
        }

        public bool AlignToTangent
        {
            get => alignToTangent;
            set
            {
                alignToTangent = value;
                if (autoUpdate)
                {
                    UpdateTransform();
                }
            }
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Update()
        {
            if (!autoUpdate)
                return;

            if (splineManager == null)
                return;

            if (Mathf.Abs(position - lastPosition) > 0.0001f)
            {
                UpdateTransform();
                lastPosition = position;
            }
        }

        private void Initialize()
        {
            if (splineManager == null)
            {
                splineManager = GetComponentInParent<SplineManager>();
            }

            if (splineManager != null)
            {
                SetSpline(splineManager.Spline);
                UpdateTransform();
            }
        }

        public void SetPosition(float t)
        {
            Position = t;
        }

        public float GetPosition()
        {
            return position;
        }

        public void UpdateTransform()
        {
            if (splineManager == null || currentSpline == null)
                return;

            Vector3 worldPosition = splineManager.GetWorldPoint(position);
            transform.position = worldPosition;

            if (alignToTangent)
            {
                Vector3 worldTangent = splineManager.GetWorldTangent(position);
                if (worldTangent.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(worldTangent);
                }
            }
        }

        public void SetSpline(ISpline spline)
        {
            currentSpline = spline;
            UpdateTransform();
        }

        public void SetSplineManager(SplineManager manager)
        {
            splineManager = manager;
            if (manager != null)
            {
                SetSpline(manager.Spline);
            }
        }

        public void MoveAlongSpline(float deltaT)
        {
            Position = Mathf.Clamp01(position + deltaT);
        }

        private void OnDrawGizmos()
        {
            if (splineManager == null)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.15f);

            if (alignToTangent)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
            }
        }
    }
}