using UnityEngine;
namespace CreaJuego
{
    [CreateAssetMenu(menuName = "CreaJuego/Pack del taller")]
    public sealed class ContentPackDefinition : ScriptableObject
    {
        public string id;
        public GameObject sceneServices;
    }
}

