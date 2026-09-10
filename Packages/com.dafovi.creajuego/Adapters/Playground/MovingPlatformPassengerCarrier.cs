using System.Collections.Generic;
using UnityEngine;

namespace CreaJuego.PlaygroundBackend
{
    // Adds the movement delta of a kinematic platform to dynamic passengers standing on it.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MovingPlatformPassengerCarrier : MonoBehaviour
    {
        readonly HashSet<Rigidbody2D> passengers = new HashSet<Rigidbody2D>();
        Rigidbody2D platform;
        Vector2 previousPosition;

        void Awake()
        {
            platform = GetComponent<Rigidbody2D>();
            previousPosition = platform.position;
        }

        void OnEnable()
        {
            if (platform == null) platform = GetComponent<Rigidbody2D>();
            previousPosition = platform.position;
            passengers.Clear();
        }

        void FixedUpdate()
        {
            Vector2 current = platform.position;
            Vector2 delta = current - previousPosition;
            previousPosition = current;
            if (delta.sqrMagnitude <= .0000001f) return;
            passengers.RemoveWhere(body => body == null);
            foreach (var passenger in passengers)
                passenger.position += delta;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            var passenger = collision.rigidbody;
            if (passenger != null && passenger.bodyType == RigidbodyType2D.Dynamic && passenger.position.y > platform.position.y)
                passengers.Add(passenger);
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.rigidbody != null) passengers.Remove(collision.rigidbody);
        }
    }
}
