using System.Linq;
using UnityEngine;
namespace CreaJuego
{
    // Only the child Visual is authored here. Physics and gameplay remain on the root.
    [DisallowMultipleComponent]
    public sealed class ItemVisual : MonoBehaviour
    {
        public SpriteRenderer renderer;
        public Animator animator;
        public SpriteRenderer geometrySource;
        IVisualMotionState support;
        GameItem item;
        Vector3 previous;
        int state;
        public static Color BaseColor(GameItem item) => item.SelectedAppearance!=null || item.customSprite!=null ? Color.white : item.tint;
        public int Facing => renderer!=null && (renderer.flipX ^ (item!=null && item.SelectedAppearance!=null && item.SelectedAppearance.flipX)) ? -1 : 1;
        public static SpriteRenderer Resolve(GameItem item)
        {
            var visual=item.GetComponent<ItemVisual>();
            return visual != null && visual.renderer != null ? visual.renderer : item.GetComponent<SpriteRenderer>();
        }
        void Start() { item=GetComponent<GameItem>(); Apply(); previous=transform.position; support=GetComponents<MonoBehaviour>().OfType<IVisualMotionState>().FirstOrDefault(); }
        public void Apply()
        {
            if(item==null) item=GetComponent<GameItem>();
            if(item==null || renderer==null) return;
            var appearance=item.SelectedAppearance;
            if(item.customSprite!=null) renderer.sprite=item.customSprite;
            else if(appearance!=null) renderer.sprite=appearance.sprite;
            else if(geometrySource!=null) renderer.sprite=geometrySource.sprite;
            renderer.color=BaseColor(item);
            if(appearance!=null)
            {
                var size=geometrySource!=null ? geometrySource.size : Vector2.one;
                renderer.transform.localScale=new Vector3(appearance.scale.x*size.x,appearance.scale.y*size.y,1);
                renderer.transform.localPosition=appearance.offset;
                renderer.flipX=appearance.flipX;
            }
            if(animator!=null)
            {
                var controller=item.customSprite==null && appearance!=null ? appearance.controller : null;
                if(animator.runtimeAnimatorController!=controller) animator.runtimeAnimatorController=controller;
                animator.enabled=controller!=null;
            }
            state=0;
        }
        void LateUpdate()
        {
            if(item==null || renderer==null || item.definition==null) return;
            float speed=(transform.position.x-previous.x)/Mathf.Max(Time.deltaTime,.0001f);
            previous=transform.position;
            bool character=item.definition.kind==ItemKind.Player || item.definition.kind==ItemKind.Enemy;
            var appearance=item.SelectedAppearance;
            if(character && Mathf.Abs(speed)>.05f) renderer.flipX=(speed<0) ^ (appearance!=null && appearance.flipX);
            var profile=appearance!=null ? appearance.animationProfile : null;
            if(animator==null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController==null || profile==null) return;
            
            var attack=GetComponent<PlayerAttack>();
            string next=attack!=null && attack.IsAttacking && !string.IsNullOrEmpty(profile.attack) && animator.HasState(0,Animator.StringToHash(profile.attack)) ? profile.attack : support!=null && !support.IsSupported ? profile.jump : Mathf.Abs(speed)>profile.movementThreshold ? profile.move : profile.idle;
            if(string.IsNullOrEmpty(next)) return;
            int hash=Animator.StringToHash(next);
            if(hash!=state && animator.HasState(0,hash)) { animator.Play(hash); state=hash; }
        }
    }
}



