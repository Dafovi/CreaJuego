using System.IO;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.PlaygroundBackend;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CreaJuego.Starter
{
    public static class V1Completion
    {
        // Explicit, narrow upgrade; does not regenerate existing sprites, prefabs or level layout.
        public static void PublishValidatedEnemy()
        {
            var d=ItemService.Catalog().Single(i=>i.kind==ItemKind.Enemy);
            d.availableInWorkshop=true;d.category="Retos";
            d.learningHint="Este enemigo va y vuelve por un tramo. Evita tocarlo para no perder puntos de vida.";
            foreach(var p in d.properties)
            {
                p.group=p.path==nameof(GameItem.damage)?"Contacto":"Movimiento";
                if(p.path==nameof(GameItem.distance)){p.label="Distancia de recorrido";p.help="Va hacia la derecha y vuelve. Su recorrido debe quedar libre.";}
                if(p.path==nameof(GameItem.damage))p.label="Daño al personaje";
            }
            EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();
            Debug.Log("CREAJUEGO_ENEMY_GATE_PASSED_PUBLISHED");
        }

        public static void Apply()
        {
            var playerDefinition = ItemService.Catalog().Single(d => d.id == "jugador");
            string playerPath = AssetDatabase.GetAssetPath(playerDefinition.prefab);
            var root = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                if (root.GetComponent<GroundedJumpGate>() == null) root.AddComponent<GroundedJumpGate>();
                PrefabUtility.SaveAsPrefabAsset(root, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            if (!playerDefinition.properties.Any(p => p.path == nameof(GameItem.canJump)))
                playerDefinition.properties = playerDefinition.properties.Concat(new[] { new EducationalProperty {
                    path = nameof(GameItem.canJump), label = "Puede saltar", group = "Salto", control = EducationalControl.Toggle,
                    help = "Desactívalo si quieres que el personaje sólo pueda caminar." } }).OrderBy(p => p.group == "Movimiento" ? 0 : p.group == "Salto" ? 1 : 2).ToArray();
            playerDefinition.properties.Single(p => p.path == nameof(GameItem.jump)).help = "Controla qué tan alto puede saltar el personaje.";
            playerDefinition.properties.Single(p => p.path == nameof(GameItem.jump)).visibleWhen = nameof(GameItem.canJump);
            playerDefinition.properties.Single(p => p.path == nameof(GameItem.health)).help = "Indica cuántos puntos de vida tiene el personaje. Los peligros le quitan puntos de vida. Si llegan a cero, termina la partida.";
            EditorUtility.SetDirty(playerDefinition);
            foreach (var d in ItemService.Catalog())
            {
                if (d.kind == ItemKind.Prize) d.properties[0].group = "Puntuación";
                if (d.kind == ItemKind.Hazard)
                {
                    d.learningHint = "Al tocar este peligro, el personaje pierde puntos de vida.";
                    foreach (var p in d.properties) p.group = "Daño";
                    d.properties.Single(p => p.path == nameof(GameItem.disappear)).help = "Si está activado, el peligro desaparece después de hacer daño.";
                }
                if (d.kind == ItemKind.Goal) { d.learningHint = "Llega aquí para completar el recorrido."; d.properties[0].group = "Final"; }
                if (d.kind == ItemKind.Platform) d.properties.Single(p => p.path == nameof(GameItem.tint)).help = "Elige el color de esta plataforma.";
                EditorUtility.SetDirty(d);
            }
            var scene = EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            foreach (var text in SceneObjects.All<Text>(scene))
            {
                if (text.text == "Vidas") { text.text = "Vida"; text.rectTransform.anchoredPosition = new Vector2(24,-55); }
                if (text.name == "Cantidad de vidas") text.rectTransform.anchoredPosition = new Vector2(180,-55);
                if (text.name == "Puntos") text.rectTransform.anchoredPosition = new Vector2(240,-55);
                if (text.name == "Cantidad de puntos") text.rectTransform.anchoredPosition = new Vector2(330,-55);
                if (text.text == "Sin vidas. Detén y vuelve a jugar.") text.text = "Sin puntos de vida. Detén y vuelve a jugar.";
            }
            EditorSceneManager.SaveScene(scene);
            string packPath = DemoBuilder.Root + "/Content/StarterPack.asset";
            if (AssetDatabase.LoadAssetAtPath<ContentPackDefinition>(packPath) == null)
            {
                var services = new GameObject("Servicios del taller");
                try
                {
                    var camera = new GameObject("Cámara del taller").AddComponent<Camera>(); camera.transform.SetParent(services.transform);
                    camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 5; camera.transform.position = new Vector3(0,1,-10);
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f,.075f,.12f);
                    camera.gameObject.AddComponent<AudioListener>();
                    var session = DemoBuilder.MakeHUD(); session.transform.SetParent(services.transform, false);
                    var pack = ScriptableObject.CreateInstance<ContentPackDefinition>(); pack.id = "starter";
                    pack.sceneServices = PrefabUtility.SaveAsPrefabAsset(services, DemoBuilder.Root + "/Content/SceneServices.prefab");
                    AssetDatabase.CreateAsset(pack, packPath);
                }
                finally { Object.DestroyImmediate(services); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CREAJUEGO_V1_COMPLETION_UPGRADE_OK");
        }
    }
}
