using System;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.PlaygroundBackend;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace CreaJuego.Starter.Editor
{
    public static class CombatMvpBuilder
    {
        public const string Controls="Muévete con A/D o las flechas, salta con Espacio y golpea con X.";
        static T Ensure<T>(GameObject root) where T:Component => root.GetComponent<T>() ?? root.AddComponent<T>();
        [MenuItem("CreaJuego/Contenido/Preparar combate MVP")]
        public static void Run()
        {
            AppearancePackBuilder.Run();
            foreach(var definition in ItemService.Catalog())
            {
                if(definition.kind==ItemKind.Decoration) { definition.availableInWorkshop=true; definition.description="Decora tu mundo. No bloquea ni hace daño."; }
                if(definition.kind==ItemKind.Player)
                {
                    definition.learningHint=Controls;
                    definition.properties=definition.properties.Where(p=>p.path!=nameof(GameItem.canAttack) && p.path!=nameof(GameItem.attackDamage)).Concat(new[]{
                        new EducationalProperty { path=nameof(GameItem.canAttack),label="Puede golpear",group="Ataque",control=EducationalControl.Toggle,help="Activa el golpe con X." },
                        new EducationalProperty { path=nameof(GameItem.attackDamage),label="Daño",group="Ataque",control=EducationalControl.Integer,minimum=1,maximum=5,visibleWhen=nameof(GameItem.canAttack),help="Puntos de vida que quita cada golpe al enemigo." }
                    }).ToArray();
                }
                if(definition.kind==ItemKind.Enemy)
                {
                    definition.learningHint="Va y vuelve por un tramo. Puedes evitarlo o golpearlo con X.";
                    definition.properties=definition.properties.Where(p=>p.path!=nameof(GameItem.health)).Concat(new[]{
                        new EducationalProperty { path=nameof(GameItem.health),label="Puntos de vida",group="Vida",control=EducationalControl.Integer,minimum=1,maximum=5,help="Cuántos puntos de vida tiene antes de desaparecer en esta partida." }
                    }).ToArray();
                }
                EditorUtility.SetDirty(definition);
                string path=AssetDatabase.GetAssetPath(definition.prefab); var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var item=root.GetComponent<GameItem>();
                    if(definition.kind==ItemKind.Player)
                    {
                        Ensure<DamageFeedback>(root); Ensure<PlayerDamageReceiver>(root);
                        var attack=Ensure<PlayerAttack>(root);
                        if(attack.attackPoint==null) { var point=new GameObject("AttackPoint"); point.transform.SetParent(root.transform,false); point.transform.localPosition=new Vector3(.65f,0,0); attack.attackPoint=point.transform; }
                    }
                    if(definition.kind==ItemKind.Enemy)
                    {
                        bool newHealth=root.GetComponent<EnemyVitality>()==null;
                        Ensure<EnemyVitality>(root); Ensure<DamageFeedback>(root); Ensure<PlaygroundContactDamage>(root);
                        if(newHealth) item.health=2;
                    }
                    if(definition.kind==ItemKind.Hazard) Ensure<PlaygroundContactDamage>(root);
                    if(definition.kind==ItemKind.Decoration)
                        foreach(var collider in root.GetComponents<Collider2D>()) UnityEngine.Object.DestroyImmediate(collider);
                    root.GetComponent<ItemVisual>()?.Apply(); PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var pack=AssetDatabase.FindAssets("t:ContentPackDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<ContentPackDefinition>(AssetDatabase.GUIDToAssetPath(g))).Single(p=>p.sceneServices!=null);
            string servicePath=AssetDatabase.GetAssetPath(pack.sceneServices); var services=PrefabUtility.LoadPrefabContents(servicePath);
            try { foreach(var label in services.GetComponentsInChildren<Text>(true)) if(label.name=="Controles") label.text=Controls; PrefabUtility.SaveAsPrefabAsset(services,servicePath); }
            finally { PrefabUtility.UnloadPrefabContents(services); }
            AssetDatabase.SaveAssets(); Debug.Log("Combate MVP preparado sin modificar escenas ni scripts vendor.");
        }
    }
}
