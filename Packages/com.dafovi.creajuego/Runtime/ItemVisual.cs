using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
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
        IVisualActionState action;
        GameItem item;
        Vector3 previous;
        int state;
        PlayableGraph graph;
        public static Color BaseColor(GameItem item) => item.SelectedAppearance!=null || item.customSprite!=null ? Color.white : item.tint;
        public int Facing => renderer!=null && (renderer.flipX ^ (item!=null && item.SelectedAppearance!=null && item.SelectedAppearance.flipX)) ? -1 : 1;
        public static SpriteRenderer Resolve(GameItem item)
        {
            var visual=item.GetComponent<ItemVisual>();
            return visual != null && visual.renderer != null ? visual.renderer : item.GetComponentInChildren<SpriteRenderer>(true);
        }
        void Start() { item=GetComponent<GameItem>(); Apply(); previous=transform.position; support=GetComponents<MonoBehaviour>().Where(component=>component.isActiveAndEnabled).OfType<IVisualMotionState>().FirstOrDefault(); action=GetComponents<MonoBehaviour>().Where(component=>component.isActiveAndEnabled).OfType<IVisualActionState>().FirstOrDefault(); }
        void OnDestroy() { if(graph.IsValid()) graph.Destroy(); }
        public void Apply()
        {
            if(item==null) item=GetComponent<GameItem>();
            if(renderer==null) renderer=GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
            if(geometrySource==null) geometrySource=renderer;
            if(item==null || renderer==null) return;
            if(graph.IsValid()) graph.Destroy();
            // Runtime authoring prefabs may retain the source kit renderer as a
            // geometry reference. Keep it for sizing, but show only CreaJuego's visual.
            renderer.enabled=true;
            if(geometrySource!=null && geometrySource!=renderer) geometrySource.enabled=false;
            var appearance=item.SelectedAppearance;
            if(animator!=null)
            {
                var controller=item.customSprite==null && appearance!=null ? appearance.controller : null;
                if(animator.runtimeAnimatorController!=controller){animator.runtimeAnimatorController=controller;animator.Rebind();if(animator.isActiveAndEnabled)animator.Update(0);}
                animator.enabled=controller!=null || item.customSprite==null && HasDirectClips(appearance);
            }
            // Animator.Rebind can restore values serialized in the source prefab.
            // The educational appearance must therefore be applied afterwards.
            if(item.customSprite!=null) renderer.sprite=item.customSprite;
            else if(appearance!=null) renderer.sprite=appearance.sprite;
            else if(geometrySource!=null) renderer.sprite=geometrySource.sprite;
            renderer.color=BaseColor(item);
            var visualAppearance=item.customSprite==null ? appearance : null;
            var size=geometrySource!=null && (item.stretchVisualToSurface || item.customSprite==null && (appearance==null || !appearance.preserveAspect)) ? geometrySource.size : Vector2.one;
            ApplyTransform(visualAppearance,size);
            renderer.flipX=visualAppearance!=null && visualAppearance.flipX;
            state=0;
        }
        void ApplyTransform(IAppearanceData visualAppearance,Vector2 size)
        {
            var sourceScale=visualAppearance!=null?visualAppearance.scale:Vector2.one;float educationalScale=Mathf.Clamp(item.visualScale,.1f,5f);
            float scaleX=sourceScale.x*educationalScale,scaleY=sourceScale.y*educationalScale;
            var offset=visualAppearance!=null?visualAppearance.offset:Vector2.zero;
            if(item.definition!=null&&item.definition.kind==ItemKind.Platform&&item.stretchVisualToSurface)
            {
                renderer.drawMode=SpriteDrawMode.Tiled;renderer.transform.localScale=new Vector3(scaleX,scaleY,1);
                renderer.size=new Vector2(size.x/Mathf.Max(.001f,Mathf.Abs(scaleX)),size.y/Mathf.Max(.001f,Mathf.Abs(scaleY)));
                if(renderer.sprite!=null)
                {
                    var pivot=new Vector2(renderer.sprite.pivot.x/renderer.sprite.rect.width,renderer.sprite.pivot.y/renderer.sprite.rect.height);
                    offset+=Vector2.Scale(pivot-new Vector2(.5f,.5f),size);
                }
            }
            else renderer.transform.localScale=new Vector3(scaleX*size.x,scaleY*size.y,1);
            renderer.transform.localPosition=offset;
        }        static bool HasDirectClips(IAppearanceData appearance)=>appearance!=null &&
            (appearance.idleClip!=null || appearance.moveClip!=null || appearance.jumpClip!=null || appearance.attackClip!=null);
        void PlayDirect(AnimationClip clip,int next)
        {
            if(clip==null || animator==null) return;
            if(graph.IsValid()) graph.Destroy();
            graph=PlayableGraph.Create("CreaJuego · "+clip.name);
            var output=AnimationPlayableOutput.Create(graph,"Apariencia",animator);
            var playable=AnimationClipPlayable.Create(graph,clip);
            playable.SetApplyFootIK(false);
            playable.SetTime(0);
            output.SetSourcePlayable(playable);
            graph.Play();
            state=next;
        }
        void LateUpdate()
        {
            if(renderer!=null) renderer.enabled=true;
            if(geometrySource!=null && geometrySource!=renderer) geometrySource.enabled=false;
            if(item==null || renderer==null || item.definition==null) return;
            var appearance=item.customSprite==null?item.SelectedAppearance:null;var size=geometrySource!=null&&(item.stretchVisualToSurface||item.customSprite==null&&(appearance==null||!appearance.preserveAspect))?geometrySource.size:Vector2.one;ApplyTransform(appearance,size);
            var body=GetComponent<Rigidbody2D>();
            float transformSpeed=(transform.position.x-previous.x)/Mathf.Max(Time.deltaTime,.0001f);
            float speed=body!=null && Mathf.Abs(body.linearVelocity.x)>.01f ? body.linearVelocity.x : transformSpeed;
            previous=transform.position;
            bool character=item.definition.kind==ItemKind.Player || item.definition.kind==ItemKind.Enemy;
            var selectedAppearance=item.SelectedAppearance;
            if(character && Mathf.Abs(speed)>.05f) renderer.flipX=(speed<0) ^ (selectedAppearance!=null && selectedAppearance.flipX);
            bool attacking=action!=null && action.IsVisuallyAttacking;
            if(item.customSprite==null && HasDirectClips(appearance))
            {
                int next=attacking && appearance.attackClip!=null ? 4 :
                    support!=null && !support.IsSupported && appearance.jumpClip!=null ? 3 :
                    Mathf.Abs(speed)>.05f && appearance.moveClip!=null ? 2 : 1;
                var clip=next==4 ? appearance.attackClip : next==3 ? appearance.jumpClip : next==2 ? appearance.moveClip : appearance.idleClip;
                if(clip==null) clip=appearance.idleClip ?? appearance.moveClip ?? appearance.jumpClip ?? appearance.attackClip;
                if(next!=state) PlayDirect(clip,next);
                return;
            }
            var profile=selectedAppearance!=null ? selectedAppearance.animationProfile : null;
            if(animator==null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController==null || profile==null) return;
            string named=attacking && !string.IsNullOrEmpty(profile.attack) && animator.HasState(0,Animator.StringToHash(profile.attack)) ? profile.attack :
                support!=null && !support.IsSupported ? profile.jump : Mathf.Abs(speed)>profile.movementThreshold ? profile.move : profile.idle;
            if(string.IsNullOrEmpty(named)) return;
            int hash=Animator.StringToHash(named);
            if(hash!=state && animator.HasState(0,hash)) { animator.Play(hash); state=hash; }
        }
    }
}
