using UnityEngine;
using CurveMaster.Components;

namespace CurveMaster.Movement
{
    /// <summary>
    /// 恆定速度移動
    /// </summary>
    public class ConstantSpeedMovement : SplineMovement
    {
        [SerializeField] private bool reverseOnEnd = false;
        private int direction = 1;

        protected override void UpdateMovement()
        {
            if (cursor == null || cursor.GetComponent<SplineManager>() == null)
                return;

            SplineManager splineManager = cursor.GetComponent<SplineManager>();
            float length = splineManager.GetLength();
            
            if (length <= 0)
                return;

            float deltaT = (speed * Time.deltaTime / length) * direction;
            float newPosition = cursor.Position + deltaT;

            if (newPosition >= 1f)
            {
                if (loop)
                {
                    newPosition = newPosition - 1f;
                }
                else if (reverseOnEnd)
                {
                    newPosition = 1f;
                    direction = -1;
                }
                else
                {
                    newPosition = 1f;
                    StopMovement();
                }
            }
            else if (newPosition <= 0f)
            {
                if (loop)
                {
                    newPosition = 1f + newPosition;
                }
                else if (reverseOnEnd)
                {
                    newPosition = 0f;
                    direction = 1;
                }
                else
                {
                    newPosition = 0f;
                    StopMovement();
                }
            }

            cursor.Position = newPosition;
        }

        public override void ResetPosition()
        {
            base.ResetPosition();
            direction = 1;
        }

        public void SetReverseOnEnd(bool value)
        {
            reverseOnEnd = value;
        }

        public void ReverseDirection()
        {
            direction = -direction;
        }
    }
}