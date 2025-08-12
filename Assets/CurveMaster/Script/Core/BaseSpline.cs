using UnityEngine;
using System.Collections.Generic;

namespace CurveMaster.Core
{
    /// <summary>
    /// 曲線基礎類別
    /// </summary>
    public abstract class BaseSpline : ISpline
    {
        protected Vector3[] controlPoints;
        protected bool isDirty = true;
        protected float cachedLength = 0f;
        protected const int SEGMENT_RESOLUTION = 100;

        public bool IsDirty => isDirty;

        public virtual void SetControlPoints(Vector3[] points)
        {
            controlPoints = points;
            SetDirty();
        }

        public virtual Vector3[] GetControlPoints()
        {
            return controlPoints;
        }

        public abstract Vector3 GetPoint(float t);

        public virtual Vector3 GetTangent(float t)
        {
            float delta = 0.0001f;
            float t1 = Mathf.Clamp01(t - delta);
            float t2 = Mathf.Clamp01(t + delta);
            
            Vector3 p1 = GetPoint(t1);
            Vector3 p2 = GetPoint(t2);
            
            return (p2 - p1).normalized;
        }

        public virtual float GetLength()
        {
            if (!isDirty) return cachedLength;
            
            cachedLength = CalculateLength();
            return cachedLength;
        }

        protected virtual float CalculateLength()
        {
            float length = 0f;
            Vector3 prevPoint = GetPoint(0f);
            
            for (int i = 1; i <= SEGMENT_RESOLUTION; i++)
            {
                float t = i / (float)SEGMENT_RESOLUTION;
                Vector3 point = GetPoint(t);
                length += Vector3.Distance(prevPoint, point);
                prevPoint = point;
            }
            
            return length;
        }

        public void SetDirty()
        {
            isDirty = true;
        }

        public void ClearDirty()
        {
            isDirty = false;
        }

        protected int GetValidControlPointCount()
        {
            return controlPoints?.Length ?? 0;
        }

        protected bool HasEnoughPoints(int minPoints)
        {
            return GetValidControlPointCount() >= minPoints;
        }
    }
}