using UnityEngine;
using CurveMaster.Core;

namespace CurveMaster.Splines
{
    /// <summary>
    /// B-Spline 曲線實作
    /// </summary>
    public class BSpline : BaseSpline
    {
        private int degree = 3;
        private float[] knots;

        public BSpline(int degree = 3)
        {
            this.degree = degree;
        }

        public override Vector3 GetPoint(float t)
        {
            if (!HasEnoughPoints(2))
            {
                // 如果點數不足，使用線性插值
                if (controlPoints != null && controlPoints.Length == 2)
                    return Vector3.Lerp(controlPoints[0], controlPoints[1], t);
                return Vector3.zero;
            }
            
            // 如果點數少於 degree + 1，降低 degree
            int actualDegree = Mathf.Min(degree, controlPoints.Length - 1);

            UpdateKnots();
            
            int n = controlPoints.Length - 1;
            t = Mathf.Clamp01(t);
            
            float knot = knots[actualDegree] + t * (knots[n + 1] - knots[actualDegree]);
            
            Vector3 result = Vector3.zero;
            for (int i = 0; i <= n; i++)
            {
                float basis = CalculateBasis(i, actualDegree, knot);
                result += controlPoints[i] * basis;
            }
            
            return result;
        }

        private void UpdateKnots()
        {
            if (controlPoints == null) return;
            
            int n = controlPoints.Length;
            int knotCount = n + degree + 1;
            
            if (knots == null || knots.Length != knotCount)
            {
                knots = new float[knotCount];
            }
            
            for (int i = 0; i < knotCount; i++)
            {
                if (i < degree + 1)
                    knots[i] = 0;
                else if (i >= n)
                    knots[i] = n - degree;
                else
                    knots[i] = i - degree;
            }
        }

        private float CalculateBasis(int i, int p, float u)
        {
            if (p == 0)
            {
                return (u >= knots[i] && u < knots[i + 1]) ? 1.0f : 0.0f;
            }
            
            float left = 0f;
            float right = 0f;
            
            float denomLeft = knots[i + p] - knots[i];
            if (denomLeft > 0)
            {
                left = (u - knots[i]) / denomLeft * CalculateBasis(i, p - 1, u);
            }
            
            float denomRight = knots[i + p + 1] - knots[i + 1];
            if (denomRight > 0)
            {
                right = (knots[i + p + 1] - u) / denomRight * CalculateBasis(i + 1, p - 1, u);
            }
            
            return left + right;
        }
    }
}