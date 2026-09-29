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
        private Collider2D bodyCollider;
        private DemoSession session;
        private readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>();
        private readonly List<RaycastHit2D> supportHits = new List<RaycastHit2D>();
        public bool Supported { get; private set; }
        public bool IsSupported => Supported;
        private void Awake()=>Initialize();
        public void Initialize()
        {
            item = GetComponent<GameItem>(); body = GetComponent<Rigidbody2D>(); jump = GetComponent<Jump>(); bodyCollider = GetComponent<Collider2D>();

            jump.checkGround = false; // External support gate, NOT the educational canJump switch.
        }
        private void Start() { session = DemoSession.InScene(gameObject.scene); jump.jumpAllowed = () => item.canJump && Supported && session != null && session.State == GameSessionState.Playing; }
        private void Update()=>EvaluateSupport();
        public void EvaluateSupport()
        {
            contacts.Clear(); body.GetContacts(contacts);
            Supported = body.simulated &&
                contacts.Exists(c => c.normal.y > .65f && c.collider != null && !c.collider.isTrigger);
            if(!Supported && body.simulated && bodyCollider!=null)
            {
                supportHits.Clear();var filter=ContactFilter2D.noFilter;filter.useTriggers=false;
                bodyCollider.Cast(Vector2.down,filter,supportHits,.12f);
                Supported=supportHits.Exists(hit=>hit.collider!=null&&!hit.collider.isTrigger&&hit.normal.y>.55f);
            }
            jump.enabled = item.canJump && session != null && session.State == GameSessionState.Playing;
        }
    }
}


