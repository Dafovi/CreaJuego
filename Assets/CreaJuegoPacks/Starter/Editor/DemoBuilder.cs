using System;
using System.IO;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.PlaygroundBackend;
using Playground.Attributes;
using Playground.Conditions;
using Playground.Movement;
using Playground.UserInterface;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreaJuego.Starter
{
    public static class DemoBuilder
    {
        public const string Root = "Assets/CreaJuegoPacks/Starter";
        public const string ScenePath = Root + "/Demo/CreaJuegoPlaygroundDemo.unity";

        [MenuItem("CreaJuego/Preparar demo")]
        public static void Prepare()
        {
            if (Application.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorSceneManager.OpenScene(ScenePath);
            CreaJuegoWindow.Open();
        }

        // Idempotent: existing authored assets are never regenerated or overwritten.
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Content"); Directory.CreateDirectory(Root + "/Demo");
            AssetDatabase.Refresh();
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            var sprite = MakeSprite();
            Definition("jugador", "Jugador", "Personajes", "Corre y salta.", "Usa las flechas y Espacio para saltar.", ItemKind.Player, new Color(.3f,.8f,1), new Vector2(.7f,1), sprite,
                Float(nameof(GameItem.speed), "Velocidad", .2f, 5), Float(nameof(GameItem.jump), "Salto", 5, 18), Integer(nameof(GameItem.health), "Vidas", 1, 10));
            Definition("plataforma", "Plataforma", "Mundo", "Un lugar donde apoyarse.", "Mueve y escala esta plataforma en la vista Escena.", ItemKind.Platform, new Color(.35f,.65f,.55f), new Vector2(3,.45f), sprite, Tint());
            Definition("movil", "Plataforma móvil", "Mundo", "Viaja de un lado al otro.", "Distancia indica cuánto recorre hacia la derecha y regresa.", ItemKind.MovingPlatform, new Color(.35f,.8f,.7f), new Vector2(2,.4f), sprite,
                Float(nameof(GameItem.speed), "Velocidad", .2f, 3), Float(nameof(GameItem.distance), "Distancia", .5f, 6));
            Definition("premio", "Premio", "Objetos", "Recógelo para sumar puntos.", "Colócalo en el camino del personaje.", ItemKind.Prize, new Color(1,.8f,.25f), new Vector2(.45f,.45f), sprite, Integer(nameof(GameItem.points), "Puntos", 1, 100));
            Definition("peligro", "Peligro", "Objetos", "Al tocarlo pierdes vidas.", "Salta por encima. Con cero vidas termina la partida.", ItemKind.Hazard, new Color(1,.3f,.4f), new Vector2(.65f,.5f), sprite,
                Integer(nameof(GameItem.damage), "Daño", 1, 10), new EducationalProperty { path = nameof(GameItem.disappear), label = "Desaparecer al tocarlo", control = EducationalControl.Toggle, help = "Se retira después del primer contacto." });
            Definition("enemigo", "Enemigo", "Objetos", "Patrulla y hace daño.", "Recorre un tramo horizontal. Puedes saltar para evitarlo.", ItemKind.Enemy, new Color(.8f,.3f,.65f), new Vector2(.6f,.7f), sprite,
                Float(nameof(GameItem.speed), "Velocidad", .2f, 3), Float(nameof(GameItem.distance), "Distancia", .5f, 6), Integer(nameof(GameItem.damage), "Daño", 1, 10));
            Definition("meta", "Meta", "Objetos", "El final de tu recorrido.", "Llega a la puerta verde para completar la experiencia.", ItemKind.Goal, new Color(.5f,1,.5f), new Vector2(.7f,1.6f), sprite,
                new EducationalProperty { path = nameof(GameItem.message), label = "Mensaje al llegar", control = EducationalControl.Text, help = "Lo que verá quien complete tu juego." });
            Definition("decoracion", "Decoración", "Decoración", "Dale color a tu mundo.", "No bloquea el paso ni hace daño.", ItemKind.Decoration, new Color(.4f,.45f,.7f), new Vector2(1,1), sprite, Tint());
            AssetDatabase.SaveAssets();
            if (File.Exists(ScenePath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Cámara del taller").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 5;
            camera.transform.position = new Vector3(0,1,-10); camera.backgroundColor = new Color(.055f,.075f,.12f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AudioListener>();
            Place("jugador", -7,-1.2f);
            var ground = Place("plataforma", 0,-2.2f); ground.transform.localScale = new Vector3(6,1,1);
            Place("plataforma", -3,-.7f);
            Place("premio", -4,-1.3f);
            Place("premio", -3,0);
            Place("peligro", 1,-1.7f);
            Place("meta", 7,-1.15f);
            Place("movil", 3,.2f);
            Place("decoracion", -6,1.8f);
            MakeHUD();
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath)) EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("CREAJUEGO_DEMO_READY " + Application.unityVersion);
        }

        private static GameItem Place(string id, float x, float y) => ItemService.Create(ItemService.Catalog().Single(d => d.id == id), new Vector3(x,y,0));
        private static EducationalProperty Float(string path, string label, float min, float max) => new EducationalProperty { path = path, label = label, minimum = min, maximum = max, control = EducationalControl.Float, help = "Ajusta " + label.ToLowerInvariant() + " y prueba el resultado." };
        private static EducationalProperty Integer(string path, string label, int min, int max) => new EducationalProperty { path = path, label = label, minimum = min, maximum = max, control = EducationalControl.Integer, help = "Cantidad de " + label.ToLowerInvariant() + "." };
        private static EducationalProperty Tint() => new EducationalProperty { path = nameof(GameItem.tint), label = "Color", control = EducationalControl.Color, help = "Elige el color que tendrá al probar." };

        private static Sprite MakeSprite()
        {
            string path = Root + "/Content/Block.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32,32);
                texture.SetPixels(Enumerable.Repeat(Color.white, 1024).ToArray()); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Definition(string id, string title, string category, string description, string hint, ItemKind kind, Color color, Vector2 size, Sprite sprite, params EducationalProperty[] properties)
        {
            string path = Root + "/Content/" + id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<GameItemDefinition>(path) != null) return;
            var definition = ScriptableObject.CreateInstance<GameItemDefinition>();
            definition.id = id; definition.displayName = title; definition.category = category; definition.description = description; definition.learningHint = hint; definition.kind = kind; definition.order = (int)kind; definition.properties = properties; definition.icon = sprite;
            AssetDatabase.CreateAsset(definition, path);
            var go = new GameObject(title);
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.drawMode = SpriteDrawMode.Sliced; renderer.size = size;
                var item = go.AddComponent<GameItem>(); item.definition = definition; item.tint = color; item.speed = kind == ItemKind.Player ? 2.5f : 1; item.jump = 14; item.disappear = kind == ItemKind.Hazard;
                if (kind != ItemKind.Decoration) { var collider = go.AddComponent<BoxCollider2D>(); collider.size = size; collider.isTrigger = kind == ItemKind.Prize || kind == ItemKind.Hazard || kind == ItemKind.Goal || kind == ItemKind.Enemy; }
                if (kind == ItemKind.Player)
                {
                    go.tag = "Player";
                    go.GetComponent<BoxCollider2D>().sharedMaterial = PlayerMaterial();
                    var body = go.AddComponent<Rigidbody2D>(); body.freezeRotation = true; body.linearDamping = 3; body.gravityScale = 3; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    go.AddComponent<Move>(); go.AddComponent<Jump>(); go.AddComponent<HealthSystemAttribute>();
                }
                if (kind == ItemKind.Prize) go.AddComponent<CollectableAttribute>();
                if (kind == ItemKind.Hazard || kind == ItemKind.Enemy) go.AddComponent<ModifyHealthAttribute>();
                if (kind == ItemKind.MovingPlatform || kind == ItemKind.Enemy) { var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; go.AddComponent<Patrol>(); }
                if (kind == ItemKind.Goal) { var condition = go.AddComponent<ConditionArea>(); condition.filterByTag = true; condition.happenOnlyOnce = true; condition.actions.Add(go.AddComponent<ReachGoalAction>()); }
                go.AddComponent<PlaygroundAdapter>().ApplyConfiguration();
                definition.prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Content/" + id + ".prefab");
                EditorUtility.SetDirty(definition);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static Text Label(Transform parent, string name, string text, Vector2 position, int size = 22)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = text; label.fontSize = size; label.color = Color.white;
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(900,70);
            return label;
        }

        private static PhysicsMaterial2D PlayerMaterial()
        {
            string path = Root + "/Content/Player.physicsMaterial2D";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material == null) { material = new PhysicsMaterial2D("Movimiento suave") { friction = 0, bounciness = 0 }; AssetDatabase.CreateAsset(material,path); }
            return material;
        }

        public static void TunePrototype()
        {
            // Explicit one-time migration of this spike's generated player, not part of Prepare.
            var definition = ItemService.Catalog().Single(d => d.kind == ItemKind.Player);
            string path = AssetDatabase.GetAssetPath(definition.prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.GetComponent<BoxCollider2D>().sharedMaterial = PlayerMaterial();
                var item = root.GetComponent<GameItem>(); item.speed = 2.5f; item.jump = 14;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        public static void BuildPlayer()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/CreaJuego/CreaJuego.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Falló la compilación del reproductor: " + report.summary.result);
            Debug.Log("CREAJUEGO_PLAYER_BUILD_OK");
        }

        public static void RenderPreview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var camera = Camera.main;
            var target = new RenderTexture(1280,720,24);
            var previous = RenderTexture.active;
            var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes("Docs/CreaJuego-scene.png",image.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); }
        }

        private static void MakeHUD()
        {
            var canvas = new GameObject("Indicaciones del juego").AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,720);
            var ui = canvas.gameObject.AddComponent<UIScript>(); ui.gameType = UIScript.GameType.Life;
            var stats = new GameObject("Marcador", typeof(RectTransform)); stats.transform.SetParent(canvas.transform,false); ui.statsPanel = stats;
            // HUD labels live on the canvas, so their coordinates are independent of panel anchors.
            Label(canvas.transform,"Controles","CREAJUEGO  ·  Flechas: moverte  ·  Espacio: saltar",new Vector2(24,-18));
            Label(canvas.transform,"Vidas","Vidas",new Vector2(24,-55)); ui.numberLabels[0] = Label(canvas.transform,"Cantidad de vidas","3",new Vector2(100,-55));
            Label(canvas.transform,"Puntos","Puntos",new Vector2(170,-55)); ui.numberLabels[1] = Label(canvas.transform,"Cantidad de puntos","0",new Vector2(250,-55));
            ui.gameOverPanel = Label(canvas.transform,"Fin","Sin vidas. Detén y vuelve a probar.",new Vector2(280,-300),32).gameObject; ui.gameOverPanel.SetActive(false);
            ui.winLabel = Label(canvas.transform,"Victoria","¡Lo lograste!",new Vector2(280,-300),32); ui.winPanel = ui.winLabel.gameObject; ui.winPanel.SetActive(false);
            var session = canvas.gameObject.AddComponent<DemoSession>(); session.playgroundUI = ui;
            session.status = Label(canvas.transform,"Objetivo","Recoge premios, evita el peligro y llega a la puerta verde.",new Vector2(24,-660));
        }
    }
}
