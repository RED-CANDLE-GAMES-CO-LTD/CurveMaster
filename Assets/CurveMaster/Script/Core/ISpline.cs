using UnityEngine;

namespace CurveMaster.Core
{
    /// <summary>
    /// 曲線介面
    /// </summary>
    public interface ISpline
    {
        /// <summary>
        /// 取得曲線上的點
        /// </summary>
        /// <param name="t">位置參數 (0-1)</param>
        Vector3 GetPoint(float t);

        /// <summary>
        /// 取得曲線上的切線
        /// </summary>
        /// <param name="t">位置參數 (0-1)</param>
        Vector3 GetTangent(float t);

        /// <summary>
        /// 取得曲線長度
        /// </summary>
        float GetLength();

        /// <summary>
        /// 設定控制點
        /// </summary>
        void SetControlPoints(Vector3[] points);

        /// <summary>
        /// 取得控制點
        /// </summary>
        Vector3[] GetControlPoints();

        /// <summary>
        /// 曲線是否需要更新
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// 標記曲線需要更新
        /// </summary>
        void SetDirty();

        /// <summary>
        /// 清除更新標記
        /// </summary>
        void ClearDirty();
    }
}