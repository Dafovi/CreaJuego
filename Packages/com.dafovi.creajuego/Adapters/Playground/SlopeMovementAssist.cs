using System.Collections.Generic;
using Playground.Movement;
using UnityEngine;

namespace CreaJuego.PlaygroundBackend
{
    // Keeps Playground's horizontal movement usable on gentle educational ramps.
    // The vendor component remains untouched and flat surfaces keep their original feel.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameItem),typeof(Rigidbody2D),typeof(Collider2D))]
    [RequireComponent(typeof(Move))]
    public sealed class SlopeMovementAssist : MonoBehaviour
    {
        readonly List<RaycastHit2D> hits=new List<RaycastHit2D>();
        readonly List<ContactPoint2D> contacts=new List<ContactPoint2D>();
        GameItem item;
        Rigidbody2D body;
        Collider2D bodyCollider;
        Move movement;

        void Awake()=>Resolve();
        void FixedUpdate()=>Apply();

        void Resolve()
        {
            if(item==null)item=GetComponent<GameItem>();
            if(body==null)body=GetComponent<Rigidbody2D>();
            if(bodyCollider==null)bodyCollider=GetComponent<Collider2D>();
            if(movement==null)movement=GetComponent<Move>();
        }

        public void Apply()
        {
            Resolve();
            if(item==null||body==null||bodyCollider==null||movement==null||!body.simulated)return;
            float input=movement.movementSource!=null?movement.movementSource().x:0;
            if(Mathf.Abs(input)<.01f)return;
            Vector2 supportNormal=default;
            contacts.Clear();
            body.GetContacts(contacts);
            foreach(var contact in contacts)
                if(contact.collider!=null&&!contact.collider.isTrigger&&contact.normal.y>.55f&&Mathf.Abs(contact.normal.x)>Mathf.Abs(supportNormal.x))supportNormal=contact.normal;
            if(supportNormal==default)
            {
                hits.Clear();
                var filter=ContactFilter2D.noFilter;
                filter.useTriggers=false;
                bodyCollider.Cast(Vector2.down,filter,hits,.18f);
                foreach(var hit in hits)
                    if(hit.collider!=null&&!hit.collider.isTrigger&&hit.normal.y>.55f&&Mathf.Abs(hit.normal.x)>Mathf.Abs(supportNormal.x))supportNormal=hit.normal;
            }
            if(Mathf.Abs(supportNormal.x)<.05f)return;
            var tangent=new Vector2(supportNormal.y,-supportNormal.x).normalized;
            if(tangent.x<0)tangent=-tangent;
            float desiredSpeed=input*Mathf.Max(.1f,item.speed);
            float currentSpeed=Vector2.Dot(body.linearVelocity,tangent);
            body.linearVelocity+=tangent*(desiredSpeed-currentSpeed);
        }
    }
}