using System;
using System.Collections.Generic;
using UnityEngine;
namespace CreaJuego
{
    public interface IAppearanceData
    {
        string id {get;}
        string displayName {get;}
        Sprite sprite {get;}
        RuntimeAnimatorController controller {get;}
        AnimationProfile animationProfile {get;}
        AnimationClip idleClip {get;}
        AnimationClip moveClip {get;}
        AnimationClip jumpClip {get;}
        AnimationClip attackClip {get;}
        Vector2 scale {get;}
        Vector2 offset {get;}
        bool flipX {get;}
    }
    [Serializable]
    public sealed class AppearanceOption : IAppearanceData
    {
        [HideInInspector] public string id;
        [InspectorName("Nombre")] public string displayName;
        [InspectorName("Sprite")] public Sprite sprite;
        [InspectorName("Prefab de referencia visual")]
        [Tooltip("Usa su SpriteRenderer y Animator. No copia scripts, colisiones ni jerarquías. Un Sprite asignado tiene prioridad.")]
        public GameObject prefab;
        [InspectorName("Animación (opcional)")] public RuntimeAnimatorController controller;
        [InspectorName("Estados de animación (opcional)")] public AnimationProfile animationProfile;
        [Header("Clips sencillos (opcional)")]
        [InspectorName("Reposo")] public AnimationClip idleClip;
        [InspectorName("Movimiento")] public AnimationClip moveClip;
        [InspectorName("Salto / en el aire")] public AnimationClip jumpClip;
        [InspectorName("Ataque")] public AnimationClip attackClip;
        [InspectorName("Escala")] public Vector2 scale=Vector2.one;
        [InspectorName("Desplazamiento")] public Vector2 offset;
        [InspectorName("Voltear horizontalmente")] public bool flipX;
        string IAppearanceData.id=>id;
        string IAppearanceData.displayName=>displayName;
        public string ValidationError {
            get {
                if(sprite!=null) return null;
                if(prefab==null) return "Asigna un Sprite o un prefab.";
                var renderers=prefab.GetComponentsInChildren<SpriteRenderer>(true);
                if(renderers.Length!=1) return "El prefab debe contener exactamente un SpriteRenderer. Para un conjunto de piezas, elige su sprite principal.";
                if(renderers[0].sprite==null) return "El prefab no tiene un sprite asignado.";
                return null;
            }
        }
        public Sprite Preview=>sprite!=null ? sprite : ValidationError==null ? Array.Find(prefab.GetComponentsInChildren<SpriteRenderer>(true),r=>r.sprite!=null).sprite : null;
        Sprite IAppearanceData.sprite=>Preview;
        RuntimeAnimatorController IAppearanceData.controller=>controller!=null ? controller : sprite==null && prefab!=null ? prefab.GetComponentInChildren<Animator>(true)?.runtimeAnimatorController : null;
        AnimationProfile IAppearanceData.animationProfile=>animationProfile;
        AnimationClip IAppearanceData.idleClip=>idleClip;
        AnimationClip IAppearanceData.moveClip=>moveClip;
        AnimationClip IAppearanceData.jumpClip=>jumpClip;
        AnimationClip IAppearanceData.attackClip=>attackClip;
        Vector2 IAppearanceData.scale=>scale;
        Vector2 IAppearanceData.offset=>offset;
        bool IAppearanceData.flipX=>flipX;
    }
    [CreateAssetMenu(menuName="CreaJuego/Lista de apariencias por categoría",fileName="Apariencias")]
    public sealed class AppearanceCategory : ScriptableObject
    {
        [InspectorName("Categoría")] public ItemKind kind;
        [InspectorName("Opciones")] public List<AppearanceOption> options=new List<AppearanceOption>();
        public AppearanceOption Find(string id)=>options.Find(a=>a!=null && a.id==id);
        // Stable keys keep scene selections intact when the list is reordered or renamed.
        public void EnsureIds()
        {
            var used=new HashSet<string>();
            foreach(var option in options) {
                if(option==null) continue;
                if(string.IsNullOrWhiteSpace(option.id) || !used.Add(option.id)) {
                    option.id=Guid.NewGuid().ToString("N"); used.Add(option.id);
                }
            }
        }
        private void OnValidate()=>EnsureIds();
    }
}

