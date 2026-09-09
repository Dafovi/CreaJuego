using UnityEngine;
namespace CreaJuego
{
    // Compatibility only: new content is authored inline in AppearanceCategory.
    public sealed class AppearanceDefinition : ScriptableObject, IAppearanceData
    {
        public string id, displayName;
        public ItemKind kind;
        public Sprite sprite;
        public RuntimeAnimatorController controller;
        public AnimationProfile animationProfile;
        public Vector2 scale = Vector2.one, offset;
        public bool flipX;
        string IAppearanceData.id=>id;
        string IAppearanceData.displayName=>displayName;
        Sprite IAppearanceData.sprite=>sprite;
        RuntimeAnimatorController IAppearanceData.controller=>controller;
        AnimationProfile IAppearanceData.animationProfile=>animationProfile;
        Vector2 IAppearanceData.scale=>scale;
        Vector2 IAppearanceData.offset=>offset;
        bool IAppearanceData.flipX=>flipX;
    }
    public interface IVisualMotionState { bool IsSupported { get; } }
}

