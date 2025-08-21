using UnityEngine;
using CurveMaster.Core;

namespace CurveMaster.Splines
{
    /// <summary>
    /// 三次樣條曲線實作（簡化版）
    /// </summary>
    public class CubicSpline : BaseSpline
    {
        public override Vector3 GetPoint(float t)
        {
            if (!HasEnoughPoints(2))
                return Vector3.zero;

            int n = controlPoints.Length;
            if (n == 2)
            {
                // 只有兩個點時，使用線性插值
                return Vector3.Lerp(controlPoints[0], controlPoints[1], t);
            }

            // 使用 Hermite 樣條簡化實作
            t = Mathf.Clamp01(t);
            
            // 特殊處理端點
            if (t >= 1f)
                return controlPoints[n - 1];
            if (t <= 0f)
                return controlPoints[0];
                
            float scaledT = t * (n - 1);
            int index = Mathf.FloorToInt(scaledT);
            float localT = scaledT - index;

            index = Mathf.Clamp(index, 0, n - 2);

            Vector3 p0 = controlPoints[index];
            Vector3 p1 = controlPoints[index + 1];
            
            // 計算切線
            Vector3 m0, m1;
            
            if (index == 0)
            {
                m0 = (controlPoints[1] - controlPoints[0]);
            }
            else
            {
                m0 = (controlPoints[index + 1] - controlPoints[index - 1]) * 0.5f;
            }
            
            if (index == n - 2)
            {
                m1 = (controlPoints[n - 1] - controlPoints[n - 2]);
            }
            else
            {
                m1 = (controlPoints[index + 2] - controlPoints[index]) * 0.5f;
            }

            // Hermite 插值
            float t2 = localT * localT;
            float t3 = t2 * localT;
            
            float h00 = 2 * t3 - 3 * t2 + 1;
            float h10 = t3 - 2 * t2 + localT;
            float h01 = -2 * t3 + 3 * t2;
            float h11 = t3 - t2;
            
            return h00 * p0 + h10 * m0 + h01 * p1 + h11 * m1;
        }
    }
}