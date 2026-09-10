using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CreaJuego.Starter.Editor
{
    public static class ExternalAppearanceIntegration
    {
        public const string GinoPrefabPath="Assets/Plugins/Platformer Game Kit/Prefabs/Player/Player.prefab";
        const string GinoArt="Assets/Plugins/Platformer Game Kit/Art/Gino/";

        [MenuItem("CreaJuego/Contenido/Integrar Gino como apariencia")]
        public static void IntegrateGino()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(GinoPrefabPath);
            if(prefab==null) throw new InvalidOperationException("No se encontró el prefab de Gino. Importa Platformer Game Kit primero.");
            var player=AssetDatabase.FindAssets("t:GameItemDefinition")
                .Select(g=>AssetDatabase.LoadAssetAtPath<GameItemDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(d=>d!=null && d.kind==ItemKind.Player);
            var category=player?.appearancePack?.CategoryFor(ItemKind.Player);
            if(category==null) throw new InvalidOperationException("No está configurada la lista de apariencias de Jugador.");
            var option=category.options.FirstOrDefault(o=>o!=null && o.prefab==prefab) ?? category.Find("platformer-kit-gino");
            if(option==null) { option=new AppearanceOption{id="platformer-kit-gino"}; category.options.Add(option); }
            category.options.RemoveAll(o=>o!=null && o!=option && (o.id=="platformer-kit-gino" || o.prefab==prefab));
            option.displayName="Gino";
            option.sprite=null;
            option.prefab=prefab;
            option.controller=null;
            option.animationProfile=null;
            option.idleClip=Clip("Gino-Idle");
            option.moveClip=Clip("Gino-Run");
            option.jumpClip=Clip("Gino-Jump-Loop");
            option.attackClip=Clip("Gino-Attack1");
            option.scale=new Vector2(.25f,.25f);
            option.offset=new Vector2(-.18f,-.45f);
            option.flipX=false;
            category.EnsureIds();
            EditorUtility.SetDirty(category);
            AssetDatabase.SaveAssets();
            Selection.activeObject=category;
            EditorGUIUtility.PingObject(category);
            Debug.Log("Gino está disponible en CreaJuego > Jugador > Apariencia. Playground conserva el control y la física.");
        }

        static AnimationClip Clip(string name)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(GinoArt+name+".anim");
            if(clip==null) throw new InvalidOperationException("Falta la animación "+name+" del Platformer Game Kit.");
            return clip;
        }
    }
}
