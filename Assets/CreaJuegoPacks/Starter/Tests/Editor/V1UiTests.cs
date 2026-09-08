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
        [TearDown] public void Cleanup(){foreach(var w in Resources.FindObjectsOfTypeAll<CreaJuegoWindow>())w.Close();Undo.ClearAll();EditorSceneManager.OpenScene(DemoBuilder.ScenePath);}
        [UnityTest] public IEnumerator MixedValuesAndIncompatibleSelection()
        {
            var a=Object.FindObjectsByType<GameItem>().First(i=>i.definition.kind==ItemKind.Prize);var b=ItemService.Duplicate(a);
            using(var s=new SerializedObject(b)){s.FindProperty(nameof(GameItem.points)).intValue=7;s.ApplyModifiedProperties();}
            Selection.objects=new Object[]{a.gameObject,b.gameObject};
            var window=EditorWindow.GetWindow<CreaJuegoWindow>();window.CreateGUI();yield return null;yield return null;
            var field=window.rootVisualElement.Q<IntegerField>("propiedad-points");
            Assert.That(field.showMixedValue,Is.True);
            field.value=5;yield return null;
            Assert.That(a.points,Is.EqualTo(5));Assert.That(b.points,Is.EqualTo(5));
            Selection.objects=new Object[]{a.gameObject,Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player).gameObject};
            yield return null;
            Assert.That(window.rootVisualElement.Q<IntegerField>("propiedad-points"),Is.Null);
            Assert.That(window.rootVisualElement.Query<Label>().ToList().Any(l=>l.text.Contains("mismo tipo")),Is.True);
        }
        [UnityTest] public IEnumerator CompactWindowHasThemeScrollAndEducationalPlayerControls()
        {
            var player=Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player);Selection.activeGameObject=player.gameObject;
            var window=EditorWindow.GetWindow<CreaJuegoWindow>();window.position=new Rect(50,50,680,500);window.CreateGUI();
            yield return null;yield return null;
            Assert.That(window.rootVisualElement.styleSheets.count,Is.GreaterThan(0));
            Assert.That(window.rootVisualElement.Q<Button>("preparar-escena"),Is.Not.Null);
            var properties=window.rootVisualElement.Q<ScrollView>("propiedades");
            Assert.That(properties.resolvedStyle.height,Is.GreaterThan(100));
            Assert.That(properties.worldBound.xMax,Is.LessThanOrEqualTo(window.rootVisualElement.worldBound.xMax+1));
            Assert.That(window.rootVisualElement.Q<Toggle>("propiedad-canJump"),Is.Not.Null);
            Assert.That(window.rootVisualElement.Q<IntegerField>("propiedad-health"),Is.Not.Null);
            var toggle=window.rootVisualElement.Q<Toggle>("propiedad-canJump");toggle.value=false;yield return null;yield return null;
            Assert.That(player.canJump,Is.False);
            Assert.That(window.rootVisualElement.Q<VisualElement>("fila-jump").style.display.value,Is.EqualTo(DisplayStyle.None));
        }
    }
}

