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
        
        /// <summary>
        /// 重置指定控制點的手柄
        /// </summary>
        public void ResetHandles(int index)
        {
            if (index < 0 || index >= controlPoints.Length)
                return;
                
            Vector3 prevPoint = index > 0 ? controlPoints[index - 1] : controlPoints[index];
            Vector3 nextPoint = index < controlPoints.Length - 1 ? controlPoints[index + 1] : controlPoints[index];
            Vector3 currentPoint = controlPoints[index];
            
            // 計算切線方向
            Vector3 tangent = (nextPoint - prevPoint).normalized;
            float distance = Vector3.Distance(prevPoint, nextPoint) * 0.25f;
            
            // 重置控制手柄
            if (index > 0)
                handleIn[index] = currentPoint - tangent * distance;
            else
                handleIn[index] = currentPoint;
                
            if (index < controlPoints.Length - 1)
                handleOut[index] = currentPoint + tangent * distance;
            else
                handleOut[index] = currentPoint;
                
            SetDirty();
        }
        
        /// <summary>
        /// 重置所有手柄
        /// </summary>
        public void ResetAllHandles()
        {
            GenerateHandles();
            SetDirty();
        }
        
        /// <summary>
        /// 設定手柄對稱模式
        /// </summary>
        public void SetHandleSymmetric(int index, bool symmetric)
        {
            if (index < 0 || index >= controlPoints.Length)
                return;
                
            if (symmetric && index > 0 && index < controlPoints.Length - 1)
            {
                // 計算對稱手柄
                Vector3 currentPoint = controlPoints[index];
                Vector3 handleInDir = (handleIn[index] - currentPoint).normalized;
                Vector3 handleOutDir = (handleOut[index] - currentPoint).normalized;
                
                // 使用平均方向
                Vector3 avgDir = ((handleOut[index] - currentPoint) - (handleIn[index] - currentPoint)).normalized;
                float inDistance = Vector3.Distance(handleIn[index], currentPoint);
                float outDistance = Vector3.Distance(handleOut[index], currentPoint);
                
                handleIn[index] = currentPoint - avgDir * inDistance;
                handleOut[index] = currentPoint + avgDir * outDistance;
                
                SetDirty();
            }
        }
        
        /// <summary>
        /// 鏡像對稱調整手柄
        /// </summary>
        public void MirrorHandle(int index, bool isHandleOut)
        {
            if (index < 0 || index >= controlPoints.Length)
                return;
                
            Vector3 currentPoint = controlPoints[index];
            
            if (isHandleOut && index > 0)
            {
                // 根據出手柄鏡像調整入手柄
                Vector3 mirrorHandle = currentPoint + (currentPoint - handleOut[index]);
                handleIn[index] = mirrorHandle;
            }
            else if (!isHandleOut && index < controlPoints.Length - 1)
            {
                // 根據入手柄鏡像調整出手柄
                Vector3 mirrorHandle = currentPoint + (currentPoint - handleIn[index]);
                handleOut[index] = mirrorHandle;
            }
            
            SetDirty();
        }
        
        /// <summary>
        /// 取得所有手柄資料（用於序列化）
        /// </summary>
        public void GetHandles(out List<Vector3> inHandles, out List<Vector3> outHandles)
        {
            inHandles = new List<Vector3>(handleIn);
            outHandles = new List<Vector3>(handleOut);
        }
        
        /// <summary>
        /// 設定所有手柄資料（用於反序列化）
        /// </summary>
        public void SetHandles(List<Vector3> inHandles, List<Vector3> outHandles)
        {
            if (inHandles != null && outHandles != null &&
                inHandles.Count == controlPoints.Length && 
                outHandles.Count == controlPoints.Length)
            {
                handleIn = new List<Vector3>(inHandles);
                handleOut = new List<Vector3>(outHandles);
                SetDirty();
            }
        }
    }
}