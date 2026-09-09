using System;
using UnityEngine;
namespace CreaJuego
{
    // Bridges an existing health backend. Authored health is never written here.
    [DisallowMultipleComponent]
    public sealed class PlayerDamageReceiver : MonoBehaviour
    {
        public const float GraceSeconds=.65f;
        Func<int> readHealth;
        Action<int> changeHealth;
        Func<bool> playing;
        int previousHealth;
        float immuneUntil;
        Vector2? origin;
        public bool Invulnerable => Time.time<immuneUntil;
        public bool CanReceive => isActiveAndEnabled && playing!=null && playing() && !Invulnerable;
        public void Configure(Func<int> read,Action<int> change,Func<bool> allowed)
        { readHealth=read; changeHealth=change; playing=allowed; previousHealth=read(); }
        public bool TryReceive(int damage,Vector2 source)
        {
            if(!CanReceive || damage<=0 || changeHealth==null) return false;
            int before=readHealth(); origin=source;
            try { changeHealth(-damage); } finally { origin=null; }
            return readHealth()<before;
        }
        public void HealthChanged(int remaining)
        {
            if(remaining<previousHealth)
            {
                immuneUntil=Time.time+GraceSeconds;
                GetComponent<DamageFeedback>()?.Flash();
                var body=GetComponent<Rigidbody2D>();
                if(remaining>0 && origin.HasValue && body!=null && body.simulated && body.bodyType==RigidbodyType2D.Dynamic)
                {
                    float away=Mathf.Sign(body.position.x-origin.Value.x); if(away==0) away=1;
                    body.AddForce(new Vector2(away*1.5f,.45f)*body.mass,ForceMode2D.Impulse);
                }
            }
            previousHealth=remaining;
        }
    }
}
