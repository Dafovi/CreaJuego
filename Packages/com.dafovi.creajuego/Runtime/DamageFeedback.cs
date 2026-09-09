using UnityEngine;
namespace CreaJuego
{
    [DisallowMultipleComponent]
    public sealed class DamageFeedback : MonoBehaviour
    {
        SpriteRenderer target;
        Color original;
        float until;
        public bool IsFlashing { get; private set; }
        public void Flash()
        {
            var item=GetComponent<GameItem>(); if(item==null) return;
            var next=ItemVisual.Resolve(item); if(next==null) return;
            if(!IsFlashing || next!=target) { Restore(); target=next; original=target.color; }
            IsFlashing=true; until=Time.time+.15f; target.color=new Color(1,.2f,.2f,original.a);
        }
        void Update() { if(IsFlashing && Time.time>=until) Restore(); }
        void OnDisable()=>Restore();
        void Restore() { if(IsFlashing && target!=null) target.color=original; IsFlashing=false; }
    }
}
