using System.Collections.Generic;
using Playground.Movement;
using UnityEngine;
namespace CreaJuego.PlaygroundBackend
{
    // Runs before vendor Jump.Update. Ground support replaces the tag-based rearm contract.
    [DefaultExecutionOrder(-150)]
    [RequireComponent(typeof(GameItem), typeof(Rigidbody2D), typeof(Jump))]
    public sealed class GroundedJumpGate : MonoBehaviour, IVisualMotionState
    {
        private GameItem item;
        private Rigidbody2D body;
        private Jump jump;
        private DemoSession session;
        private readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>();
        public bool Supported { get; private set; }
        public bool IsSupported => Supported;
        private void Awake()
        {
            item = GetComponent<GameItem>(); body = GetComponent<Rigidbody2D>(); jump = GetComponent<Jump>();

            jump.checkGround = false; // External support gate, NOT the educational canJump switch.
        }
        private void Start() { session = DemoSession.InScene(gameObject.scene); jump.jumpAllowed = () => item.canJump && Supported && session != null && session.State == GameSessionState.Playing; }
        private void Update()
        {
            contacts.Clear(); body.GetContacts(contacts);
            Supported = body.simulated && body.linearVelocity.y <= .15f &&
                contacts.Exists(c => c.normal.y > .65f && c.collider != null && !c.collider.isTrigger);
            jump.enabled = item.canJump && session != null && session.State == GameSessionState.Playing;
        }
    }
}


