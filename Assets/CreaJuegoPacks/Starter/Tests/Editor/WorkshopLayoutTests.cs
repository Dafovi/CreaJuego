using System.Collections;
using System.Linq;
using CreaJuego.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace CreaJuego.Starter.Tests
{
    public sealed class WorkshopLayoutTests
    {
        [SetUp] public void Setup()=>EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        [TearDown] public void Cleanup(){
            WorkshopTestWindows.Close();
            foreach(var w in Resources.FindObjectsOfTypeAll<CreaJuegoWindow>())w.Close();
            Undo.ClearAll();EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        }
        private static void Activate(Button b){using(var e=NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}}
        [UnityTest] public IEnumerator OpenWorkshopIsIdempotentAndSeparatesResponsibilities()
        {
            CreaJuegoWindow.Open();WorkshopTestWindows.Open();yield return null;yield return null;
            var elements=EditorWindow.GetWindow<CreaJuegoWindow>();
            var previous=elements.position;
            CreaJuegoWindow.Open();yield return null;
            Assert.That(Resources.FindObjectsOfTypeAll<CreaJuegoWindow>().Length,Is.EqualTo(1));
            Assert.That(Resources.FindObjectsOfTypeAll<CreaJuegoPropertiesWindow>().Length,Is.EqualTo(1));
            Assert.That(Resources.FindObjectsOfTypeAll<CreaJuegoPlayBarWindow>().Length,Is.EqualTo(1));
            Assert.That(Resources.FindObjectsOfTypeAll<SceneView>().Length,Is.GreaterThan(0));
            Assert.That(elements.position,Is.EqualTo(previous),"Opening existing windows does not reposition them.");
            Assert.That(elements.rootVisualElement.Q("propiedades"),Is.Null);
            Assert.That(WorkshopTestWindows.Properties.Q("catalogo"),Is.Null);
            Assert.That(WorkshopTestWindows.Properties.Q("mi-juego"),Is.Null);
            Assert.That(elements.rootVisualElement.Q("jugar"),Is.Null);
            Assert.That(WorkshopTestWindows.Properties.Q("jugar"),Is.Null);
            Assert.That(WorkshopTestWindows.Play.Query<Button>().ToList().Count(b=>b.name=="jugar"),Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator CreationSelectionEditsDuplicateDeleteUndoAndSceneChangeStayShared()
        {
            CreaJuegoWindow.Open();WorkshopTestWindows.Open();yield return null;
            var root=EditorWindow.GetWindow<CreaJuegoWindow>().rootVisualElement;
            Activate(root.Q<Button>("crear-premio"));yield return null;yield return null;
            var prize=Selection.activeGameObject.GetComponent<GameItem>();
            var field=WorkshopTestWindows.Properties.Q<IntegerField>("propiedad-points");
            field.value=9;yield return null;
            Assert.That(prize.points,Is.EqualTo(9));
            Activate(root.Q<Button>("duplicar"));yield return null;yield return null;
            var copy=Selection.activeGameObject.GetComponent<GameItem>();
            Assert.That(copy,Is.Not.EqualTo(prize));
            Assert.That(WorkshopTestWindows.Properties.Q<IntegerField>("propiedad-points").value,Is.EqualTo(9));
            Selection.activeGameObject=prize.gameObject;yield return null;
            Assert.That(root.Q<ScrollView>("mi-juego").Query<Button>().ToList().Single(b=>ReferenceEquals(b.userData,prize)).ClassListContains("selected"),Is.True);
            var copyRow=root.Q<ScrollView>("mi-juego").Query<Button>().ToList().Single(b=>ReferenceEquals(b.userData,copy));
            Activate(copyRow);yield return null;
            Assert.That(Selection.activeGameObject,Is.EqualTo(copy.gameObject));
            Activate(root.Q<Button>("eliminar"));yield return null;yield return null;
            Assert.That(WorkshopTestWindows.Properties.Q<IntegerField>("propiedad-points"),Is.Null);
            int count=SceneItemService.Entries().Length;
            Undo.PerformUndo();yield return null;yield return null;
            Assert.That(root.Q<ScrollView>("mi-juego").Query<Button>().ToList().Count(b=>b.userData is GameItem),Is.EqualTo(count+1));
            Undo.PerformRedo();yield return null;yield return null;
            Assert.That(root.Q<ScrollView>("mi-juego").Query<Button>().ToList().Count(b=>b.userData is GameItem),Is.EqualTo(count));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return null;yield return null;
            Assert.That(root.Q<ScrollView>("mi-juego").Query<Button>().ToList(),Is.Empty);
            Assert.That(WorkshopTestWindows.Properties.Q<IntegerField>("propiedad-points"),Is.Null);
        }
        [UnityTest] public IEnumerator NarrowPanelsAndHorizontalPlayBarRemainUsable()
        {
            CreaJuegoWindow.Open();WorkshopTestWindows.Open();
            var elements=EditorWindow.GetWindow<CreaJuegoWindow>();
            var properties=EditorWindow.GetWindow<CreaJuegoPropertiesWindow>();
            var play=EditorWindow.GetWindow<CreaJuegoPlayBarWindow>();
            elements.position=new Rect(40,40,280,460);
            properties.position=new Rect(800,40,300,460);
            play.position=new Rect(100,520,650,180);
            Selection.activeGameObject=SceneItemService.Entries().Single(e=>e.item.definition.kind==ItemKind.Player).item.gameObject;
            yield return null;yield return null;
            Assert.That(elements.rootVisualElement.Q<ScrollView>("catalogo").resolvedStyle.height,Is.GreaterThan(60));
            Assert.That(elements.rootVisualElement.Q<ScrollView>("mi-juego").resolvedStyle.height,Is.GreaterThan(60));
            var panel=properties.rootVisualElement.Q<ScrollView>("propiedades");
            Assert.That(panel.worldBound.xMax,Is.LessThanOrEqualTo(properties.rootVisualElement.worldBound.xMax+1));
            Assert.That(panel.Q<Toggle>("propiedad-canJump"),Is.Not.Null);
            Assert.That(play.rootVisualElement.Q<Button>("jugar").worldBound.yMax,Is.LessThanOrEqualTo(play.rootVisualElement.worldBound.yMax+1));
        }
    }
}

