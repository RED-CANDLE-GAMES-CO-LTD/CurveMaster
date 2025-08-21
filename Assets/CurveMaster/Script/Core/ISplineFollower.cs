using UnityEngine;

namespace CurveMaster.Core
{
    /// <summary>
    /// 曲線跟隨者介面
    /// </summary>
    public interface ISplineFollower
    {
        /// <summary>
        /// 設定在曲線上的位置
        /// </summary>
        /// <param name="t">位置參數 (0-1)</param>
        void SetPosition(float t);

        /// <summary>
        /// 取得當前位置參數
        /// </summary>
        float GetPosition();

        /// <summary>
        /// 更新位置與旋轉
        /// </summary>
        void UpdateTransform();

        /// <summary>
        /// 設定要跟隨的曲線
        /// </summary>
        void SetSpline(ISpline spline);
    }
}