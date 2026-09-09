using System.IO;
using System.Linq;
using CreaJuego.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace CreaJuego.Starter.Editor
{
    public static class AppearancePackBuilder
    {
        const string Root="Assets/CreaJuegoPacks/MundoMisterioso";
        static T Asset<T>(string name) where T:ScriptableObject
        {
            string path=Root+"/"+name+".asset";
            var value=AssetDatabase.LoadAssetAtPath<T>(path);
            if(value==null) { value=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value,path); }
            return value;
        }
        [MenuItem("CreaJuego/Contenido/Preparar Mundo misterioso")]
        public static void Run()
        {
            foreach(var path in Directory.GetFiles(Root+"/Art","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=Path.GetFileName(path).StartsWith("pp_") ? 18 : 16; importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false; importer.SaveAndReimport();
            }
            var profile=Asset<AnimationProfile>("Movimiento suave");
            string controllerPath=Root+"/Personaje.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                foreach(string name in new[]{"Idle","Move","Jump"})
                {
                    var clip=new AnimationClip { name=name, frameRate=12 };
                    float lift=name=="Move" ? .06f : name=="Idle" ? .02f : .08f;
                    clip.SetCurve("",typeof(Transform),"localPosition.y",new AnimationCurve(new Keyframe(0,0),new Keyframe(.25f,lift),new Keyframe(.5f,0)));
                    var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true; AnimationUtility.SetAnimationClipSettings(clip,settings);
                    AssetDatabase.CreateAsset(clip,Root+"/"+name+".anim");
                    controller.layers[0].stateMachine.AddState(name).motion=clip;
                }
            }
            var pack=Asset<ContentPackDefinition>("Mundo misterioso"); pack.id="mundo-misterioso";
            var entries=new[]{
                (84,"Brujita",ItemKind.Player),(85,"Exploradora",ItemKind.Player),
                (36,"Piedra clara",ItemKind.Platform),(37,"Piedra antigua",ItemKind.Platform),(38,"Piedra oscura",ItemKind.Platform),
                (115,"Moneda dorada",ItemKind.Prize),(116,"Gema azul",ItemKind.Prize),(1027,"Llave dorada",ItemKind.Prize),
                (103,"Pinchos",ItemKind.Hazard),(104,"Trampa roja",ItemKind.Hazard),
                (21,"Puerta antigua",ItemKind.Goal),(33,"Puerta misteriosa",ItemKind.Goal),
                (120,"Murciélago",ItemKind.Enemy),(121,"Fantasma",ItemKind.Enemy),(122,"Escarabajo",ItemKind.Enemy),(123,"Seta",ItemKind.Enemy),
                (2072,"Mesa",ItemKind.Decoration),(2073,"Taburete",ItemKind.Decoration),(2074,"Cuenco",ItemKind.Decoration),
                (2065,"Lápida",ItemKind.Decoration),(2064,"Estatua",ItemKind.Decoration),(2089,"Cofre cerrado",ItemKind.Decoration),
                (2090,"Cofre abierto",ItemKind.Decoration),(2091,"Cofre alto",ItemKind.Decoration),
                (3124,"Hierba",ItemKind.Decoration),(3125,"Planta alta",ItemKind.Decoration),(3126,"Pino",ItemKind.Decoration),
                (3127,"Cactus",ItemKind.Decoration),(3128,"Hongo pequeño",ItemKind.Decoration),(3129,"Hongo alto",ItemKind.Decoration)};
            pack.appearances=entries.Select(e=>{
                var a=Asset<AppearanceDefinition>("apariencia-"+e.Item1); a.id="tiny-dungeon-"+e.Item1; a.displayName=e.Item2; a.kind=e.Item3;
                                int tile=e.Item1; bool platformer=false;
                switch(tile) { case 115: tile=151; platformer=true; break; case 116: tile=67; platformer=true; break; case 103: tile=68; platformer=true; break; case 104: tile=12; platformer=true; break; case 1027: tile=27; platformer=true; break; }
                if(e.Item1>=3000) { tile=e.Item1-3000; platformer=true; } else if(e.Item1>=2000) tile=e.Item1-2000;
                a.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/"+(platformer?"pp_":"tile_")+tile.ToString("D4")+".png");
                a.scale=Vector2.one;
                a.controller=e.Item1==84 ? controller : null; a.animationProfile=e.Item1==84 ? profile : null;
                EditorUtility.SetDirty(a); return a;
            }).ToArray(); EditorUtility.SetDirty(pack);
            foreach(var definition in ItemService.Catalog())
            {
                var appearance=pack.DefaultFor(definition.kind); if(appearance==null) continue;
                definition.appearancePack=pack; EditorUtility.SetDirty(definition);
                string path=AssetDatabase.GetAssetPath(definition.prefab);
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var item=root.GetComponent<GameItem>();
                    var visual=root.GetComponent<ItemVisual>();
                    if(visual==null)
                    {
                        visual=root.AddComponent<ItemVisual>();
                        var child=new GameObject("Visual"); child.transform.SetParent(root.transform,false);
                        visual.renderer=child.AddComponent<SpriteRenderer>(); visual.animator=child.AddComponent<Animator>();
                        var legacy=root.GetComponent<SpriteRenderer>();
                        if(legacy!=null) { visual.renderer.sortingOrder=legacy.sortingOrder; visual.renderer.sortingLayerID=legacy.sortingLayerID; legacy.enabled=false; }
                    }
                    visual.geometrySource=root.GetComponent<SpriteRenderer>();
                    if(item.appearance==null) item.appearance=appearance;
                    visual.Apply(); PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets(); Debug.Log("Mundo misterioso: apariencias preparadas, física conservada.");
        }
    }
}


