using System.Collections;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.PlaygroundBackend;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CreaJuego.Starter.Tests
{
    public class V11UxTests
    {
        [SetUp] public void Setup() => EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        [TearDown] public void Cleanup() {
            foreach(var w in Resources.FindObjectsOfTypeAll<CreaJuegoWindow>()) w.Close();
            WorkshopTestWindows.Close(); Undo.ClearAll(); EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        }
        private static void Activate(Button button) {
            using(var evt=NavigationSubmitEvent.GetPooled()) { evt.target=button; button.SendEvent(evt); }
        }
        [UnityTest] public IEnumerator CatalogCreatesAndSceneListOnlySelectsAndTracksUndo()
        {
            var window=EditorWindow.GetWindow<CreaJuegoWindow>(); window.CreateGUI(); WorkshopTestWindows.Open();
            yield return null;
            var root=window.rootVisualElement;
            int before=SceneItemService.Entries().Length;
            Activate(root.Q<Button>("crear-plataforma"));
            yield return null; yield return null;
            Assert.That(SceneItemService.Entries().Length,Is.EqualTo(before+1));
            var created=Selection.activeGameObject.GetComponent<GameItem>();
            string technicalName=created.name;
            var list=root.Q<ScrollView>("mi-juego");
            Assert.That(list.Query<Button>().ToList().Count,Is.EqualTo(before+1));
            Selection.activeGameObject=null;
            Activate(list.Query<Button>().ToList().Single(b=>ReferenceEquals(b.userData,created)));
            yield return null;
            Assert.That(Selection.activeGameObject,Is.EqualTo(created.gameObject));
            Assert.That(SceneItemService.Entries().Length,Is.EqualTo(before+1));
            Assert.That(root.Q<Button>("crear-plataforma").ClassListContains("selected"),Is.False);
            Activate(root.Q<Button>("duplicar"));
            yield return null; yield return null;
            Assert.That(list.Query<Button>().ToList().Count,Is.EqualTo(before+2));
            Assert.That(created.name,Is.EqualTo(technicalName));
            Assert.That(SceneItemService.Entries().Select(e=>e.label).Distinct().Count(),Is.EqualTo(before+2));
            Undo.IncrementCurrentGroup();
            Activate(root.Q<Button>("eliminar"));
            yield return null; yield return null;
            Assert.That(list.Query<Button>().ToList().Count,Is.EqualTo(before+1));
            Undo.PerformUndo(); yield return null;
            Assert.That(list.Query<Button>().ToList().Count,Is.EqualTo(before+2));
            Undo.PerformRedo(); yield return null;
            Assert.That(list.Query<Button>().ToList().Count,Is.EqualTo(before+1));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return null; yield return null;
            Assert.That(list.Query<Button>().ToList(),Is.Empty);
        }
        [Test] public void CombinedInputHasOneDirectionForEquivalentKeys()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try {
                foreach(var pair in new[] {
                    (new[]{Key.A},-1f), (new[]{Key.D},1f),
                    (new[]{Key.LeftArrow},-1f), (new[]{Key.RightArrow},1f),
                    (new[]{Key.A,Key.LeftArrow},-1f), (new[]{Key.D,Key.RightArrow},1f),
                    (new[]{Key.A,Key.RightArrow},0f), (new[]{Key.W,Key.Space},0f) })
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(pair.Item1)); InputSystem.Update();
                    Assert.That(WorkshopInput.ReadMovement(),Is.EqualTo(new Vector2(pair.Item2,0)));
                }
                var player=SceneItemService.Entries().Single(e=>e.item.definition.kind==ItemKind.Player).item;
                ((IItemBackend)player.GetComponent<PlaygroundAdapter>()).ApplyConfiguration();
                Assert.That(player.GetComponent<Playground.Movement.Move>().movementSource,Is.Not.Null);
            } finally { InputSystem.RemoveDevice(keyboard); }
        }
        [Test] public void ParticipantTerminologyAndCatalogAreConsistent()
        {
            var window=EditorWindow.GetWindow<CreaJuegoWindow>(); window.CreateGUI(); WorkshopTestWindows.Open();
            Assert.That(WorkshopTestWindows.Play.Q<Button>("jugar").text,Is.EqualTo("▶ JUGAR"));
            var player=ItemService.WorkshopCatalog().Single(d=>d.kind==ItemKind.Player);
            Assert.That(player.properties.Single(p=>p.path=="health").label,Is.EqualTo("Puntos de vida"));
            Assert.That(player.learningHint,Does.Contain("A/D"));
            Assert.That(ItemService.WorkshopCatalog().Length,Is.EqualTo(7));
            Assert.That(SceneObjects.All<UnityEngine.UI.Text>(UnityEngine.SceneManagement.SceneManager.GetActiveScene()).Any(t=>t.text.Contains("Resistencia")),Is.False);
        }
    }
}

