using UnityEngine;
namespace CreaJuego
{
    [CreateAssetMenu(menuName="CreaJuego/Perfil de animación")]
    public sealed class AnimationProfile : ScriptableObject
    {
        public string idle="Idle", move="Move", jump="Jump";
        public string attack="Attack";
        public float movementThreshold=.05f;
    }
}

