using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CreaJuego.Starter.Editor
{
    public static class ExternalAppearanceIntegration
    {
        const string KitRoot="Assets/Plugins/Platformer Game Kit/";
        public const string GinoPrefabPath=KitRoot+"Prefabs/Player/Player.prefab";
        public const string EnemyPrefabRoot=KitRoot+"Prefabs/Enemies/";

        [MenuItem("CreaJuego/Contenido/Integrar Gino como apariencia")]
        public static void IntegrateGino()
        {
            var definition=Definition(ItemKind.Player);
            var category=Category(definition);
            Configure(category,definition,"platformer-kit-gino","Gino",GinoPrefabPath,"Gino",
                "Gino-Idle","Gino-Run","Gino-Jump-Loop","Gino-Attack1");
            Finish(category,"Gino está disponible en CreaJuego > Jugador > Apariencia. Playground conserva el control y la física.");
        }

        [MenuItem("CreaJuego/Contenido/Integrar enemigos de Platformer Game Kit")]
        public static void IntegrateEnemies()
        {
            var enemy=Definition(ItemKind.Enemy);
            var enemies=Category(enemy);
            Configure(enemies,enemy,"platformer-kit-gobbat","Gobbat",EnemyPrefabRoot+"Gobbat.prefab","Gobbat",
                "Gobbat-Fly","Gobbat-Fly",null,"Gobbat-Attack");
            Configure(enemies,enemy,"platformer-kit-gobbler","Gobbler",EnemyPrefabRoot+"Gobbler.prefab","Gobbler",
                "Gobbler-Idle","Gobbler-Walk",null,"Gobbler-Attack");
            Configure(enemies,enemy,"platformer-kit-mawflower","Flor carnívora",EnemyPrefabRoot+"MawFlower.prefab","MawFlower",
                "MawFlower-Idle","MawFlower-Idle",null,"MawFlower-Attack-Right");
            Configure(enemies,enemy,"platformer-kit-naga","Naga",EnemyPrefabRoot+"Naga.prefab","Naga",
                "Naga-Idle","Naga-Walk",null,"Naga-Attack");
            Configure(enemies,enemy,"platformer-kit-scarecrow","Espantapájaros",EnemyPrefabRoot+"Scarecrow.prefab","Scarecrow",
                "Scarecrow-Idle","Scarecrow-Walk","Scarecrow-Jump-Loop","Scarecrow-Attack");

            var hazard=Definition(ItemKind.Hazard);
            var hazards=Category(hazard);
            Configure(hazards,hazard,"platformer-kit-spikes","Pinchos del kit",EnemyPrefabRoot+"Spikes.prefab",null,
                null,null,null,null);
            Finish(new[]{enemies,hazards},"Cinco enemigos animados y los pinchos están disponibles. CreaJuego conserva patrullaje, daño, vida y físicas.");
        }

        static GameItemDefinition Definition(ItemKind kind)
        {
            var definition=AssetDatabase.FindAssets("t:GameItemDefinition")
                .Select(g=>AssetDatabase.LoadAssetAtPath<GameItemDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(d=>d!=null && d.kind==kind && d.appearancePack!=null);
            if(definition==null) throw new InvalidOperationException("No está configurada la definición de "+kind+".");
            return definition;
        }

        static AppearanceCategory Category(GameItemDefinition definition)
        {
            var category=definition.appearancePack.CategoryFor(definition.kind);
            if(category==null) throw new InvalidOperationException("No está configurada la lista de apariencias de "+definition.displayName+".");
            return category;
        }

        static void Configure(AppearanceCategory category,GameItemDefinition definition,string id,string displayName,
            string prefabPath,string artFolder,string idle,string move,string jump,string attack)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(prefab==null) throw new InvalidOperationException("No se encontró "+prefabPath+". Importa Platformer Game Kit primero.");
            var option=category.options.FirstOrDefault(o=>o!=null && o.prefab==prefab) ?? category.Find(id);
            if(option==null) { option=new AppearanceOption{id=id}; category.options.Add(option); }
            category.options.RemoveAll(o=>o!=null && o!=option && (o.id==id || o.prefab==prefab));
            option.displayName=displayName;
            option.sprite=null;
            option.prefab=prefab;
            option.controller=null;
            option.animationProfile=null;
            option.idleClip=Clip(artFolder,idle);
            option.moveClip=Clip(artFolder,move);
            option.jumpClip=Clip(artFolder,jump);
            option.attackClip=Clip(artFolder,attack);
            FitToEducationalCollider(option,definition);
            option.flipX=false;
            category.EnsureIds();
            EditorUtility.SetDirty(category);
        }

        static AnimationClip Clip(string folder,string name)
        {
            if(string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(name)) return null;
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(KitRoot+"Art/"+folder+"/"+name+".anim");
            if(clip==null) throw new InvalidOperationException("Falta la animación "+name+" del Platformer Game Kit.");
            return clip;
        }

        static void FitToEducationalCollider(AppearanceOption option,GameItemDefinition definition)
        {
            var preview=option.Preview;
            var collider=definition.prefab!=null ? definition.prefab.GetComponent<BoxCollider2D>() : null;
            if(preview==null || collider==null || preview.bounds.size.y<=0) { option.scale=Vector2.one; option.offset=Vector2.zero; return; }
            float scale=collider.size.y/preview.bounds.size.y;
            option.scale=Vector2.one*scale;
            option.offset=new Vector2(collider.offset.x-preview.bounds.center.x*scale,
                collider.offset.y-collider.size.y*.5f-preview.bounds.min.y*scale);
        }

        static void Finish(AppearanceCategory category,string message)=>Finish(new[]{category},message);
        static void Finish(AppearanceCategory[] categories,string message)
        {
            AssetDatabase.SaveAssets();
            Selection.activeObject=categories[0];
            EditorGUIUtility.PingObject(categories[0]);
            Debug.Log(message);
        }
    }
}