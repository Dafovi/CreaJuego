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
        SpriteRenderer feedback;
        Sprite feedbackSprite;
        public bool IsAttacking => Time.time<activeUntil;
        public bool IsVisuallyAttacking => IsAttacking;
        public int AttackCount { get; private set; }
        public Vector2 LastCenter { get; private set; }
        readonly HashSet<EnemyVitality> hit=new HashSet<EnemyVitality>();
        void Update() { if(pressed!=null && pressed()) TryAttack(); if(feedback!=null)feedback.enabled=IsAttacking; }
        void OnDestroy(){if(feedbackSprite!=null)Destroy(feedbackSprite.texture);if(feedbackSprite!=null)Destroy(feedbackSprite);}
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
            ShowAttackFeedback(facing);
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
        void ShowAttackFeedback(int facing)
        {
            if(feedback==null)
            {
                var effect=new GameObject("Golpe visible",typeof(SpriteRenderer));effect.transform.SetParent(transform,false);feedback=effect.GetComponent<SpriteRenderer>();feedbackSprite=CreateFeedbackSprite();feedback.sprite=feedbackSprite;feedback.color=new Color(1f,.9f,.25f,.95f);feedback.sortingOrder=120;
            }
            feedback.transform.localPosition=new Vector3(.65f*facing,.05f,-.2f);feedback.transform.localScale=new Vector3(.55f*facing,.55f,1);feedback.enabled=true;
        }
        static Sprite CreateFeedbackSprite()
        {
            const int size=24;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,name="Golpe de CreaJuego"};var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=x-4,dy=y-size*.5f;float radius=Mathf.Sqrt(dx*dx+dy*dy);float angle=Mathf.Abs(Mathf.Atan2(dy,Mathf.Max(.01f,dx)))*Mathf.Rad2Deg;
                bool arc=radius>8&&radius<11&&angle<58;bool spark=x>13&&((Mathf.Abs(y-12)<2)||(Mathf.Abs((y-12)-(x-15))<1));pixels[y*size+x]=arc||spark?Color.white:Color.clear;
            }
            texture.SetPixels(pixels);texture.Apply();return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.25f,.5f),size);
        }
    }
}
