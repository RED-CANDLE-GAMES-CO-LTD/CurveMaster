using UnityEngine;
using System.Collections.Generic;

namespace CurveMaster.Utils
{
    /// <summary>
    /// 曲線快取系統
    /// </summary>
    public class SplineCache
    {
        private struct CachedPoint
        {
            public Vector3 position;
            public Vector3 tangent;
            public float distance;
        }

        private List<CachedPoint> cachedPoints;
        private int cacheResolution;
        private bool isCacheValid;
        private float totalLength;

        public SplineCache(int resolution = 100)
        {
            cacheResolution = resolution;
            cachedPoints = new List<CachedPoint>(resolution + 1);
            isCacheValid = false;
        }

        public void BuildCache(System.Func<float, Vector3> getPoint, System.Func<float, Vector3> getTangent)
        {
            cachedPoints.Clear();
            totalLength = 0f;
            
            Vector3 prevPoint = getPoint(0);
            cachedPoints.Add(new CachedPoint
            {
                position = prevPoint,
                tangent = getTangent(0),
                distance = 0
            });

            for (int i = 1; i <= cacheResolution; i++)
            {
                float t = i / (float)cacheResolution;
                Vector3 point = getPoint(t);
                Vector3 tangent = getTangent(t);
                float segmentLength = Vector3.Distance(prevPoint, point);
                totalLength += segmentLength;

                cachedPoints.Add(new CachedPoint
                {
                    position = point,
                    tangent = tangent,
                    distance = totalLength
                });

                prevPoint = point;
            }

            isCacheValid = true;
        }

        public Vector3 GetCachedPoint(float t)
        {
            if (!isCacheValid || cachedPoints.Count == 0)
                return Vector3.zero;

            t = Mathf.Clamp01(t);
            float index = t * (cachedPoints.Count - 1);
            int i = Mathf.FloorToInt(index);
            float lerp = index - i;

            if (i >= cachedPoints.Count - 1)
                return cachedPoints[cachedPoints.Count - 1].position;

            return Vector3.Lerp(cachedPoints[i].position, cachedPoints[i + 1].position, lerp);
        }

        public Vector3 GetCachedTangent(float t)
        {
            if (!isCacheValid || cachedPoints.Count == 0)
                return Vector3.forward;

            t = Mathf.Clamp01(t);
            float index = t * (cachedPoints.Count - 1);
            int i = Mathf.FloorToInt(index);
            float lerp = index - i;

            if (i >= cachedPoints.Count - 1)
                return cachedPoints[cachedPoints.Count - 1].tangent;

            return Vector3.Slerp(cachedPoints[i].tangent, cachedPoints[i + 1].tangent, lerp);
        }

        public float GetCachedLength()
        {
            return isCacheValid ? totalLength : 0f;
        }

        public float GetDistanceAtT(float t)
        {
            if (!isCacheValid || cachedPoints.Count == 0)
                return 0f;

            t = Mathf.Clamp01(t);
            float index = t * (cachedPoints.Count - 1);
            int i = Mathf.FloorToInt(index);
            float lerp = index - i;

            if (i >= cachedPoints.Count - 1)
                return totalLength;

            return Mathf.Lerp(cachedPoints[i].distance, cachedPoints[i + 1].distance, lerp);
        }

        public float GetTAtDistance(float distance)
        {
            if (!isCacheValid || cachedPoints.Count == 0 || totalLength == 0)
                return 0f;

            distance = Mathf.Clamp(distance, 0, totalLength);

            for (int i = 0; i < cachedPoints.Count - 1; i++)
            {
                if (distance <= cachedPoints[i + 1].distance)
                {
                    float segmentStart = cachedPoints[i].distance;
                    float segmentEnd = cachedPoints[i + 1].distance;
                    float segmentT = (distance - segmentStart) / (segmentEnd - segmentStart);
                    return (i + segmentT) / (cachedPoints.Count - 1);
                }
            }

            return 1f;
        }

        public void InvalidateCache()
        {
            isCacheValid = false;
        }

        public bool IsCacheValid()
        {
            return isCacheValid;
        }

        public void SetResolution(int resolution)
        {
            if (cacheResolution != resolution)
            {
                cacheResolution = resolution;
                InvalidateCache();
            }
        }
    }
}