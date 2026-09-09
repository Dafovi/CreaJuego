using UnityEngine;
namespace CreaJuego
{
    [CreateAssetMenu(menuName = "CreaJuego/Pack del taller")]
    public sealed class ContentPackDefinition : ScriptableObject
    {
        public string id;
        public AppearanceDefinition[] appearances = System.Array.Empty<AppearanceDefinition>();
        public AppearanceDefinition DefaultFor(ItemKind kind) => System.Array.Find(appearances, a => a != null && a.kind == kind);
        public GameObject sceneServices;
    }
}


