using UnityEngine;
using CurveMaster.Core;
using System.Collections.Generic;

namespace CurveMaster.Splines
{
    /// <summary>
    /// B-Spline 插值曲線 - 通過所有控制點
    /// </summary>
    public class BSplineInterpolating : BaseSpline
    {
        private int degree = 3;
        private List<Vector3> bsplineControlPoints = new List<Vector3>();
        private bool needsUpdate = true;

        public BSplineInterpolating(int degree = 3)
        {
            this.degree = degree;
        }

        public override void SetControlPoints(Vector3[] points)
        {
            base.SetControlPoints(points);
            needsUpdate = true;
        }

        public override Vector3 GetPoint(float t)
        {
            if (!HasEnoughPoints(2))
                return Vector3.zero;

            if (controlPoints.Length == 2)
                return Vector3.Lerp(controlPoints[0], controlPoints[1], t);

            // 更新 B-Spline 控制點
            if (needsUpdate)
            {
                ComputeBSplineControlPoints();
                needsUpdate = false;
            }

            // 使用簡化的插值方法 - 確保通過所有點
            int segmentCount = controlPoints.Length - 1;
            t = Mathf.Clamp01(t);
            
            // 特殊處理端點
            if (t >= 1f)
                return controlPoints[controlPoints.Length - 1];
            if (t <= 0f)
                return controlPoints[0];
                
            float scaledT = t * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
            float localT = scaledT - segmentIndex;

            // 使用三次插值確保平滑並通過控制點
            Vector3 p0 = GetInterpolationPoint(segmentIndex - 1);
            Vector3 p1 = controlPoints[segmentIndex];
            Vector3 p2 = controlPoints[segmentIndex + 1];
            Vector3 p3 = GetInterpolationPoint(segmentIndex + 2);

            // 使用 Catmull-Rom 類似的插值確保通過 p1 和 p2
            float t2 = localT * localT;
            float t3 = t2 * localT;

            Vector3 v0 = (p2 - p0) * 0.5f;
            Vector3 v1 = (p3 - p1) * 0.5f;

            return p1 * (2 * t3 - 3 * t2 + 1) +
                   p2 * (-2 * t3 + 3 * t2) +
                   v0 * (t3 - 2 * t2 + localT) +
                   v1 * (t3 - t2);
        }

        private Vector3 GetInterpolationPoint(int index)
        {
            if (index < 0)
                return controlPoints[0] + (controlPoints[0] - controlPoints[1]);
            if (index >= controlPoints.Length)
                return controlPoints[controlPoints.Length - 1] + 
                       (controlPoints[controlPoints.Length - 1] - controlPoints[controlPoints.Length - 2]);
            return controlPoints[index];
        }

        private void ComputeBSplineControlPoints()
        {
            // 這裡可以計算用於視覺化的額外控制點
            // 但主要插值邏輯確保通過所有給定的控制點
            bsplineControlPoints.Clear();
            if (controlPoints != null)
            {
                bsplineControlPoints.AddRange(controlPoints);
            }
        }

        /// <summary>
        /// 取得用於視覺化的 B-Spline 控制點
        /// </summary>
        public Vector3[] GetBSplineControlPoints()
        {
            if (needsUpdate)
            {
                ComputeBSplineControlPoints();
                needsUpdate = false;
            }
            return bsplineControlPoints.ToArray();
        }
    }
}