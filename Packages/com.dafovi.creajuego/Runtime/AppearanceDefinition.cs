using UnityEngine;
namespace CreaJuego
{
    [CreateAssetMenu(menuName="CreaJuego/Apariencia")]
    public sealed class AppearanceDefinition : ScriptableObject
    {
        public string id, displayName;
        public ItemKind kind;
        public Sprite sprite;
        public RuntimeAnimatorController controller;
        public AnimationProfile animationProfile;
        public Vector2 scale = Vector2.one, offset;
        public bool flipX;
    }
    public interface IVisualMotionState { bool IsSupported { get; } }
}
