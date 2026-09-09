using UnityEngine;
namespace CreaJuego
{
    [CreateAssetMenu(menuName = "CreaJuego/Pack del taller")]
    public sealed class ContentPackDefinition : ScriptableObject
    {
        public string id;
        [InspectorName("Listas por categoría")] public AppearanceCategory[] categories=System.Array.Empty<AppearanceCategory>();
        public AppearanceCategory CategoryFor(ItemKind kind)=>System.Array.Find(categories,c=>c!=null && c.kind==kind);
        [HideInInspector] public AppearanceDefinition[] appearances = System.Array.Empty<AppearanceDefinition>();
        public AppearanceDefinition DefaultFor(ItemKind kind) => System.Array.Find(appearances, a => a != null && a.kind == kind);
        public GameObject sceneServices;
    }
}



