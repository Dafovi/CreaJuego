using System;
using System.Linq;
using UnityEngine;

namespace CreaJuego.Web
{
    [Serializable]
    public sealed class RuntimeAppearanceDefault { public ItemKind kind; public string appearanceId; }

    [CreateAssetMenu(menuName="CreaJuego/Web/Pack preparado")]
    public sealed class RuntimeContentPack:ScriptableObject
    {
        public string id;
        public GameItemDefinition[] definitions=Array.Empty<GameItemDefinition>();
        public ContentPackDefinition preparedAppearances;
        public RuntimeAppearanceDefault[] defaults=Array.Empty<RuntimeAppearanceDefault>();
        public GameObject sceneServices;
        public GameItemDefinition Find(string definitionId)=>definitions.FirstOrDefault(d=>d!=null&&d.id==definitionId);
        public AppearanceCategory CategoryFor(ItemKind kind)=>preparedAppearances!=null?preparedAppearances.CategoryFor(kind):null;
        public AppearanceOption[] OptionsFor(ItemKind kind,string search=null)
        {
            var category=CategoryFor(kind);if(category==null)return Array.Empty<AppearanceOption>();
            return category.options.Where(o=>o!=null&&o.Preview!=null&&(string.IsNullOrWhiteSpace(search)||o.displayName.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
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