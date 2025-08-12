using UnityEngine;
using CurveMaster.Components;

namespace CurveMaster.Movement
{
    /// <summary>
    /// 曲線移動基礎類別
    /// </summary>
    public abstract class SplineMovement : MonoBehaviour
    {
        [SerializeField] protected SplineCursor cursor;
        [SerializeField] protected float speed = 1f;
        [SerializeField] protected bool isMoving = false;
        [SerializeField] protected bool loop = false;

        protected virtual void Awake()
        {
            if (cursor == null)
            {
                cursor = GetComponent<SplineCursor>();
            }
        }

        protected virtual void Update()
        {
            if (isMoving && cursor != null)
            {
                UpdateMovement();
            }
        }

        protected abstract void UpdateMovement();

        public virtual void StartMovement()
        {
            isMoving = true;
        }

        public virtual void StopMovement()
        {
            isMoving = false;
        }

        public virtual void ResetPosition()
        {
            if (cursor != null)
            {
                cursor.Position = 0f;
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = Mathf.Max(0f, newSpeed);
        }
    }
}