using System.Collections;
using System.Linq;
using CreaJuego.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
namespace CreaJuego.Starter.Tests
{
    public sealed class V1UiTests
    {
        [SetUp] public void Setup()=>EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        [TearDown] public void Cleanup(){foreach(var w in Resources.FindObjectsOfTypeAll<CreaJuegoWindow>())w.Close();WorkshopTestWindows.Close(); Undo.ClearAll();EditorSceneManager.OpenScene(DemoBuilder.ScenePath);}
        [UnityTest] public IEnumerator MixedValuesAndIncompatibleSelection()
        {
            var a=Object.FindObjectsByType<GameItem>().First(i=>i.definition.kind==ItemKind.Prize);var b=ItemService.Duplicate(a);
            using(var s=new SerializedObject(b)){s.FindProperty(nameof(GameItem.points)).intValue=7;s.ApplyModifiedProperties();}
            Selection.objects=new Object[]{a.gameObject,b.gameObject};
            var window=EditorWindow.GetWindow<CreaJuegoWindow>();window.CreateGUI(); WorkshopTestWindows.Open();yield return null;yield return null;
            var field=WorkshopTestWindows.Properties.Q<IntegerField>("propiedad-points");
            Assert.That(field.showMixedValue,Is.True);
            field.value=5;yield return null;
            Assert.That(a.points,Is.EqualTo(5));Assert.That(b.points,Is.EqualTo(5));
            Selection.objects=new Object[]{a.gameObject,Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player).gameObject};
            yield return null;
            Assert.That(WorkshopTestWindows.Properties.Q<IntegerField>("propiedad-points"),Is.Null);
            Assert.That(WorkshopTestWindows.Properties.Query<Label>().ToList().Any(l=>l.text.Contains("mismo tipo")),Is.True);
        }
        [UnityTest] public IEnumerator CompactWindowHasThemeScrollAndEducationalPlayerControls()
        {
            var player=Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player);Selection.activeGameObject=player.gameObject;
            var window=EditorWindow.GetWindow<CreaJuegoWindow>();window.position=new Rect(50,50,680,500);window.CreateGUI(); WorkshopTestWindows.Open();
            yield return null;yield return null;
            Assert.That(window.rootVisualElement.styleSheets.count,Is.GreaterThan(0));
            Assert.That(WorkshopTestWindows.Play.Q<Button>("preparar-escena"),Is.Not.Null);
            var properties=WorkshopTestWindows.Properties.Q<ScrollView>("propiedades");
            Assert.That(properties.resolvedStyle.height,Is.GreaterThan(100));
            Assert.That(properties.worldBound.xMax,Is.LessThanOrEqualTo(WorkshopTestWindows.Properties.worldBound.xMax+1));
            Assert.That(WorkshopTestWindows.Properties.Q<Toggle>("propiedad-canJump"),Is.Not.Null);
            Assert.That(WorkshopTestWindows.Properties.Q<IntegerField>("propiedad-health"),Is.Not.Null);
            var toggle=WorkshopTestWindows.Properties.Q<Toggle>("propiedad-canJump");toggle.value=false;yield return null;yield return null;
            Assert.That(player.canJump,Is.False);
            Assert.That(WorkshopTestWindows.Properties.Q<VisualElement>("fila-jump").style.display.value,Is.EqualTo(DisplayStyle.None));
        }
    }
}

