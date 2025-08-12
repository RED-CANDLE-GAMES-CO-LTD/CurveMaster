using UnityEngine;
using CurveMaster.Core;

namespace CurveMaster.Splines
{
    /// <summary>
    /// Catmull-Rom 曲線實作
    /// </summary>
    public class CatmullRomSpline : BaseSpline
    {
        private float tension = 0.5f;

        public CatmullRomSpline(float tension = 0.5f)
        {
            this.tension = tension;
        }

        public override Vector3 GetPoint(float t)
        {
            if (!HasEnoughPoints(2))
            {
                if (controlPoints != null && controlPoints.Length == 2)
                    return Vector3.Lerp(controlPoints[0], controlPoints[1], t);
                return Vector3.zero;
            }

            int pointCount = controlPoints.Length;
            
            // 特殊處理端點
            if (t >= 1f)
                return controlPoints[pointCount - 1];
            if (t <= 0f)
                return controlPoints[0];
                
            float scaledT = t * (pointCount - 1);
            int index = Mathf.FloorToInt(scaledT);
            float localT = scaledT - index;

            index = Mathf.Clamp(index, 0, pointCount - 2);

            Vector3 p0 = GetControlPoint(index - 1);
            Vector3 p1 = GetControlPoint(index);
            Vector3 p2 = GetControlPoint(index + 1);
            Vector3 p3 = GetControlPoint(index + 2);

            return CalculateCatmullRom(p0, p1, p2, p3, localT);
        }

        private Vector3 GetControlPoint(int index)
        {
            if (index < 0)
                return controlPoints[0];
            if (index >= controlPoints.Length)
                return controlPoints[controlPoints.Length - 1];
            return controlPoints[index];
        }

        private Vector3 CalculateCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            Vector3 v0 = (p2 - p0) * tension;
            Vector3 v1 = (p3 - p1) * tension;

            Vector3 result = 
                p1 * (2 * t3 - 3 * t2 + 1) +
                p2 * (-2 * t3 + 3 * t2) +
                v0 * (t3 - 2 * t2 + t) +
                v1 * (t3 - t2);

            return result;
        }

        public void SetTension(float value)
        {
            tension = Mathf.Clamp01(value);
            SetDirty();
        }

        public float GetTension()
        {
            return tension;
        }
    }
}