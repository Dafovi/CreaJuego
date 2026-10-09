using System;
using System.Linq;
using UnityEngine;

namespace CreaJuego.Web
{
    public static class CreaJuegoBranding
    {
        public const string ProductName = "Gamer";
        public const string Tagline = "Crea tu propia aventura";
        public static string FullTitle => ProductName + ": " + Tagline;
    }

    [Serializable]
    public sealed class RuntimeAppearanceDefault { public ItemKind kind; public string appearanceId; }

    [Serializable]
    public sealed class RuntimeGameTypeDefinition
    {
        public string id,displayName,description,learningHint;
        public bool available;
    }

    public static class RuntimeGameTypeCatalog
    {
        static readonly RuntimeGameTypeDefinition[] defaults={
            new RuntimeGameTypeDefinition{id="platformer",displayName="Juego de plataformas",description="Crea caminos, saltos, premios, peligros y una meta.",learningHint="Empieza con un nivel vacío y construye el recorrido a tu manera.",available=true},
            new RuntimeGameTypeDefinition{id="catch-and-dodge",displayName="Atrapa y esquiva",description="Muévete de lado a lado, recoge lo bueno que cae y evita los peligros.",learningHint="Añade un personaje, premios y peligros. Su posición marca desde dónde caerán.",available=true}
        };
        public static RuntimeGameTypeDefinition[] Defaults=>defaults;
    }

    [CreateAssetMenu(menuName="CreaJuego/Web/Pack preparado")]
    public sealed class RuntimeContentPack:ScriptableObject
    {
        public string id;
        public GameItemDefinition[] definitions=Array.Empty<GameItemDefinition>();
        public ContentPackDefinition preparedAppearances;
        public RuntimeTilePaletteDefinition tilePalette;
        public RuntimeAppearanceDefault[] defaults=Array.Empty<RuntimeAppearanceDefault>();
        [Header("Tipos de juego")]
        public RuntimeGameTypeDefinition[] gameTypes=Array.Empty<RuntimeGameTypeDefinition>();
        public GameObject sceneServices;
        [Header("Marca")]
        public string productName=CreaJuegoBranding.ProductName;
        public string tagline=CreaJuegoBranding.Tagline;
        public Sprite brandIcon;
        public RuntimeGameTypeDefinition[] GameTypes=>gameTypes!=null&&gameTypes.Length>0?gameTypes:RuntimeGameTypeCatalog.Defaults;
        public GameItemDefinition Find(string definitionId)=>definitions.FirstOrDefault(d=>d!=null&&d.id==definitionId);
        public AppearanceCategory CategoryFor(ItemKind kind)=>preparedAppearances!=null?preparedAppearances.CategoryFor(kind):null;
        public AppearanceOption[] OptionsFor(ItemKind kind,string search=null,string definitionId=null)
        {
            var category=CategoryFor(kind);if(category==null)return Array.Empty<AppearanceOption>();
            return category.options.Where(o=>o!=null&&o.Preview!=null&&(o.definitionIds==null||o.definitionIds.Length==0||string.IsNullOrEmpty(definitionId)||o.definitionIds.Contains(definitionId))&&(string.IsNullOrWhiteSpace(search)||o.displayName.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
        }
        public string DefaultFor(ItemKind kind)
        {
            var options=OptionsFor(kind);
            if(kind==ItemKind.Player)
            {
                var gino=options.FirstOrDefault(o=>o.id=="platformer-kit-gino" || o.displayName=="Gino" || o.idleClip!=null&&o.idleClip.name=="Gino-Idle");
                if(gino!=null)return gino.id;
            }
            if(kind==ItemKind.Enemy)
            {
                var scarecrow=options.FirstOrDefault(o=>o.id=="platformer-kit-scarecrow");
                if(scarecrow!=null)return scarecrow.id;
            }
            return defaults.FirstOrDefault(d=>d.kind==kind)?.appearanceId??CategoryFor(kind)?.Default?.id??options.FirstOrDefault()?.id??"";
        }
    }
}
