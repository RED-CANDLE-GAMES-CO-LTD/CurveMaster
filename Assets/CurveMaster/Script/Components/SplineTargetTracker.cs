using UnityEngine;

namespace CurveMaster.Components
{
    /// <summary>
    /// 控制點追蹤元件 - 讓控制點追蹤指定目標
    /// </summary>
    [RequireComponent(typeof(SplineControlPoint))]
    public class SplineTargetTracker : MonoBehaviour
    {
        public enum TrackingMode
        {
            Direct,      // 直接追蹤
            Smooth,      // 平滑追蹤
            Spring,      // 彈性追蹤
            Limited      // 範圍限制追蹤
        }

        [Header("追蹤設定")]
        [SerializeField] private bool enableTracking = true;
        [SerializeField] private Transform targetObject;
        [SerializeField] private TrackingMode trackingMode = TrackingMode.Smooth;
        
        [Header("追蹤參數")]
        [SerializeField] private float trackingSpeed = 5f;
        [SerializeField] private float springDamping = 0.5f;
        [SerializeField] private float maxDistance = 10f;
        
        [Header("偏移設定")]
        [SerializeField] private Vector3 targetOffset = Vector3.zero;
        [SerializeField] private bool useLocalOffset = false;
        
        [Header("Initialization")]
        [Tooltip("Instantly snap to target position when GameObject is enabled")]
        [SerializeField] private bool snapOnEnable = true;
        
        // 內部變數
        private SplineControlPoint controlPoint;
        private Vector3 velocity;
        private Vector3 originalPosition;
        private bool hasOriginalPosition;
        private bool justEnabled;
        
        public bool EnableTracking
        {
            get => enableTracking;
            set => enableTracking = value;
        }
        
        public Transform TargetObject
        {
            get => targetObject;
            set => targetObject = value;
        }
        
        public float TrackingSpeed
        {
            get => trackingSpeed;
            set => trackingSpeed = Mathf.Max(0, value);
        }
        
        private void Awake()
        {
            controlPoint = GetComponent<SplineControlPoint>();
            if (!hasOriginalPosition)
            {
                originalPosition = transform.position;
                hasOriginalPosition = true;
            }
        }
        
        private void OnEnable()
        {
            if (!hasOriginalPosition)
            {
                originalPosition = transform.position;
                hasOriginalPosition = true;
            }
            
            // Mark as just enabled for instant snap
            justEnabled = snapOnEnable;
            
            // Don't update position here - wait for LateUpdate to ensure proper order
            // This prevents jitter when multiple components are updating
        }
        
        private void LateUpdate()
        {
            if (!enableTracking || targetObject == null)
                return;
            
            // If just enabled and snap is on, do instant update first
            if (justEnabled && snapOnEnable)
            {
                transform.position = CalculateTargetPosition();
                velocity = Vector3.zero; // Reset velocity for spring mode
                justEnabled = false;
                return; // Skip normal update this frame since we're already at target
            }
            
            UpdateTracking();
        }
        
        private void UpdateTracking()
        {
            Vector3 targetPosition = CalculateTargetPosition();
            
            switch (trackingMode)
            {
                case TrackingMode.Direct:
                    transform.position = targetPosition;
                    break;
                    
                case TrackingMode.Smooth:
                    transform.position = Vector3.Lerp(
                        transform.position, 
                        targetPosition, 
                        trackingSpeed * Time.deltaTime
                    );
                    break;
                    
                case TrackingMode.Spring:
                    transform.position = Vector3.SmoothDamp(
                        transform.position,
                        targetPosition,
                        ref velocity,
                        springDamping / trackingSpeed
                    );
                    break;
                    
                case TrackingMode.Limited:
                    Vector3 desiredPosition = Vector3.Lerp(
                        transform.position,
                        targetPosition,
                        trackingSpeed * Time.deltaTime
                    );
                    
                    // 限制在最大距離內
                    Vector3 offset = desiredPosition - originalPosition;
                    if (offset.magnitude > maxDistance)
                    {
                        offset = offset.normalized * maxDistance;
                        desiredPosition = originalPosition + offset;
                    }
                    
                    transform.position = desiredPosition;
                    break;
            }
        }
        
        private Vector3 CalculateTargetPosition()
        {
            Vector3 basePosition = targetObject.position;
            
            if (useLocalOffset)
            {
                // 使用目標的本地座標系偏移
                basePosition += targetObject.TransformDirection(targetOffset);
            }
            else
            {
                // 使用世界座標偏移
                basePosition += targetOffset;
            }
            
            return basePosition;
        }
        
        /// <summary>
        /// 重設到原始位置
        /// </summary>
        public void ResetToOriginal()
        {
            if (hasOriginalPosition)
            {
                transform.position = originalPosition;
                velocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// 更新原始位置為當前位置
        /// </summary>
        public void SetOriginalPosition()
        {
            originalPosition = transform.position;
            hasOriginalPosition = true;
        }
        
        /// <summary>
        /// 設定追蹤目標和參數
        /// </summary>
        public void SetupTracking(Transform target, TrackingMode mode, float speed)
        {
            targetObject = target;
            trackingMode = mode;
            trackingSpeed = speed;
            enableTracking = target != null;
        }
        
        /// <summary>
        /// Force immediate snap to target position (used by ShapeKeeper)
        /// </summary>
        public void ForceToTarget()
        {
            if (enableTracking && targetObject != null)
            {
                transform.position = CalculateTargetPosition();
                velocity = Vector3.zero;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!enableTracking || targetObject == null)
                return;
            
            // 繪製到目標的連線
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, CalculateTargetPosition());
            
            // 繪製目標位置
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(CalculateTargetPosition(), 0.1f);
            
            // 如果是限制模式，顯示範圍
            if (trackingMode == TrackingMode.Limited && hasOriginalPosition)
            {
                Gizmos.color = new Color(0, 1, 1, 0.2f);
                Gizmos.DrawWireSphere(originalPosition, maxDistance);
            }
        }
    }
}