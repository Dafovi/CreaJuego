using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CreaJuego.Editor
{
    public static class AppearanceCategoryMigration
    {
        public static string Label(ItemKind kind)
        {
            switch(kind) {
                case ItemKind.Player:return "Jugador";
                case ItemKind.Platform:return "Plataformas";
                case ItemKind.MovingPlatform:return "Plataformas móviles";
                case ItemKind.Enemy:return "Enemigos";
                case ItemKind.Prize:return "Premios";
                case ItemKind.Hazard:return "Peligros";
                case ItemKind.Goal:return "Metas";
                case ItemKind.Decoration:return "Decoración";
                default:return "Fondos";
            }
        }
        [MenuItem("CreaJuego/Contenido/Organizar listas por categoría")]
        public static void Run()
        {
            foreach(var guid in AssetDatabase.FindAssets("t:ContentPackDefinition")) {
                var path=AssetDatabase.GUIDToAssetPath(guid);
                var pack=AssetDatabase.LoadAssetAtPath<ContentPackDefinition>(path);
                if(pack.appearances.Length==0) continue;
                string parent=Path.GetDirectoryName(path).Replace('\\','/');
                string folder=parent+"/Categorías";
                if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent,"Categorías");
                var categories=pack.categories.Where(c=>c!=null).ToList();
                foreach(ItemKind kind in Enum.GetValues(typeof(ItemKind))) {
                    if(categories.Any(c=>c.kind==kind)) continue; // Never overwrite manually edited lists.
                    var category=ScriptableObject.CreateInstance<AppearanceCategory>();
                    category.kind=kind;
                    var sources=pack.appearances.Where(a=>a!=null && a.kind==(kind==ItemKind.MovingPlatform ? ItemKind.Platform : kind));
                    foreach(var a in sources) category.options.Add(new AppearanceOption{id=a.id,displayName=a.displayName,sprite=a.sprite,controller=a.controller,animationProfile=a.animationProfile,scale=a.scale,offset=a.offset,flipX=a.flipX});
                    category.EnsureIds();
                    AssetDatabase.CreateAsset(category,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+Label(kind)+".asset"));
                    categories.Add(category);
                }
                pack.categories=categories.ToArray(); EditorUtility.SetDirty(pack);
                string legacy=parent+"/Compatibilidad";
                if(!AssetDatabase.IsValidFolder(legacy)) AssetDatabase.CreateFolder(parent,"Compatibilidad");
                foreach(var appearance in pack.appearances.Where(a=>a!=null)) {
                    string old=AssetDatabase.GetAssetPath(appearance);
                    if(Path.GetDirectoryName(old).Replace('\\','/')!=parent) continue;
                    string error=AssetDatabase.MoveAsset(old,legacy+"/"+Path.GetFileName(old));
                    if(!string.IsNullOrEmpty(error)) Debug.LogError(error);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Listas por categoría preparadas. Las referencias de escenas anteriores se conservan.");
        }
    }
}
