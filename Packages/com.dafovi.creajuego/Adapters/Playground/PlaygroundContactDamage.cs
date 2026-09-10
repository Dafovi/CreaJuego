using Playground.Attributes;
using UnityEngine;
namespace CreaJuego.PlaygroundBackend
{
    // Vendor component remains configuration-compatible; this owns safe contact delivery.
    [DisallowMultipleComponent]
    public sealed class PlaygroundContactDamage : MonoBehaviour, IVisualActionState
    {
        float visualAttackUntil;
        public bool IsVisuallyAttacking => Time.time < visualAttackUntil;
        void OnTriggerEnter2D(Collider2D other)=>Contact(other);
        void OnTriggerStay2D(Collider2D other)=>Contact(other);
        void OnCollisionEnter2D(Collision2D other)=>Contact(other.collider);
        void OnCollisionStay2D(Collision2D other)=>Contact(other.collider);
        void Contact(Collider2D other)
        {
            if(!isActiveAndEnabled) return;
            var session=DemoSession.InScene(gameObject.scene);
            if(session==null || session.State!=GameSessionState.Playing) return;
            var vitality=GetComponent<EnemyVitality>(); if(vitality!=null && vitality.Defeated) return;
            var source=GetComponent<ModifyHealthAttribute>();
            if(source==null || !source.enabled) return;
            var receiver=other.GetComponentInParent<PlayerDamageReceiver>();
            if(receiver!=null && receiver.TryReceive(-source.healthChange,transform.position))
            {
                var appearance=GetComponent<GameItem>()?.SelectedAppearance;
                var clip=appearance?.attackClip;
                float duration=clip!=null ? clip.length : .12f;
                visualAttackUntil=Time.time+Mathf.Max(.12f,duration);
                if(source.destroyWhenActivated) Destroy(gameObject);
            }
        }
    }
}