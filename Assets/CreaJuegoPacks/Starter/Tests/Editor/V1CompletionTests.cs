using System;
using System.Collections;
using System.IO;
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
using Object = UnityEngine.Object;

namespace CreaJuego.Starter.Tests
{
    public sealed class V1CompletionTests
    {
        private sealed class ResultCounter { public int count; public void Observe(GameSessionState state) { count++; } }
        private const string SavedScene = "Assets/CreaJuegoV1PersistenceTest.unity";
        [SetUp] public void Setup() { if (!Application.isPlaying) EditorSceneManager.OpenScene(DemoBuilder.ScenePath); }
        [TearDown] public void Cleanup()
        {
            if (Application.isPlaying) return;
            Undo.ClearAll(); EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            if (File.Exists(SavedScene)) AssetDatabase.DeleteAsset(SavedScene);
        }
        [UnityTearDown] public IEnumerator LeavePlay() { if (Application.isPlaying) yield return new ExitPlayMode(); }
        private static IEnumerator Wait(float seconds) { float until = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < until) yield return null; }
        private static GameItem Item(ItemKind kind) => Object.FindObjectsByType<GameItem>().First(i => i.definition.kind == kind);
        private static GameItem Create(ItemKind kind, Vector3 point) => ItemService.Create(ItemService.Catalog().Single(d => d.kind == kind), point);
        private static void Empty() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SceneService.Prepare(); }
        private static Keyboard Keyboard()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            return InputSystem.AddDevice<Keyboard>();
        }
        private static void Keys(Keyboard keyboard, params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        [Test] public void PreflightDetectsMissingDuplicateAndBrokenEssentials()
        {
            Assert.That(SceneService.Validate().All(c => c.passed), Is.True, string.Join("; ", SceneService.Validate().Where(c=>!c.passed).Select(c=>c.help)));
            var player = Item(ItemKind.Player);
            var duplicate = Object.Instantiate(player.gameObject);
            Assert.That(SceneService.Validate().Single(c => c.label == "Personaje").passed, Is.False);
            Object.DestroyImmediate(duplicate);
            Object.DestroyImmediate(player.gameObject);
            Assert.That(SceneService.Validate().Single(c => c.label == "Personaje").passed, Is.False);
            Object.DestroyImmediate(Item(ItemKind.Goal).gameObject);
            Assert.That(SceneService.Validate().Single(c => c.label == "Meta").passed, Is.False);
            Object.DestroyImmediate(Object.FindAnyObjectByType<DemoSession>());
            Assert.That(SceneService.Validate().Single(c => c.label == "Sesión").passed, Is.False);
            Assert.That(SceneService.TryPlay(out var checks), Is.False);
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
        }

        [Test] public void PreparationIsExplicitIdempotentAndUndoable()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(SceneService.Validate().Any(c => !c.passed), Is.True);
            SceneService.Prepare(); SceneService.Prepare();
            Assert.That(Object.FindObjectsByType<DemoSession>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>().Length, Is.EqualTo(1));
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(Object.FindObjectsByType<DemoSession>().Length, Is.Zero);
            Undo.PerformRedo();
            Assert.That(Object.FindObjectsByType<DemoSession>().Length, Is.EqualTo(1));
            Object.FindAnyObjectByType<DemoSession>().playgroundUI.numberLabels[1] = null;
            Assert.That(SceneService.Validate().Single(c => c.label == "Marcador").passed, Is.False);
        }

        [Test] public void MultiColorPreviewUndoAndPrefabOverrides()
        {
            var a = Item(ItemKind.Platform); var b = ItemService.Duplicate(a);
            Color before = a.tint;
            Undo.IncrementCurrentGroup();
            using (var serialized = new SerializedObject(new Object[] { a, b }))
            {
                serialized.FindProperty(nameof(GameItem.tint)).colorValue = Color.magenta;
                serialized.ApplyModifiedProperties();
            }
            Undo.FlushUndoRecordObjects();
            Assert.That(a.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.magenta));
            Assert.That(b.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.magenta));
            Assert.That(PrefabUtility.HasPrefabInstanceAnyOverrides(a.gameObject, false), Is.True);
            Undo.PerformUndo();
            Assert.That(a.tint, Is.EqualTo(before)); Assert.That(a.GetComponent<SpriteRenderer>().color, Is.EqualTo(before));
            Undo.PerformRedo(); Assert.That(b.tint, Is.EqualTo(Color.magenta)); Assert.That(b.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.magenta));
        }

        [UnityTest] public IEnumerator GroundAirWallLandingAndCanJumpContract()
        {
            Empty();
            Create(ItemKind.Platform, Vector3.zero);
            var wall = Create(ItemKind.Platform, new Vector3(1.5f,3,0)); wall.transform.rotation = Quaternion.Euler(0,0,90);
            Create(ItemKind.Player, new Vector3(0,1,0));
            yield return new EnterPlayMode();
            var keyboard = Keyboard(); var player = Item(ItemKind.Player); var body = player.GetComponent<Rigidbody2D>(); var gate = player.GetComponent<GroundedJumpGate>();
            yield return Wait(.5f);
            Assert.That(gate.Supported, Is.True, "Ground permits jumping");
            Keys(keyboard, Key.Space); yield return Wait(.08f);
            Assert.That(body.linearVelocity.y, Is.GreaterThan(1));
            Keys(keyboard); yield return Wait(.08f);
            float velocity = body.linearVelocity.y;
            Keys(keyboard, Key.Space); yield return Wait(.08f);
            Assert.That(body.linearVelocity.y, Is.LessThan(velocity), "No second jump in air");
            Keys(keyboard);
            body.gravityScale = 0; body.position = new Vector2(.92f,3); body.linearVelocity = new Vector2(1,0);
            yield return Wait(.2f);
            Assert.That(gate.Supported, Is.False, "Lateral wall is not ground");
            Keys(keyboard, Key.Space); yield return Wait(.08f);
            Assert.That(body.linearVelocity.y, Is.LessThan(.15f), "Wall contact does not rearm");
            Keys(keyboard); body.gravityScale = 3; body.position = new Vector2(0,1); body.linearVelocity = Vector2.zero;
            yield return Wait(.5f); Assert.That(gate.Supported, Is.True, "Landing rearms support");
            player.canJump = false; yield return null;
            Keys(keyboard, Key.Space); yield return Wait(.08f);
            Assert.That(body.linearVelocity.y, Is.LessThan(.15f));
            Keys(keyboard); yield return Wait(.08f); player.canJump = true; yield return null;
            Keys(keyboard, Key.Space); yield return Wait(.08f);
            Assert.That(body.linearVelocity.y, Is.GreaterThan(1), "Enabled and supported jumps");
            InputSystem.RemoveDevice(keyboard); yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator WinIsAtomicAndRejectsLateDamagePointsAndSecondResult()
        {
            yield return new EnterPlayMode(); yield return Wait(.1f);
            var session = Object.FindAnyObjectByType<DemoSession>(); var player = Item(ItemKind.Player);
            var health = player.GetComponent<HealthSystemAttribute>(); var body = player.GetComponent<Rigidbody2D>();
            var notifications = new ResultCounter(); session.ResultChanged += notifications.Observe;
            body.linearVelocity = new Vector2(4,3);
            session.Complete("¡Llegaste!");
            session.Complete("No repetir"); session.ObserveHealth(0); health.ModifyHealth(-100);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Won));
            Assert.That(notifications.count, Is.EqualTo(1)); Assert.That(health.health, Is.EqualTo(player.health));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(player.GetComponent<Move>().enabled, Is.False); Assert.That(player.GetComponent<Jump>().enabled, Is.False);
            var prize = Item(ItemKind.Prize);
            Assert.That(prize.GetComponent<CollectableAttribute>().interactionAllowed(), Is.False);
            Assert.That(Item(ItemKind.Hazard).GetComponent<ModifyHealthAttribute>().interactionAllowed(), Is.False);
            // Deliberately restore physics to prove the guard, not just collider disabling, blocks a late contact.
            body.simulated = true; player.GetComponent<Collider2D>().enabled = true; prize.GetComponent<Collider2D>().enabled = true;
            body.position = prize.transform.position; yield return Wait(.1f);
            Assert.That(prize != null, Is.True); Assert.That(session.playgroundUI.numberLabels[1].text, Is.EqualTo("0"));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Won));
            yield return new ExitPlayMode();
            yield return new EnterPlayMode(); yield return Wait(.1f);
            session = Object.FindAnyObjectByType<DemoSession>();
            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(Item(ItemKind.Player).GetComponent<HealthSystemAttribute>().health, Is.EqualTo(3));
            Assert.That(session.playgroundUI.numberLabels[1].text, Is.EqualTo("0"));
            Assert.That(Object.FindObjectsByType<GameItem>().Count(i => i.definition.kind == ItemKind.Prize), Is.EqualTo(2));
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator LethalDamageImmediatelyLosesAndCannotWin()
        {
            yield return new EnterPlayMode(); yield return Wait(.1f);
            var session = Object.FindAnyObjectByType<DemoSession>(); var notifications = new ResultCounter(); session.ResultChanged += notifications.Observe;
            var player = Item(ItemKind.Player); player.GetComponent<HealthSystemAttribute>().ModifyHealth(-10);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Lost));
            Assert.That(player.GetComponent<Move>().enabled, Is.False);
            session.Complete("No debería ganar"); session.ObserveHealth(0);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Lost)); Assert.That(notifications.count, Is.EqualTo(1));
            Assert.That(session.playgroundUI.gameOverPanel.activeSelf, Is.True);
            yield return Wait(.1f); Assert.That(player == null, Is.True);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator AuthoringSaveReopenUndoRedoAndPlayPreserveConfiguration()
        {
            Assert.That(File.Exists(SavedScene), Is.False);
            var prize = Create(ItemKind.Prize, new Vector3(10,2,0));
            using(var serialized = new SerializedObject(prize))
            {
                serialized.FindProperty(nameof(GameItem.points)).intValue = 23; serialized.ApplyModifiedProperties();
            }
            EditorSceneManager.SaveScene(prize.gameObject.scene, SavedScene);
            EditorSceneManager.OpenScene(SavedScene);
            prize = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Prize && i.points == 23);
            Undo.IncrementCurrentGroup();
            using(var serialized = new SerializedObject(prize))
            {
                serialized.FindProperty(nameof(GameItem.points)).intValue = 24; serialized.ApplyModifiedProperties();
            }
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(prize.points, Is.EqualTo(23));
            Undo.PerformRedo(); Assert.That(prize.points, Is.EqualTo(24));
            yield return new EnterPlayMode(); yield return Wait(.1f);
            Assert.That(Object.FindObjectsByType<GameItem>().Any(i=>i.points==24 && i.definition.kind==ItemKind.Prize), Is.True);
            yield return new ExitPlayMode();
            Assert.That(Object.FindObjectsByType<GameItem>().Any(i=>i.points==24 && i.definition.kind==ItemKind.Prize), Is.True);
        }

        [UnityTest] public IEnumerator NewSceneAuthoringFlowWinsThenLosesWithKeyboard()
        {
            Empty(); Create(ItemKind.Player, Vector3.zero);
            var ground = Create(ItemKind.Platform, new Vector3(0,-1,0)); ground.transform.localScale = new Vector3(6,1,1);
            var platformCopy = ItemService.Duplicate(ground); platformCopy.transform.position = new Vector3(-15,-1,0);
            var prize = Create(ItemKind.Prize, new Vector3(1,0,0)); var copy = ItemService.Duplicate(prize); copy.transform.position = new Vector3(-6,1,0);
            using(var serialized = new SerializedObject(prize)){ serialized.FindProperty(nameof(GameItem.points)).intValue=5; serialized.ApplyModifiedProperties(); }
            Create(ItemKind.Hazard, new Vector3(2,0,0));
            var goal = Create(ItemKind.Goal, new Vector3(4,0,0));
            using(var serialized = new SerializedObject(goal)){ serialized.FindProperty(nameof(GameItem.message)).stringValue="¡Llegaste!"; serialized.ApplyModifiedProperties(); }
            Assert.That(SceneService.Validate().All(c=>c.passed), Is.True);
            yield return new EnterPlayMode();
            var keyboard = Keyboard(); yield return Wait(.3f); Keys(keyboard, Key.RightArrow);
            var session = Object.FindAnyObjectByType<DemoSession>(); float deadline=Time.realtimeSinceStartup+8;
            while(session.State==GameSessionState.Playing && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(session.State, Is.EqualTo(GameSessionState.Won));
            Assert.That(session.status.text, Is.EqualTo("¡Llegaste!"));
            Assert.That(session.playgroundUI.numberLabels[1].text, Is.EqualTo("5"));
            Assert.That(Item(ItemKind.Player).GetComponent<HealthSystemAttribute>().health, Is.EqualTo(2));
            InputSystem.RemoveDevice(keyboard); yield return new ExitPlayMode();
            using(var serialized = new SerializedObject(Item(ItemKind.Hazard))){ serialized.FindProperty(nameof(GameItem.damage)).intValue=10; serialized.ApplyModifiedProperties(); }
            yield return new EnterPlayMode();
            keyboard=Keyboard(); yield return Wait(.3f); Keys(keyboard,Key.RightArrow);
            session=Object.FindAnyObjectByType<DemoSession>(); deadline=Time.realtimeSinceStartup+8;
            while(session.State==GameSessionState.Playing && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(session.State, Is.EqualTo(GameSessionState.Lost));
            InputSystem.RemoveDevice(keyboard); yield return new ExitPlayMode();
        }
    }
}

