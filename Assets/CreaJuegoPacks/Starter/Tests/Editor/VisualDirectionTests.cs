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
    public sealed class VisualDirectionTests
    {
        [SetUp] public void Setup()=>EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        [TearDown] public void Cleanup() {
            foreach(var w in Resources.FindObjectsOfTypeAll<CreaJuegoWindow>()) w.Close();
            Undo.ClearAll(); EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        }
        private static string Labels(VisualElement root)=>string.Join(" ",root.Query<Label>().ToList().Select(l=>l.text));
        private static void Activate(Button button) {
            using(var evt=NavigationSubmitEvent.GetPooled()) {evt.target=button; button.SendEvent(evt);}
        }
        [UnityTest] public IEnumerator EmptyStateFlowAndCreationHaveClearSections()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Selection.activeGameObject=null;
            var window=EditorWindow.GetWindow<CreaJuegoWindow>(); window.CreateGUI(); yield return null;
            var root=window.rootVisualElement;
            Assert.That(Labels(root),Does.Contain("AÑADIR AL JUEGO").And.Contain("MI JUEGO").And.Contain("Propiedades"));
            Assert.That(Labels(root.Q<ScrollView>("mi-juego")),Does.Contain("Tu juego todavía está vacío.").And.Contain("Elige algo arriba para comenzar."));
            Assert.That(Labels(root.Q<ScrollView>("propiedades")),Does.Contain("Selecciona algo de Mi juego"));
            Assert.That(root.Q("flujo")[0].ClassListContains("current"),Is.True);
            Assert.That(root.Query<Button>().ToList().Count(b=>b.name=="jugar"),Is.EqualTo(1));
            Assert.That(root.Q<Button>("jugar").parent,Is.EqualTo(root.Q("juego-estado")));
            Activate(root.Q<Button>("crear-jugador")); yield return null; yield return null;
            Assert.That(root.Q("flujo")[2].ClassListContains("current"),Is.True);
            Assert.That(Labels(root.Q<ScrollView>("propiedades")),Does.Contain("Este es el personaje que controla quien juega."));
            Assert.That(root.Q<ScrollView>("mi-juego").Query<Button>().ToList().Single().Q<Label>("marca-seleccion").text,Is.EqualTo("✓"));
            Selection.activeGameObject=null; yield return null;
            Assert.That(root.Q("flujo")[1].ClassListContains("current"),Is.True);
        }
        [UnityTest] public IEnumerator PreflightReflectsMissingGoalAndUndoWithoutTechnicalLabels()
        {
            var window=EditorWindow.GetWindow<CreaJuegoWindow>(); window.CreateGUI(); yield return null;
            var root=window.rootVisualElement;
            Assert.That(root.Q<Label>("preflight-title").text,Is.EqualTo("✓ ¡Todo listo para jugar!"));
            var goal=SceneItemService.Entries().Single(e=>e.item.definition.kind==ItemKind.Goal).item;
            ItemService.Delete(goal); yield return null; yield return null;
            Assert.That(root.Q<Label>("preflight-title").text,Is.EqualTo("TU JUEGO NECESITA ALGO"));
            Assert.That(root.Q<Label>("preflight-help").text,Does.Contain("Agrega una Meta"));
            Assert.That(Labels(root.Q<ScrollView>("validacion")),Does.Not.Contain("Cámara").And.Not.Contain("Sesión").And.Not.Contain("Marcador").And.Not.Contain("backend"));
            Undo.PerformUndo(); yield return null; yield return null;
            Assert.That(root.Q<Label>("preflight-title").text,Does.Contain("Todo listo"));
        }
        [Test] public void EveryExistingPreflightFailureStillBlocksAndSuggestionsAreOptional()
        {
            var original=SceneService.Validate();
            Assert.That(original.All(c=>c.passed),Is.True);
            foreach(var check in original) {
                var failing=original.Select(c=>new SceneCheck(c.label,c!=check,c.help)).ToList();
                var view=WorkshopPresentation.Describe(failing);
                Assert.That(view.ready,Is.False,check.label);
                Assert.That(view.help,Is.Not.Empty);
                Assert.That(view.checks.Any(c=>!c.passed && !c.optional),Is.True,check.label);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var suggestions=WorkshopPresentation.Describe(original);
            Assert.That(suggestions.ready,Is.True,"Presentation must not introduce new rules.");
            Assert.That(suggestions.checks.Where(c=>c.optional).All(c=>!c.passed),Is.True);
        }
        [UnityTest] public IEnumerator CompactAndWideWindowsKeepControlsAndFooterReachable()
        {
            var player=SceneItemService.Entries().Single(e=>e.item.definition.kind==ItemKind.Player).item;
            Selection.activeGameObject=player.gameObject;
            var window=EditorWindow.GetWindow<CreaJuegoWindow>();
            foreach(var size in new[]{new Vector2(680,500),new Vector2(1100,760)}) {
                window.position=new Rect(50,50,size.x,size.y); window.CreateGUI();
                yield return null; yield return null;
                var root=window.rootVisualElement;
                foreach(var name in new[]{"catalogo","mi-juego","propiedades"}) {
                    var panel=root.Q<ScrollView>(name);
                    Assert.That(panel.resolvedStyle.height,Is.GreaterThan(30),name);
                    Assert.That(panel.worldBound.xMax,Is.LessThanOrEqualTo(root.worldBound.xMax+1),name);
                }
                Assert.That(root.Q<Button>("jugar").worldBound.yMax,Is.LessThanOrEqualTo(root.worldBound.yMax+1));
                var field=root.Q<IntegerField>("propiedad-health");
                field.Focus(); field.value=5; yield return null;
                Assert.That(player.health,Is.EqualTo(5));
                Assert.That(root.panel.focusController.focusedElement,Is.Not.Null);
                Assert.That(root.Q<Button>("jugar").text,Is.EqualTo("▶ JUGAR"));
            }
        }
    }
}
