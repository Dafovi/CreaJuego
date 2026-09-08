using System.Collections;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.PlaygroundBackend;
using NUnit.Framework;
using Playground.Attributes;
using Playground.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CreaJuego.Starter.Tests
{
    public sealed class SpikeTests
    {
        [SetUp] public void Setup() { if (!Application.isPlaying) EditorSceneManager.OpenScene(DemoBuilder.ScenePath); }
        [TearDown] public void Cleanup() { if (!Application.isPlaying) { Undo.ClearAll(); EditorSceneManager.OpenScene(DemoBuilder.ScenePath); } }

        private static IEnumerator Wait(float seconds) { float until = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < until) yield return null; }
        [UnityTearDown] public IEnumerator LeavePlay() { if (Application.isPlaying) yield return new ExitPlayMode(); }
        [Test] public void CatalogHasUniqueIdsAndValidEducationalBindings()
        {
            var catalog = ItemService.Catalog();
            Assert.That(catalog.Length, Is.EqualTo(8));
            Assert.That(catalog.Select(d => d.id).Distinct().Count(), Is.EqualTo(catalog.Length));
            foreach (var d in catalog)
            {
                Assert.That(d.prefab, Is.Not.Null);
                var item = d.prefab.GetComponent<GameItem>();
                Assert.That(ItemService.Backend(item), Is.Not.Null);
                using var serialized = new SerializedObject(item);
                foreach (var p in d.properties) Assert.That(serialized.FindProperty(p.path), Is.Not.Null, d.id + ":" + p.path);
            }
        }

        [Test] public void CreateUndoRedoPreservesPrefabAndSelection()
        {
            var definition = ItemService.Catalog().Single(d => d.kind == ItemKind.Prize);
            var item = ItemService.Create(definition, new Vector3(2,3,0));
            Assert.That(Selection.activeGameObject, Is.EqualTo(item.gameObject));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(item), Is.True);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(item == null, Is.True);
            Undo.PerformRedo();
            Assert.That(Object.FindObjectsByType<GameItem>().Any(i => i.definition == definition && i.transform.position == new Vector3(2,3,0)), Is.True);
        }

        [UnityTest] public IEnumerator WindowShowsCatalogAndBindsEducationalSliders()
        {
            var window = EditorWindow.GetWindow<CreaJuegoWindow>();
            window.CreateGUI();
            var definition = ItemService.Catalog().Single(d => d.kind == ItemKind.Player);
            var item = Object.FindObjectsByType<GameItem>().Single(i => i.definition == definition);
            Selection.activeGameObject = item.gameObject;
            yield return null;
            Assert.That(window.rootVisualElement.Q<Button>("crear-jugador"), Is.Not.Null);
            Assert.That(window.rootVisualElement.Q<Button>("crear-meta"), Is.Not.Null);
            Assert.That(window.rootVisualElement.Q<Button>("jugar").text, Is.EqualTo("▶ JUGAR"));
            var sliders = window.rootVisualElement.Q<ScrollView>("propiedades").Query<Slider>().ToList().Where(s => s.label == "Velocidad" || s.label == "Fuerza de salto").ToList();
            Assert.That(sliders.Count, Is.EqualTo(2));
            sliders.Single(s => s.label == "Velocidad").value = 3;
            sliders.Single(s => s.label == "Fuerza de salto").value = 15;
            yield return null;
            Assert.That(item.speed, Is.EqualTo(3)); Assert.That(item.jump, Is.EqualTo(15));
            window.Close();
        }

        [Test] public void SerializedMultiEditUndoAndBackendMapping()
        {
            var definition = ItemService.Catalog().Single(d => d.kind == ItemKind.Player);
            var a = Object.FindObjectsByType<GameItem>().Single(i => i.definition == definition);
            // Advanced Unity authoring can still produce several players; bindings must handle mixed selection.
            var b = ((GameObject)PrefabUtility.InstantiatePrefab(definition.prefab)).GetComponent<GameItem>();
            float before = a.speed;
            Undo.IncrementCurrentGroup();
            using var serialized = new SerializedObject(new Object[] { a,b });
            serialized.FindProperty(nameof(GameItem.speed)).floatValue = 3.25f;
            serialized.FindProperty(nameof(GameItem.jump)).floatValue = 12;
            serialized.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            Assert.That(a.speed, Is.EqualTo(3.25f)); Assert.That(b.speed, Is.EqualTo(3.25f));
            Assert.That(PrefabUtility.HasPrefabInstanceAnyOverrides(a.gameObject,false), Is.True);
            ItemService.Backend(a).ApplyConfiguration();
            Assert.That(a.GetComponent<Move>().speed, Is.EqualTo(3.25f / 20f));
            Assert.That(a.GetComponent<Jump>().jumpStrength, Is.EqualTo(12));
            Undo.PerformUndo(); Assert.That(a.speed, Is.EqualTo(before)); Assert.That(b.speed, Is.EqualTo(before));
            Undo.PerformRedo(); Assert.That(a.speed, Is.EqualTo(3.25f));
        }

        [UnityTest] public IEnumerator RealPlaygroundMovementCollectDamageGoalAndPersistence()
        {
            var authoredPlayer = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Player);
            using (var serialized = new SerializedObject(authoredPlayer))
            {
                serialized.FindProperty(nameof(GameItem.speed)).floatValue = 2;
                serialized.FindProperty(nameof(GameItem.jump)).floatValue = 11;
                serialized.ApplyModifiedProperties();
            }
            yield return new EnterPlayMode();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var player = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Player);
            var body = player.GetComponent<Rigidbody2D>();
            yield return Wait(.5f);
            float initialX = body.position.x;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            

            yield return Wait(.3f);
            Assert.That(body.position.x, Is.GreaterThan(initialX + .1f), "Real Move reads the game keyboard and moves the player");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); 
            yield return Wait(.1f);
            float initialY = body.position.y;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); 
            yield return Wait(.1f);
            Assert.That(body.position.y, Is.GreaterThan(initialY + .15f), "Real Jump applies impulse");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); 
            var moving = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.MovingPlatform);
            float platformX = moving.transform.position.x;
            yield return Wait(.2f);
            Assert.That(moving.transform.position.x, Is.GreaterThan(platformX), "Real Patrol moves the platform");
            var prize = Object.FindObjectsByType<GameItem>().First(i => i.definition.kind == ItemKind.Prize);
            body.position = prize.transform.position; body.linearVelocity = Vector2.zero;
            yield return Wait(.1f);
            Assert.That(prize == null, Is.True, "Real Collectable destroys the reward on trigger");
            var session = Object.FindAnyObjectByType<DemoSession>();
            Assert.That(session.playgroundUI.numberLabels[1].text, Is.EqualTo("1"));
            var hazard = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Hazard);
            body.position = hazard.transform.position; body.linearVelocity = Vector2.zero;
            yield return Wait(.1f);
            Assert.That(player.GetComponent<HealthSystemAttribute>().health, Is.EqualTo(2));
            Assert.That(hazard == null, Is.True);
            var goal = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Goal);
            body.position = goal.transform.position; body.linearVelocity = Vector2.zero;
            yield return Wait(.1f);
            Assert.That(session.Completed, Is.True, "Real ConditionArea invokes the CreaJuego goal action");
            Assert.That(session.status.text, Is.EqualTo(goal.message));
            InputSystem.RemoveDevice(keyboard);
            yield return new ExitPlayMode();
            authoredPlayer = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Player);
            Assert.That(authoredPlayer.speed, Is.EqualTo(2)); Assert.That(authoredPlayer.jump, Is.EqualTo(11));
            Assert.That(Object.FindObjectsByType<GameItem>().Count(i => i.definition.kind == ItemKind.Prize), Is.EqualTo(2), "Play mode destruction does not alter authored scene");
        }

        [UnityTest] public IEnumerator BothControlSchemesDriveTheSamePlaygroundBody()
        {
            yield return new EnterPlayMode();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            var player=Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player);
            var body=player.GetComponent<Rigidbody2D>();
            try
            {
                foreach(var keys in new[] {new[]{Key.A},new[]{Key.D},new[]{Key.LeftArrow},new[]{Key.RightArrow},new[]{Key.A,Key.LeftArrow},new[]{Key.D,Key.RightArrow}})
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    body.position=new Vector2(-7,-1.2f); body.linearVelocity=Vector2.zero;
                    yield return Wait(.15f);
                    float start=body.position.x;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
                    yield return Wait(.2f);
                    float sign=keys[0]==Key.A || keys[0]==Key.LeftArrow ? -1 : 1;
                    Assert.That((body.position.x-start)*sign,Is.GreaterThan(.05f),keys[0].ToString());
                    // Exact combined magnitude is covered synchronously by CombinedInputHasOneDirectionForEquivalentKeys.
                }
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                body.position=new Vector2(-7,-1.2f); body.linearVelocity=Vector2.zero;
                yield return Wait(.4f);
                float groundedY=body.position.y;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
                yield return Wait(.1f);
                Assert.That(body.position.y,Is.LessThanOrEqualTo(groundedY+.05f),"W does not jump");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.Space));
                yield return Wait(.1f);
                Assert.That(body.position.y,Is.GreaterThan(groundedY+.15f),"Space still jumps with D held");
            }
            finally {InputSystem.RemoveDevice(keyboard);}
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator RealPlaygroundHazardCanLoseGame()
        {
            yield return new EnterPlayMode();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
            var player = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Player);
            var hazard = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Hazard);
            hazard.GetComponent<ModifyHealthAttribute>().healthChange = -10;
            var session = Object.FindAnyObjectByType<DemoSession>();
            player.GetComponent<Rigidbody2D>().position = hazard.transform.position;
            yield return Wait(.15f);
            Assert.That(player == null, Is.True);
            Assert.That(session.playgroundUI.gameOverPanel.activeSelf, Is.True);
            InputSystem.RemoveDevice(keyboard);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator DemoCanBeCompletedWithKeyboardWithoutTeleporting()
        {
            yield return new EnterPlayMode();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var player = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Player);
            var session = Object.FindAnyObjectByType<DemoSession>();
            yield return Wait(.3f);
            float deadline = Time.realtimeSinceStartup + 12;
            while (player != null && !session.Completed && Time.realtimeSinceStartup < deadline)
            {
                float x = player.transform.position.x;
                bool jump = (x > -4.7f && x < -1.3f) || (x > -.5f && x < 2);
                InputSystem.QueueStateEvent(keyboard, jump ? new KeyboardState(Key.RightArrow,Key.Space) : new KeyboardState(Key.RightArrow));
                yield return Wait(.12f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.RightArrow));
                yield return Wait(.04f);
            }
            Assert.That(session.Completed, Is.True, "Continuous keyboard route; final position=" + (player == null ? "lost" : player.transform.position.ToString()));
            InputSystem.RemoveDevice(keyboard);
            yield return new ExitPlayMode();
        }
    }
}









