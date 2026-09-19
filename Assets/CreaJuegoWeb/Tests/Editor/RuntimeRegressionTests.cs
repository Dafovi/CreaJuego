using System.Collections;
using System.Linq;
using CreaJuego.Web;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.TestTools;
using UnityEngine.UI;

namespace CreaJuego.Web.Tests
{
    public sealed class RuntimeRegressionTests
    {
        RuntimeAuthoringController Open()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();
            controller.NewProject(false);
            return controller;
        }

        [Test]
        public void SelectionUsesStableInstanceIdAcrossRebuild()
        {
            var c=Open();var data=c.Project.objects.Single(o=>o.definitionId=="premio");
            Assert.That(c.Selection.Select(data.instanceId),Is.Not.Null);
            var before=c.Selection.SelectedItem;c.Rebuild();
            Assert.That(c.Selection.SelectedInstanceId,Is.EqualTo(data.instanceId));
            Assert.That(c.Selection.SelectedItem,Is.Not.Null.And.Not.SameAs(before));
            Assert.That(c.SelectedData(),Is.SameAs(data));
        }

        [Test]
        public void AuthoringHitTestSelectsPlayerByVisualWithoutGameplayCollider()
        {
            var c=Open();var player=c.Project.objects.Single(o=>o.definitionId=="jugador");
            var item=c.Selection.Select(player.instanceId);var collider=item.GetComponent<Collider2D>();collider.enabled=false;
            var visual=ItemVisual.Resolve(item);var hit=RuntimeAuthoringHitTest.Pick(visual.bounds.center,c.buildRoot.GetComponentsInChildren<GameItem>());
            Assert.That(hit,Is.SameAs(item));
        }

        [Test]
        public void AuthoringHitTestPrefersSpecificVisualOverPlatform()
        {
            var c=Open();var player=c.Project.objects.Single(o=>o.definitionId=="jugador");var item=c.Selection.Select(player.instanceId);
            var hit=RuntimeAuthoringHitTest.Pick(ItemVisual.Resolve(item).bounds.center,c.buildRoot.GetComponentsInChildren<GameItem>());
            Assert.That(hit.definition.kind,Is.EqualTo(ItemKind.Player));
        }

        [Test]
        public void ZoomNormalizesBrowserAndTraditionalWheelSteps()
        {
            Assert.That(RuntimePointerContext.Zoom(10,1,2,30),Is.EqualTo(RuntimePointerContext.Zoom(10,120,2,30)).Within(.001f));
            Assert.That(RuntimePointerContext.Zoom(10,1,2,30),Is.LessThan(9));
            Assert.That(RuntimePointerContext.Zoom(10,-1,2,30),Is.GreaterThan(11));
        }

        [UnityTest]
        public IEnumerator RowsPropertiesScrollAndPlayRoundTripStaySynchronized()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);
            yield return new EnterPlayMode();yield return null;
            var c=Object.FindAnyObjectByType<RuntimeAuthoringController>();var ui=Object.FindAnyObjectByType<RuntimeAuthoringUI>();
            Assert.That(ui.VisibleItemRowCount,Is.EqualTo(c.Project.objects.Count));
            var firstRow=Object.FindObjectsByType<RuntimeItemRow>(FindObjectsInactive.Exclude).First().GetComponent<RectTransform>();Assert.That(firstRow.pivot.y,Is.EqualTo(1));Assert.That(firstRow.anchorMin.y,Is.EqualTo(1));Assert.That(Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude).Single(x=>x.name=="Mi juego").verticalNormalizedPosition,Is.EqualTo(1).Within(.01f));

            var expected=new[]{("jugador","JUGADOR","Velocidad"),("plataforma","PLATAFORMA","Ancho"),("enemigo","ENEMIGO","Daño"),("decoracion","DECORACIÓN","Arrastra")};
            foreach(var entry in expected)
            {
                var data=c.Project.objects.First(o=>o.definitionId==entry.Item1);
                Assert.That(ui.ClickItemRow(data.instanceId),Is.True);yield return null;
                Assert.That(c.Selection.SelectedInstanceId,Is.EqualTo(data.instanceId));
                Assert.That(ui.VisiblePropertiesText,Does.Contain(entry.Item2).And.Contain(entry.Item3));
            }

            int before=c.Project.objects.Count;var created=c.Create("premio",Vector3.zero);var createdId=c.SelectedId();yield return null;
            Assert.That(created,Is.Not.Null);Assert.That(ui.VisibleItemRowCount,Is.EqualTo(before+1));
            Assert.That(ui.ClickItemRow(createdId),Is.True);yield return null;
            Assert.That(ui.VisiblePropertiesText,Does.Contain("PREMIO").And.Contain("Puntos"));
            c.DeleteSelected();yield return null;Assert.That(ui.VisibleItemRowCount,Is.EqualTo(before));
            c.Undo();yield return null;Assert.That(ui.VisibleItemRowCount,Is.EqualTo(before+1));
            c.Redo();yield return null;Assert.That(ui.VisibleItemRowCount,Is.EqualTo(before));

            Canvas.ForceUpdateCanvases();
            Assert.That(RuntimePointerContext.IsPointerOverBlockingUI(new Vector2(Screen.width*.05f,Screen.height*.5f)),Is.True);
            Assert.That(RuntimePointerContext.IsPointerOverBlockingUI(new Vector2(Screen.width*.5f,Screen.height*.5f)),Is.False);
            Assert.That(RuntimePointerContext.IsPointerOverBlockingUI(new Vector2(Screen.width*.9f,Screen.height*.5f)),Is.True);
            Assert.That(Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude).All(s=>s.scrollSensitivity>=20),Is.True);

            Assert.That(c.EnterPlay(),Is.Null);yield return null;Assert.That(c.Project.objects.Count,Is.EqualTo(before));
            c.ExitPlay();yield return null;Assert.That(ui.VisibleItemRowCount,Is.EqualTo(before));
            var player=c.Project.objects.Single(o=>o.definitionId=="jugador");Assert.That(ui.ClickItemRow(player.instanceId),Is.True);yield return null;
            Assert.That(ui.VisiblePropertiesText,Does.Contain("JUGADOR").And.Contain("Fuerza de salto"));
            yield return new ExitPlayMode();
        }
    }
}
