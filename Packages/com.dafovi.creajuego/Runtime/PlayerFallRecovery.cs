using UnityEngine;

namespace CreaJuego
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerFallRecovery : MonoBehaviour
    {
        [SerializeField, HideInInspector] private Vector3 safePosition;
        [SerializeField, HideInInspector] private bool hasSafePosition;
        [SerializeField, Min(3)] private float fallDistance = 12;

        public Vector3 SafePosition => safePosition;
        public bool HasSafePosition => hasSafePosition;
        public float FallLimit => useWorldLimit ? worldLimit : safePosition.y - fallDistance;
        private bool useWorldLimit;
        private float worldLimit;
        public void ConfigureWorldLimit(Vector3 initialPosition, float lowerLimit)
        {
            safePosition = initialPosition; hasSafePosition = true; worldLimit = lowerLimit; useWorldLimit = true;
        }

        private void Awake() => CaptureCurrentPosition();

        private void FixedUpdate()
        {
            if (hasSafePosition && transform.position.y < FallLimit) ReturnToStart();
        }

        public void CaptureCurrentPosition()
        {
            safePosition = transform.position;
            hasSafePosition = true;
        }

        public void ReturnToStart()
        {
            if (!hasSafePosition) CaptureCurrentPosition();
            var body = GetComponent<Rigidbody2D>();
            transform.position = safePosition;
            body.position = safePosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            body.WakeUp();
        }
    }
}
