using UnityEngine;
using CurveMaster.Core;
using System.Collections.Generic;

namespace CurveMaster.Splines
{
    /// <summary>
    /// 貝茲曲線實作 - 通過所有控制點
    /// </summary>
    public class BezierSpline : BaseSpline
    {
        // 每個控制點的貝茲控制手柄
        private List<Vector3> handleIn = new List<Vector3>();
        private List<Vector3> handleOut = new List<Vector3>();
        
        public override void SetControlPoints(Vector3[] points)
        {
            base.SetControlPoints(points);
            GenerateHandles();
        }
        
        /// <summary>
        /// 自動生成貝茲控制手柄
        /// </summary>
        private void GenerateHandles()
        {
            if (controlPoints == null || controlPoints.Length < 2)
                return;
                
            handleIn.Clear();
            handleOut.Clear();
            
            for (int i = 0; i < controlPoints.Length; i++)
            {
                Vector3 prevPoint = i > 0 ? controlPoints[i - 1] : controlPoints[i];
                Vector3 nextPoint = i < controlPoints.Length - 1 ? controlPoints[i + 1] : controlPoints[i];
                Vector3 currentPoint = controlPoints[i];
                
                // 計算切線方向
                Vector3 tangent = (nextPoint - prevPoint).normalized;
                float distance = Vector3.Distance(prevPoint, nextPoint) * 0.25f;
                
                // 設定控制手柄
                handleIn.Add(currentPoint - tangent * distance);
                handleOut.Add(currentPoint + tangent * distance);
            }
            
            // 端點特殊處理
            if (controlPoints.Length >= 2)
            {
                handleIn[0] = controlPoints[0];
                handleOut[controlPoints.Length - 1] = controlPoints[controlPoints.Length - 1];
            }
        }
        
        public override Vector3 GetPoint(float t)
        {
            if (!HasEnoughPoints(2))
                return Vector3.zero;
                
            if (controlPoints.Length == 2)
                return Vector3.Lerp(controlPoints[0], controlPoints[1], t);
            
            // 確保手柄已生成
            if (handleIn.Count != controlPoints.Length)
                GenerateHandles();
            
            int segmentCount = controlPoints.Length - 1;
            t = Mathf.Clamp01(t);
            float scaledT = t * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
            float localT = scaledT - segmentIndex;
            
            // 使用貝茲曲線連接相鄰控制點
            return CalculateBezier(
                controlPoints[segmentIndex],
                handleOut[segmentIndex],
                handleIn[segmentIndex + 1],
                controlPoints[segmentIndex + 1],
                localT
            );
        }

        private Vector3 CalculateBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float oneMinusT = 1f - t;
            float oneMinusT2 = oneMinusT * oneMinusT;
            float oneMinusT3 = oneMinusT2 * oneMinusT;
            float t2 = t * t;
            float t3 = t2 * t;

            return oneMinusT3 * p0 +
                   3f * oneMinusT2 * t * p1 +
                   3f * oneMinusT * t2 * p2 +
                   t3 * p3;
        }

        public override Vector3 GetTangent(float t)
        {
            if (!HasEnoughPoints(2))
                return Vector3.forward;
                
            if (controlPoints.Length == 2)
                return (controlPoints[1] - controlPoints[0]).normalized;
            
            // 確保手柄已生成
            if (handleIn.Count != controlPoints.Length)
                GenerateHandles();
            
            int segmentCount = controlPoints.Length - 1;
            t = Mathf.Clamp01(t);
            float scaledT = t * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
            float localT = scaledT - segmentIndex;
            
            return CalculateBezierDerivative(
                controlPoints[segmentIndex],
                handleOut[segmentIndex],
                handleIn[segmentIndex + 1],
                controlPoints[segmentIndex + 1],
                localT
            ).normalized;
        }

        private Vector3 CalculateBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float oneMinusT = 1f - t;
            float oneMinusT2 = oneMinusT * oneMinusT;
            float t2 = t * t;

            return 3f * oneMinusT2 * (p1 - p0) +
                   6f * oneMinusT * t * (p2 - p1) +
                   3f * t2 * (p3 - p2);
        }

        /// <summary>
        /// 取得控制手柄
        /// </summary>
        public Vector3 GetHandleIn(int index)
        {
            if (index < 0 || index >= handleIn.Count)
                return Vector3.zero;
            return handleIn[index];
        }
        
        public Vector3 GetHandleOut(int index)
        {
            if (index < 0 || index >= handleOut.Count)
                return Vector3.zero;
            return handleOut[index];
        }
        
        /// <summary>
        /// 設定控制手柄
        /// </summary>
        public void SetHandleIn(int index, Vector3 handle)
        {
            if (index >= 0 && index < handleIn.Count)
            {
                handleIn[index] = handle;
                SetDirty();
            }
        }
        
        public void SetHandleOut(int index, Vector3 handle)
        {
            if (index >= 0 && index < handleOut.Count)
            {
                handleOut[index] = handle;
                SetDirty();
            }
        }
        
        /// <summary>
        /// 取得手柄數量
        /// </summary>
        public int GetHandleCount()
        {
            return handleIn.Count;
        }
    }
}