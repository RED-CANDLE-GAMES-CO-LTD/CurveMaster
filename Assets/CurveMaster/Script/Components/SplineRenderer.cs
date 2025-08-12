using UnityEngine;

namespace CurveMaster.Components
{
    /// <summary>
    /// 曲線渲染器元件
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [ExecuteInEditMode]
    public class SplineRenderer : MonoBehaviour
    {
        [SerializeField] private SplineManager splineManager;
        [SerializeField] private int renderResolution = 100;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Gradient colorGradient;
        
        private LineRenderer lineRenderer;
        private Vector3[] lastControlPoints;
        private int lastResolution;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            if (splineManager == null)
            {
                splineManager = GetComponentInParent<SplineManager>();
            }

            SetupLineRenderer();
            UpdateSplineVisualization();
        }

        private void SetupLineRenderer()
        {
            if (lineRenderer == null) return;

            // 設定 LineRenderer 屬性
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            
            // 設定材質
            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }
            else
            {
                // 使用預設材質
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            // 設定顏色漸層
            if (colorGradient == null)
            {
                colorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(Color.green, 0.0f);
                colorKeys[1] = new GradientColorKey(Color.blue, 1.0f);
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
                
                colorGradient.SetKeys(colorKeys, alphaKeys);
            }
            lineRenderer.colorGradient = colorGradient;
        }

        private void Update()
        {
            if (!autoUpdate) return;
            
            if (HasChanged())
            {
                UpdateSplineVisualization();
            }
        }

        private bool HasChanged()
        {
            if (splineManager == null || splineManager.Spline == null)
                return false;

            Vector3[] currentPoints = splineManager.Spline.GetControlPoints();
            
            if (lastControlPoints == null || lastControlPoints.Length != currentPoints?.Length)
                return true;

            if (lastResolution != renderResolution)
                return true;

            if (currentPoints != null)
            {
                for (int i = 0; i < currentPoints.Length; i++)
                {
                    if (lastControlPoints[i] != currentPoints[i])
                        return true;
                }
            }

            return false;
        }

        public void UpdateSplineVisualization()
        {
            if (splineManager == null || splineManager.Spline == null || lineRenderer == null)
            {
                if (lineRenderer != null)
                    lineRenderer.positionCount = 0;
                return;
            }

            Vector3[] controlPoints = splineManager.Spline.GetControlPoints();
            if (controlPoints == null || controlPoints.Length < 2)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            // 設定線段點數
            int pointCount = renderResolution + 1;
            lineRenderer.positionCount = pointCount;

            // 計算每個點的位置
            Vector3[] positions = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)renderResolution;
                positions[i] = splineManager.GetWorldPoint(t);
            }

            lineRenderer.SetPositions(positions);

            // 更新快取
            lastControlPoints = (Vector3[])controlPoints.Clone();
            lastResolution = renderResolution;
        }

        public void SetLineWidth(float width)
        {
            lineWidth = Mathf.Max(0.001f, width);
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
            }
        }

        public void SetRenderResolution(int resolution)
        {
            renderResolution = Mathf.Max(2, resolution);
            UpdateSplineVisualization();
        }

        public void SetSplineManager(SplineManager manager)
        {
            splineManager = manager;
            UpdateSplineVisualization();
        }

        public void SetColorGradient(Gradient gradient)
        {
            colorGradient = gradient;
            if (lineRenderer != null)
            {
                lineRenderer.colorGradient = colorGradient;
            }
        }

        private void OnValidate()
        {
            if (lineRenderer != null)
            {
                SetupLineRenderer();
                UpdateSplineVisualization();
            }
        }
    }
}