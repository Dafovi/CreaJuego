using System.Collections;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.PlaygroundBackend;
using NUnit.Framework;
using Playground.Attributes;
using Playground.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
namespace CreaJuego.Starter.Tests
{
    public sealed class CombatMvpTests
    {
        [SetUp] public void Setup() { if(!Application.isPlaying) EditorSceneManager.OpenScene(DemoBuilder.ScenePath); }
        [TearDown] public void Cleanup() { if(!Application.isPlaying) { WorkshopTestWindows.Close(); Undo.ClearAll(); EditorSceneManager.OpenScene(DemoBuilder.ScenePath); } }
        [UnityTearDown] public IEnumerator LeavePlay() { if(Application.isPlaying) yield return new ExitPlayMode(); }
        static GameItem Item(ItemKind kind)=>Object.FindObjectsByType<GameItem>().First(i=>i.definition.kind==kind);
        static GameItem Create(ItemKind kind,Vector3 point)=>ItemService.Create(ItemService.Catalog().Single(d=>d.kind==kind),point);
        static IEnumerator Wait(float seconds) { float end=Time.realtimeSinceStartup+seconds; while(Time.realtimeSinceStartup<end) yield return null; }
        [UnityTest] public IEnumerator PropertiesFollowRootsVisualChildrenComponentsAndUndo()
        {
            WorkshopTestWindows.Open();
            foreach(var kind in new[]{ItemKind.Player,ItemKind.Platform,ItemKind.Prize,ItemKind.Hazard,ItemKind.Goal,ItemKind.Enemy,ItemKind.Decoration})
            {
                var item=Object.FindObjectsByType<GameItem>().FirstOrDefault(i=>i.definition.kind==kind) ?? Create(kind,new Vector3(30,0,0));
                Selection.activeObject=item.gameObject; yield return null;
                Assert.That(WorkshopTestWindows.Properties.Q<Label>(className:"selection-title").text,Is.EqualTo(SceneItemService.Label(item)));
                Selection.activeObject=item.GetComponent<ItemVisual>().renderer.gameObject; yield return null;
                Assert.That(WorkshopTestWindows.Properties.Q<Label>(className:"selection-title").text,Is.EqualTo(SceneItemService.Label(item)),"SceneView-picked Visual resolves its authoring root");
                Selection.activeObject=item.GetComponent<ItemVisual>().renderer; yield return null;
                Assert.That(EducationalSelection.Items().Single(),Is.EqualTo(item));
            }
            Undo.ClearAll(); Undo.IncrementCurrentGroup();
            var prize=Item(ItemKind.Prize); Selection.activeObject=prize.gameObject; yield return null;
            using(var data=new SerializedObject(prize)) { data.FindProperty(nameof(GameItem.points)).intValue=8; data.ApplyModifiedProperties(); }
            Undo.FlushUndoRecordObjects(); Selection.activeObject=Item(ItemKind.Player).gameObject; yield return null; Undo.PerformUndo(); yield return null;
            // Unity may restore its own selection together with Undo. Follow that selection.
            Assert.That(WorkshopTestWindows.Properties.Q<Label>(className:"selection-title").text,Is.EqualTo(SceneItemService.Label(EducationalSelection.Items().Single())));
            Assert.That(prize.points,Is.Not.EqualTo(8));
            Undo.PerformRedo(); yield return null;
            Assert.That(WorkshopTestWindows.Properties.Q<Label>(className:"selection-title").text,Is.EqualTo(SceneItemService.Label(EducationalSelection.Items().Single())));
            Assert.That(prize.points,Is.EqualTo(8));
            Selection.activeObject=Item(ItemKind.Player).gameObject; yield return null;
            Assert.That(WorkshopTestWindows.Properties.Q<VisualElement>("fila-speed"),Is.Not.Null);
            Assert.That(WorkshopTestWindows.Properties.Q<VisualElement>("fila-points"),Is.Null);
        }
        [UnityTest] public IEnumerator CreationDuplicationDeletionAndUndoNeverLeaveStaleBindings()
        {
            WorkshopTestWindows.Open(); var item=Create(ItemKind.Enemy,Vector3.zero); yield return null;
            var copy=ItemService.Duplicate(item); yield return null;
            Assert.That(EducationalSelection.Items().Single(),Is.EqualTo(copy));
            Assert.That(WorkshopTestWindows.Properties.Q<Label>(className:"selection-title").text,Is.EqualTo(SceneItemService.Label(copy)));
            ItemService.Delete(copy); yield return null;
            Assert.That(WorkshopTestWindows.Properties.Q<VisualElement>("fila-health"),Is.Null);
            Undo.PerformUndo(); Selection.activeObject=item.gameObject; yield return null;
            Assert.That(WorkshopTestWindows.Properties.Q<VisualElement>("fila-health"),Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }
        [Test] public void PreparedAndCustomArtAreWhiteWithoutDiscardingLegacyAuthoringTint()
        {
            var item=Item(ItemKind.Player); item.tint=Color.magenta;
            ItemAppearance.Apply(item,true); Assert.That(ItemVisual.Resolve(item).color,Is.EqualTo(Color.white));
            var a=item.definition.appearancePack.appearances.Last(x=>x.kind==ItemKind.Player);
            ItemAppearance.Choose(new[]{item},a,a.sprite); Undo.FlushUndoRecordObjects(); Assert.That(ItemVisual.Resolve(item).color,Is.EqualTo(Color.white));
            Undo.PerformUndo(); Assert.That(ItemVisual.Resolve(item).color,Is.EqualTo(Color.white));
            Selection.activeObject=item.gameObject; var view=new EducationalPropertiesView();
            try { view.ShowSelection(); Assert.That(view.Q<VisualElement>("fila-tint"),Is.Null); } finally { view.Dispose(); }
        }
        [Test] public void PackExposesReadableRewardsThreatsAndNonBlockingDecoration()
        {
            var pack=Item(ItemKind.Player).definition.appearancePack;
            Assert.That(pack.appearances.Where(a=>a.kind==ItemKind.Prize).Select(a=>a.displayName),Is.EquivalentTo(new[]{"Moneda dorada","Gema azul","Llave dorada"}));
            Assert.That(pack.appearances.Where(a=>a.kind==ItemKind.Hazard).Select(a=>a.displayName),Is.EquivalentTo(new[]{"Pinchos","Trampa roja"}));
            Assert.That(pack.appearances.Count(a=>a.kind==ItemKind.Decoration),Is.EqualTo(14));
            Assert.That(pack.appearances.All(a=>a.sprite!=null),Is.True);
            var decoration=Create(ItemKind.Decoration,Vector3.zero);
            Assert.That(decoration.GetComponentsInChildren<Collider2D>(),Is.Empty);
            Assert.That(decoration.GetComponent<PlaygroundContactDamage>(),Is.Null);
            Assert.That(decoration.GetComponent<CollectableAttribute>(),Is.Null);
            Selection.activeObject=decoration.gameObject; WorkshopTestWindows.Open();
            var selector=WorkshopTestWindows.Properties.Q<AppearanceSelector>(); selector.Q<ToolbarSearchField>().value="Cofre";
            Assert.That(selector.Q<ScrollView>("lista-apariencias").Query<Button>().ToList().Count(b=>b.style.display.value!=DisplayStyle.None),Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator LegacyColorControlHidesAfterAssigningArtWithoutReselection()
        {
            var definition=Object.Instantiate(Item(ItemKind.Player).definition);
            definition.properties=definition.properties.Where(p=>p.path!=nameof(GameItem.tint)).Concat(new[]{new EducationalProperty {path=nameof(GameItem.tint),label="Color",group="Apariencia",control=EducationalControl.Color}}).ToArray();
            var root=new GameObject("Placeholder antiguo"); var item=root.AddComponent<GameItem>(); item.definition=definition; item.tint=Color.blue;
            root.AddComponent<SpriteRenderer>().sprite=item.definition.appearancePack.DefaultFor(ItemKind.Player).sprite;
            Selection.activeObject=root; WorkshopTestWindows.Open(); yield return null;
            Assert.That(WorkshopTestWindows.Properties.Q<VisualElement>("fila-tint"),Is.Not.Null);
            ItemAppearance.Choose(new[]{item},null,item.definition.appearancePack.DefaultFor(ItemKind.Player).sprite);
            yield return null; yield return null;
            Assert.That(Selection.activeGameObject,Is.EqualTo(root));
            Assert.That(WorkshopTestWindows.Properties.Q<VisualElement>("fila-tint"),Is.Null);
            Assert.That(ItemVisual.Resolve(item).color,Is.EqualTo(Color.white));
            ItemAppearance.Choose(new[]{item},null,null); yield return null; yield return null;
            Assert.That(WorkshopTestWindows.Properties.Q<VisualElement>("fila-tint"),Is.Not.Null);
            Object.DestroyImmediate(root); Object.DestroyImmediate(definition);
        }
        [Test] public void CombatDescriptorsExposeOnlyEducationalSettings()
        {
            var player=ItemService.Catalog().Single(d=>d.kind==ItemKind.Player);
            Assert.That(player.properties.Single(p=>p.path==nameof(GameItem.canAttack)).label,Is.EqualTo("Puede golpear"));
            Assert.That(player.properties.Single(p=>p.path==nameof(GameItem.attackDamage)).visibleWhen,Is.EqualTo(nameof(GameItem.canAttack)));
            var enemy=ItemService.Catalog().Single(d=>d.kind==ItemKind.Enemy);
            Assert.That(enemy.properties.Single(p=>p.path==nameof(GameItem.health)).maximum,Is.EqualTo(5));
        }
        [UnityTest] public IEnumerator DamageFlashesRestoresAndHonorsGraceInterval()
        {
            yield return new EnterPlayMode(); yield return null;
            var player=Item(ItemKind.Player); var body=player.GetComponent<Rigidbody2D>(); body.gravityScale=0; body.position=new Vector2(-30,20);
            var receiver=player.GetComponent<PlayerDamageReceiver>(); var health=player.GetComponent<HealthSystemAttribute>();
            var feedback=player.GetComponent<DamageFeedback>(); var renderer=ItemVisual.Resolve(player); var original=renderer.color; float mass=body.mass, speed=player.speed;
            Assert.That(receiver.TryReceive(1,body.position+Vector2.left),Is.True);
            Assert.That(health.health,Is.EqualTo(2)); Assert.That(feedback.IsFlashing,Is.True); Assert.That(renderer.color,Is.Not.EqualTo(original));
            Assert.That(receiver.TryReceive(1,body.position),Is.False); Assert.That(health.health,Is.EqualTo(2));
            yield return Wait(.2f); Assert.That(renderer.color,Is.EqualTo(original)); Assert.That(feedback.IsFlashing,Is.False);
            Assert.That(receiver.TryReceive(1,body.position),Is.False);
            yield return Wait(.5f); Assert.That(receiver.TryReceive(1,body.position),Is.True); Assert.That(health.health,Is.EqualTo(1));
            Assert.That(player.health,Is.EqualTo(3)); Assert.That(player.speed,Is.EqualTo(speed)); Assert.That(body.mass,Is.EqualTo(mass));
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator KeyboardAttackFacingToggleStaticFallbackDeathAndRestore()
        {
            var player=Item(ItemKind.Player); player.transform.position=new Vector3(-20,20,0);
            var appearance=player.definition.appearancePack.appearances.Last(a=>a.kind==ItemKind.Player);
            ItemAppearance.Choose(new[]{player},appearance,appearance.sprite);
            var right=Create(ItemKind.Enemy,new Vector3(-19.2f,20,0)); right.name="Derecha combate"; right.health=2; right.speed=0;
            var left=Create(ItemKind.Enemy,new Vector3(-20.8f,20,0)); left.name="Izquierda combate"; left.health=2; left.speed=0;
            var prize=Create(ItemKind.Prize,new Vector3(-19,20,0)); prize.name="Premio intacto";
            var goal=Create(ItemKind.Goal,new Vector3(-19,20,0)); goal.name="Meta intacta";
            var deco=Create(ItemKind.Decoration,new Vector3(-19,20,0)); deco.name="Decoración intacta";
            yield return new EnterPlayMode(); yield return null;
            player=Item(ItemKind.Player); var body=player.GetComponent<Rigidbody2D>(); body.gravityScale=0; body.linearVelocity=Vector2.zero;
            right=GameObject.Find("Derecha combate").GetComponent<GameItem>(); left=GameObject.Find("Izquierda combate").GetComponent<GameItem>();
            right.GetComponent<Patrol>().enabled=false; left.GetComponent<Patrol>().enabled=false;
            var visual=player.GetComponent<ItemVisual>(); Object.Destroy(visual.animator); yield return null;
            var attack=player.GetComponent<PlayerAttack>(); var rightHealth=right.GetComponent<EnemyVitality>(); var leftHealth=left.GetComponent<EnemyVitality>();
            var keys=InputSystem.AddDevice<Keyboard>(); InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            visual.renderer.flipX=false; InputSystem.QueueStateEvent(keys,new KeyboardState(Key.X)); yield return Wait(.08f);
            Assert.That(attack.AttackCount,Is.EqualTo(1)); Assert.That(rightHealth.Remaining,Is.EqualTo(1)); Assert.That(leftHealth.Remaining,Is.EqualTo(2));
            Assert.That(right.GetComponent<DamageFeedback>().IsFlashing,Is.True);
            InputSystem.QueueStateEvent(keys,new KeyboardState()); yield return Wait(.4f);
            visual.renderer.flipX=true; InputSystem.QueueStateEvent(keys,new KeyboardState(Key.X)); yield return Wait(.05f);
            Assert.That(leftHealth.Remaining,Is.EqualTo(1)); Assert.That(attack.LastCenter.x,Is.LessThan(player.transform.position.x));
            InputSystem.QueueStateEvent(keys,new KeyboardState()); yield return Wait(.4f); player.canAttack=false;
            InputSystem.QueueStateEvent(keys,new KeyboardState(Key.X)); yield return Wait(.05f); Assert.That(attack.AttackCount,Is.EqualTo(2));
            player.canAttack=true; visual.renderer.flipX=false; Assert.That(attack.TryAttack(),Is.True); yield return Wait(.2f);
            Assert.That(rightHealth.Defeated,Is.True); Assert.That(right.gameObject.activeSelf,Is.False); Assert.That(right.health,Is.EqualTo(2));
            Assert.That(GameObject.Find("Premio intacto"),Is.Not.Null); Assert.That(GameObject.Find("Meta intacta"),Is.Not.Null); Assert.That(GameObject.Find("Decoración intacta"),Is.Not.Null);
            Assert.That(body.simulated,Is.True); Assert.That(player.GetComponent<Collider2D>().enabled,Is.True);
            InputSystem.RemoveDevice(keys); yield return new ExitPlayMode();
            Assert.That(GameObject.Find("Derecha combate").activeSelf,Is.True);
            Assert.That(GameObject.Find("Derecha combate").GetComponent<GameItem>().health,Is.EqualTo(2));
        }
    }
}






