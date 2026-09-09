using System.Collections;
using System.Linq;
using CreaJuego.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Unity.Cinemachine;
using Object=UnityEngine.Object;
namespace CreaJuego.Starter.Tests
{
    public sealed class WorldUxTests
    {
        [SetUp] public void Setup() {
            if(Application.isPlaying) return;
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            // Test-created world objects are isolated from the participant's authored limits/background.
            foreach(var boundary in Object.FindObjectsByType<InvisibleBoundary>()) Object.DestroyImmediate(boundary.gameObject);
            foreach(var background in Object.FindObjectsByType<WorkshopBackground>()) Object.DestroyImmediate(background.gameObject);
        }
        [TearDown] public void Cleanup() {
            if(Application.isPlaying) return;
            Undo.ClearAll();
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            AssetDatabase.DeleteAsset("Assets/WorldPersistenceTest.unity");
        }
        [UnityTearDown] public IEnumerator LeavePlay(){if(Application.isPlaying) yield return new ExitPlayMode();}
        static GameItem Player()=>Object.FindObjectsByType<GameItem>().First(i=>i.definition.kind==ItemKind.Player);
        public static void PrepareDemoCamera() {
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            WorldAuthoringService.EnsureCamera();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        [Test] public void BackgroundIsUniqueReplacementAndRemovalSupportUndo()
        {
            var sprite=Player().appearance.sprite;
            var first=WorldAuthoringService.SetBackground(sprite);
            Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
            var alternate=Player().definition.appearancePack.appearances.First(a=>a.sprite!=sprite).sprite;
            Assert.That(WorldAuthoringService.SetBackground(alternate),Is.SameAs(first));
            Undo.FlushUndoRecordObjects();
            Assert.That(Object.FindObjectsByType<WorkshopBackground>().Length,Is.EqualTo(1));
            Undo.PerformUndo();
            Assert.That(first.GetComponent<SpriteRenderer>().sprite,Is.EqualTo(sprite));
            Undo.PerformRedo();
            Assert.That(first.GetComponent<SpriteRenderer>().sprite,Is.EqualTo(alternate));
            WorldAuthoringService.SetBackground(null);
            Assert.That(WorldAuthoringService.Background(),Is.Null);
            Undo.PerformUndo();
            Assert.That(WorldAuthoringService.Background(),Is.Not.Null);
        }
        [Test] public void BackgroundCoversCameraAndWorldObjectsPersist()
        {
            var background=WorldAuthoringService.SetBackground(Player().appearance.sprite);
            var camera=background.output; camera.aspect=16f/9;
            camera.transform.position+=Vector3.right*20; background.Fit();
            var bounds=background.GetComponent<SpriteRenderer>().bounds;
            Assert.That(bounds.size.x,Is.GreaterThanOrEqualTo(camera.orthographicSize*2*camera.aspect-.001));
            Assert.That(bounds.size.y,Is.GreaterThanOrEqualTo(camera.orthographicSize*2-.001));
            Assert.That(bounds.center.x,Is.EqualTo(camera.transform.position.x).Within(.001));
            var boundary=WorldAuthoringService.AddBoundary(new Vector3(25,0,0));
            using(var serialized=new SerializedObject(boundary.GetComponent<BoxCollider2D>())){
                serialized.FindProperty("m_Size").vector2Value=new Vector2(2,12);
                serialized.ApplyModifiedProperties();
            }
            EditorSceneManager.SaveScene(boundary.gameObject.scene,"Assets/WorldPersistenceTest.unity");
            EditorSceneManager.OpenScene("Assets/WorldPersistenceTest.unity");
            Assert.That(Object.FindObjectsByType<WorkshopBackground>().Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<InvisibleBoundary>().Single().GetComponent<BoxCollider2D>().size,Is.EqualTo(new Vector2(2,12)));
        }
        [Test] public void InvisibleBoundaryCreationAndSizeAreUndoable()
        {
            var boundary=WorldAuthoringService.AddBoundary(Vector3.zero);
            Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
            using(var serialized=new SerializedObject(boundary.GetComponent<BoxCollider2D>())){
                serialized.FindProperty("m_Size").vector2Value=new Vector2(3,8);
                serialized.ApplyModifiedProperties();
            }
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(boundary.GetComponent<BoxCollider2D>().size,Is.EqualTo(new Vector2(.5f,20)));
            Undo.PerformRedo();
            Assert.That(boundary.GetComponent<BoxCollider2D>().size,Is.EqualTo(new Vector2(3,8)));
            Undo.PerformUndo(); Undo.PerformUndo();
            Assert.That(Object.FindObjectsByType<InvisibleBoundary>(),Is.Empty);
        }
        [Test] public void ThemeToggleAffectsAllThreeWindowsAndSurvivesRecreation()
        {
            bool old=WorkshopTheme.Dark;
            var windows=new EditorWindow[]{EditorWindow.GetWindow<CreaJuegoWindow>(),EditorWindow.GetWindow<CreaJuegoPropertiesWindow>(),EditorWindow.GetWindow<CreaJuegoPlayBarWindow>()};
            try {
                ((CreaJuegoWindow)windows[0]).CreateGUI();
                ((CreaJuegoPropertiesWindow)windows[1]).CreateGUI();
                ((CreaJuegoPlayBarWindow)windows[2]).CreateGUI();
                foreach(bool dark in new[]{false,true}){
                    windows[0].rootVisualElement.Q<Toggle>("modo-oscuro").value=dark;
                    foreach(var window in windows) Assert.That(window.rootVisualElement.ClassListContains("dark"),Is.EqualTo(dark));
                }
                ((CreaJuegoPropertiesWindow)windows[1]).CreateGUI();
                Assert.That(windows[1].rootVisualElement.ClassListContains("dark"),Is.True);
            } finally {WorkshopTheme.Dark=old; foreach(var window in windows) window.Close();}
        }
        [UnityTest] public IEnumerator CatalogTracksChosenCustomSprite()
        {
            var window=EditorWindow.GetWindow<CreaJuegoWindow>(); window.CreateGUI();
            var player=Player(); Selection.activeGameObject=player.gameObject;
            var sprite=player.definition.appearancePack.appearances.First(a=>a.sprite!=player.appearance.sprite).sprite;
            try {
                ItemAppearance.Choose(new[]{player},null,sprite);
                yield return null; yield return null;
                Assert.That(window.rootVisualElement.Q<Image>("icono-"+player.definition.id).sprite,Is.EqualTo(sprite));
            } finally {window.Close();}
        }
        [Test] public void CameraCreationUndoRemovesOnlyWorkshopInfrastructure()
        {
            foreach(var old in Object.FindObjectsByType<WorkshopCameraRig>()) Object.DestroyImmediate(old.gameObject);
            foreach(var old in Object.FindObjectsByType<CinemachineBrain>()) Object.DestroyImmediate(old);
            Undo.ClearAll();
            var output=WorldAuthoringService.OutputCamera();
            WorldAuthoringService.EnsureCamera(); Undo.FlushUndoRecordObjects();
            Assert.That(Object.FindObjectsByType<WorkshopCameraRig>().Length,Is.EqualTo(1));
            Undo.PerformUndo();
            Assert.That(Object.FindObjectsByType<WorkshopCameraRig>(),Is.Empty);
            Assert.That(output,Is.Not.Null); Assert.That(output.GetComponent<CinemachineBrain>(),Is.Null);
            Undo.PerformRedo();
            Assert.That(Object.FindObjectsByType<WorkshopCameraRig>().Single().cameraController.Follow,Is.EqualTo(Player().transform));
        }
        [Test] public void GameViewUsesSixteenByNine()
        {
            WorkshopEditorConfiguration.ConfigureGameView();
            PlayModeWindow.GetRenderingResolution(out uint width,out uint height);
            Assert.That(width,Is.EqualTo(1280)); Assert.That(height,Is.EqualTo(720));
        }
        [Test] public void CameraSetupIsIdempotentAndTargetsLogicalPlayer()
        {
            WorldAuthoringService.EnsureCamera(); WorldAuthoringService.EnsureCamera();
            var rig=Object.FindObjectsByType<WorkshopCameraRig>().Single();
            Assert.That(rig.cameraController.Follow,Is.EqualTo(Player().transform));
            Assert.That(rig.output.GetComponent<CinemachineBrain>(),Is.Not.Null);
        }
        [UnityTest] public IEnumerator CameraFollowsAndInvisibleWallBlocksPhysics()
        {
            WorldAuthoringService.EnsureCamera();
            WorldAuthoringService.AddBoundary(new Vector3(50,0,0));
            yield return new EnterPlayMode();
            var player=Player();
            player.GetComponent<Rigidbody2D>().simulated=false;
            var rig=Object.FindObjectsByType<WorkshopCameraRig>().Single();
            player.transform.position=new Vector3(30,3,0);
            float until=Time.realtimeSinceStartup+1.5f;
            while(Time.realtimeSinceStartup<until) yield return null;
            Assert.That(rig.output.transform.position.x,Is.EqualTo(30).Within(1));
            var probe=new GameObject("Prueba límite",typeof(BoxCollider2D),typeof(Rigidbody2D));
            probe.transform.position=new Vector3(48,0,0);
            var body=probe.GetComponent<Rigidbody2D>(); body.gravityScale=0;
            until=Time.realtimeSinceStartup+.6f;
            while(Time.realtimeSinceStartup<until){body.linearVelocity=Vector2.right*10;yield return new WaitForFixedUpdate();}
            Assert.That(probe.transform.position.x,Is.LessThan(49.5f));
            Assert.That(Object.FindObjectsByType<InvisibleBoundary>().Single().GetComponentsInChildren<Renderer>(),Is.Empty);
            yield return new ExitPlayMode();
        }
    }
}

