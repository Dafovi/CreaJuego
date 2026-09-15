using System.Collections;
using System.IO;
using System.Linq;
using CreaJuego.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CreaJuego.Starter.Tests
{
    public sealed class PilotReadinessTests
    {
        const string TestRoot = "Assets/TrabajosTaller/Equipo_Prueba_Automatica";

        [SetUp]
        public void Setup() { if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }

        [TearDown]
        public void Cleanup()
        {
            if (Application.isPlaying) return;
            Undo.ClearAll();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh();
        }
        [UnityTearDown]
        public IEnumerator LeavePlayMode() { if (Application.isPlaying) yield return new ExitPlayMode(); }

        [Test]
        public void FallRecoveryReturnsToSafePositionAndClearsVelocity()
        {
            var go = new GameObject("Jugador", typeof(Rigidbody2D), typeof(PlayerFallRecovery));
            var body = go.GetComponent<Rigidbody2D>();
            go.transform.position = new Vector3(2, 3, 0);
            var recovery = go.GetComponent<PlayerFallRecovery>();
            recovery.CaptureCurrentPosition();
            body.linearVelocity = new Vector2(4, -8);
            body.angularVelocity = 5;
            go.transform.position = new Vector3(40, -30, 0);
            recovery.ReturnToStart();
            Assert.That((Vector3)body.position, Is.EqualTo(new Vector3(2, 3, 0)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(body.angularVelocity, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FallingBelowLimitRecoversAutomaticallyInPlayMode()
        {
            var go = new GameObject("Jugador", typeof(Rigidbody2D), typeof(PlayerFallRecovery));
            go.GetComponent<Rigidbody2D>().gravityScale = 0;
            go.transform.position = new Vector3(3, 4, 0);
            yield return new EnterPlayMode();
            var recovery = Object.FindAnyObjectByType<PlayerFallRecovery>();
            var body = recovery.GetComponent<Rigidbody2D>();
            recovery.transform.position = new Vector3(30, recovery.FallLimit - 1, 0);
            body.position = recovery.transform.position;
            body.linearVelocity = new Vector2(5, -10);
            recovery.SendMessage("FixedUpdate");
            yield return null;
            Assert.That(body.position, Is.EqualTo((Vector2)recovery.SafePosition).Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            yield return new ExitPlayMode();
        }

        [Test]
        public void StartSupportDetectsPlayerOnPlatformAndRejectsEmptyAir()
        {
            var player = ItemService.Create(Definition(ItemKind.Player), Vector3.zero);
            var platform = ItemService.Create(Definition(ItemKind.Platform), new Vector3(0, -1, 0));
            Physics2D.SyncTransforms();
            Assert.That(PlayerSafetyService.HasSupportingSurface(player), Is.True);
            player.transform.position = new Vector3(0, 5, 0);
            Physics2D.SyncTransforms();
            Assert.That(PlayerSafetyService.HasSupportingSurface(player), Is.False);
            Object.DestroyImmediate(platform.gameObject);
        }

        [Test]
        public void PreflightRequiresPlayerSurfaceSupportGoalAndRecovery()
        {
            SceneService.Prepare();
            var checks = SceneService.Validate();
            Assert.That(checks.Single(c => c.label == "Personaje").passed, Is.False);
            Assert.That(checks.Single(c => c.label == "Superficie").passed, Is.False);
            Assert.That(checks.Single(c => c.label == "Inicio seguro").passed, Is.False);
            Assert.That(WorkshopPresentation.Describe(checks).help, Does.Contain("Coloca al Jugador encima de una Plataforma"));
            Assert.That(checks.Single(c => c.label == "Recuperación de caída").passed, Is.False);
            Assert.That(checks.Single(c => c.label == "Meta").passed, Is.False);
        }

        [Test]
        public void NewGameCreatesUniqueSceneMetadataImagesAndServices()
        {
            var first = WorkshopGameService.CreateNew("Mi aventura", "Prueba Automatica");
            Assert.That(first, Is.EqualTo(TestRoot + "/Juego_Mi_aventura.unity"));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(first), Is.Not.Null);
            Assert.That(AssetDatabase.IsValidFolder(TestRoot + "/Imagenes"), Is.True);
            var metadata = WorkshopGameService.Metadata(SceneManager.GetActiveScene());
            Assert.That(metadata.GameName, Is.EqualTo("Mi aventura"));
            Assert.That(metadata.TeamName, Is.EqualTo("Prueba Automatica"));
            Assert.That(SceneObjects.All<Camera>(SceneManager.GetActiveScene()), Is.Not.Empty);
            Assert.That(SceneObjects.All<MonoBehaviour>(SceneManager.GetActiveScene()).OfType<IWorkshopSession>(), Is.Not.Empty);
            var second = WorkshopGameService.CreateNew("Mi aventura", "Prueba Automatica");
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(second, Does.EndWith("Juego_Mi_aventura 1.unity"));
        }

        [TestCase("png")]
        [TestCase("jpg")]
        public void ComputerImageIsCopiedUniquelyAsSpriteWithoutChangingPhysics(string extension)
        {
            WorkshopGameService.CreateNew("Imagen", "Prueba Automatica");
            var player = ItemService.Create(Definition(ItemKind.Player), Vector3.zero);
            var bodyBefore = EditorJsonUtility.ToJson(player.GetComponent<Rigidbody2D>());
            var colliderBefore = EditorJsonUtility.ToJson(player.GetComponent<Collider2D>());
            var source = Path.Combine(Path.GetTempPath(), "CreaJuegoPilotImage." + extension);
            var texture = new Texture2D(4, 4);
            texture.SetPixels(Enumerable.Repeat(Color.magenta, 16).ToArray()); texture.Apply();
            File.WriteAllBytes(source, extension == "png" ? texture.EncodeToPNG() : texture.EncodeToJPG());
            Object.DestroyImmediate(texture);
            try
            {
                var first = WorkshopImageImportService.ImportAndAssign(source, new[] { player });
                var second = WorkshopImageImportService.ImportAndAssign(source, new[] { player });
                Assert.That(AssetDatabase.GetAssetPath(first), Does.StartWith(TestRoot + "/Imagenes/"));
                Assert.That(AssetDatabase.GetAssetPath(second), Is.Not.EqualTo(AssetDatabase.GetAssetPath(first)));
                Assert.That(player.customSprite, Is.EqualTo(second));
                Assert.That(EditorJsonUtility.ToJson(player.GetComponent<Rigidbody2D>()), Is.EqualTo(bodyBefore));
                Assert.That(EditorJsonUtility.ToJson(player.GetComponent<Collider2D>()), Is.EqualTo(colliderBefore));
            }
            finally { File.Delete(source); }
        }

        [Test]
        public void MetadataAndImportedAppearancePersistAfterReopen()
        {
            var path = WorkshopGameService.CreateNew("Persistencia", "Prueba Automatica");
            var item = ItemService.Create(Definition(ItemKind.Decoration), Vector3.zero);
            var source = Path.Combine(Path.GetTempPath(), "CreaJuegoPilotPersist.png");
            var texture = new Texture2D(2, 2); File.WriteAllBytes(source, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            try { WorkshopImageImportService.ImportAndAssign(source, new[] { item }); }
            finally { File.Delete(source); }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(path);
            Assert.That(WorkshopGameService.Metadata(SceneManager.GetActiveScene()).DisplayName, Is.EqualTo("Persistencia · Prueba Automatica"));
            var restored = SceneObjects.All<GameItem>(SceneManager.GetActiveScene()).Single();
            Assert.That(restored.customSprite, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(restored.customSprite), Does.StartWith(TestRoot + "/Imagenes/"));
        }

        [Test]
        public void PilotCatalogContainsSevenMainItemsAndWorkshopExtras()
        {
            Assert.That(ItemService.PilotCatalog().Select(d => d.kind), Is.EquivalentTo(new[] { ItemKind.Player, ItemKind.Platform, ItemKind.Prize, ItemKind.Hazard, ItemKind.Enemy, ItemKind.Goal, ItemKind.Decoration }));
            Assert.That(ItemService.ExtraCatalog().Select(d => d.id), Is.EquivalentTo(new[] { "movil", "piso" }));
        }

        [Test]
        public void BackgroundCanUseAnImageImportedFromComputer()
        {
            WorkshopGameService.CreateNew("Fondo", "Prueba Automatica");
            var source = Path.Combine(Path.GetTempPath(), "CreaJuegoPilotBackground.png");
            var texture = new Texture2D(8, 4);
            texture.SetPixels(Enumerable.Repeat(Color.cyan, 32).ToArray());
            texture.Apply();
            File.WriteAllBytes(source, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            try
            {
                var imported = WorkshopImageImportService.Import(source, SceneManager.GetActiveScene());
                var background = WorldAuthoringService.SetBackground(imported);
                Assert.That(background.GetComponent<SpriteRenderer>().sprite, Is.EqualTo(imported));
                Assert.That(AssetDatabase.GetAssetPath(imported), Does.StartWith(TestRoot + "/Imagenes/"));
                var tools = new WorldToolsView();
                Assert.That(tools.Q<Button>("elegir-fondo-computador"), Is.Not.Null);
            }
            finally { File.Delete(source); }
        }

        [Test]
        public void LongFloorIsAnExtraWithWidePhysicsAndStretchableVisual()
        {
            var definition = ItemService.ExtraCatalog().Single(d => d.id == "piso");
            var floor = ItemService.Create(definition, Vector3.zero);
            Assert.That(floor.definition.kind, Is.EqualTo(ItemKind.Platform));
            Assert.That(floor.stretchVisualToSurface, Is.True);
            Assert.That(floor.GetComponent<BoxCollider2D>().size.x, Is.GreaterThanOrEqualTo(8));
            Assert.That(floor.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void EducationalGizmosDescribeMovementAndInteractions()
        {
            var moving = ItemService.Create(ItemService.ExtraCatalog().Single(d => d.id == "movil"), new Vector3(2, 3, 0));
            moving.distance = 6;
            Assert.That(EducationalGizmos.TryGetMovementPath(moving, out var start, out var end), Is.True);
            Assert.That(start, Is.EqualTo(new Vector3(2, 3, 0)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(end, Is.EqualTo(new Vector3(8, 3, 0)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(EducationalGizmos.InteractionLabel(moving), Is.EqualTo("Superficie móvil"));
            var hazard = ItemService.Create(Definition(ItemKind.Hazard), Vector3.zero);
            Assert.That(EducationalGizmos.InteractionLabel(hazard), Is.EqualTo("Hace daño"));
        }
        [Test]
        public void FacilitatorReturnMovesOnlyPlayerAndSupportsUndo()
        {
            var player = ItemService.Create(Definition(ItemKind.Player), new Vector3(1, 2, 0));
            var decoration = ItemService.Create(Definition(ItemKind.Decoration), new Vector3(8, 3, 0));
            player.GetComponent<PlayerFallRecovery>().CaptureCurrentPosition();
            player.transform.position = new Vector3(30, -20, 0);
            var decorationPosition = decoration.transform.position;
            WorkshopFacilitatorMenu.ReturnPlayerToStart();
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(1, 2, 0)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(decoration.transform.position, Is.EqualTo(decorationPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Undo.PerformUndo();
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(30, -20, 0)).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void NewGameAndPlayBarExposeSpanishIdentityControls()
        {
            var newGame = EditorWindow.GetWindow<NewWorkshopGameWindow>(true, "Nuevo juego");
            try
            {
                newGame.CreateGUI();
                Assert.That(newGame.rootVisualElement.Q<UnityEngine.UIElements.TextField>("nombre-juego"), Is.Not.Null);
                Assert.That(newGame.rootVisualElement.Q<UnityEngine.UIElements.TextField>("nombre-equipo"), Is.Not.Null);
                Assert.That(newGame.rootVisualElement.Q<UnityEngine.UIElements.Button>("crear-juego").text, Is.EqualTo("Crear juego"));
            }
            finally { newGame.Close(); }

            WorkshopGameService.CreateNew("Mi piloto", "Prueba Automatica");
            var playBar = EditorWindow.GetWindow<CreaJuegoPlayBarWindow>();
            try
            {
                playBar.CreateGUI();
                Assert.That(playBar.rootVisualElement.Q<UnityEngine.UIElements.Label>("identidad-juego").text, Is.EqualTo("Mi piloto · Prueba Automatica"));
            }
            finally { playBar.Close(); }
        }

        private static GameItemDefinition Definition(ItemKind kind) => ItemService.PilotCatalog().Single(d => d.kind == kind);
    }
}
