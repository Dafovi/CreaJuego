using System;
using System.Collections.Generic;
using UnityEngine;
namespace CreaJuego
{
    [DisallowMultipleComponent]
    public sealed class PlayerAttack : MonoBehaviour, IVisualActionState
    {
        public Transform attackPoint;
        public Func<bool> pressed, playing;
        public const float Cooldown=.35f;
        float readyAt, activeUntil;
        public bool IsAttacking => Time.time<activeUntil;
        public bool IsVisuallyAttacking => IsAttacking;
        public int AttackCount { get; private set; }
        public Vector2 LastCenter { get; private set; }
        readonly HashSet<EnemyVitality> hit=new HashSet<EnemyVitality>();
        void Update() { if(pressed!=null && pressed()) TryAttack(); }
        public bool TryAttack()
        {
            var item=GetComponent<GameItem>();
            if(!isActiveAndEnabled || item==null || !item.canAttack || playing==null || !playing() || Time.time<readyAt) return false;
            float visualDuration=item.SelectedAppearance?.attackClip!=null ? item.SelectedAppearance.attackClip.length : .12f;
            readyAt=Time.time+Mathf.Max(Cooldown,visualDuration); activeUntil=Time.time+Mathf.Max(.12f,visualDuration); AttackCount++;
            var visual=GetComponent<ItemVisual>(); var sprite=ItemVisual.Resolve(item);
            int facing=visual!=null ? visual.Facing : sprite!=null && sprite.flipX ? -1 : 1;
            LastCenter=(Vector2)transform.position+Vector2.right*(.65f*facing);
            if(attackPoint!=null) attackPoint.position=new Vector3(LastCenter.x,LastCenter.y,transform.position.z);
            hit.Clear();
            // One instantaneous query per press: each enemy can receive at most one hit.
            foreach(var collider in Physics2D.OverlapBoxAll(LastCenter,new Vector2(.8f,.8f),0))
            {
                if(collider.gameObject.scene!=gameObject.scene) continue;
                var enemy=collider.GetComponentInParent<EnemyVitality>();
                if(enemy!=null && hit.Add(enemy)) enemy.TryReceive(Mathf.Clamp(item.attackDamage,1,5));
            }
            return true;
        }
    }
}
