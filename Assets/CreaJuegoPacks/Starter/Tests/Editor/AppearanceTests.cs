using System.Collections;
using System.Linq;
using CreaJuego.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace CreaJuego.Starter.Tests
{
    public sealed class AppearanceTests
    {
        static GameItem Player()=>Object.FindObjectsByType<GameItem>().First(i=>i.definition.kind==ItemKind.Player);
        [SetUp] public void Setup() { if(!Application.isPlaying) EditorSceneManager.OpenScene(DemoBuilder.ScenePath); }
        [TearDown] public void Cleanup() { if(!Application.isPlaying) { Undo.ClearAll(); EditorSceneManager.OpenScene(DemoBuilder.ScenePath); AssetDatabase.DeleteAsset("Assets/AppearancePersistenceTest.unity"); } }
        [UnityTearDown] public IEnumerator LeavePlay() { if(Application.isPlaying) yield return new ExitPlayMode(); }
        [Test] public void PackHasCompatibleDefaultsAndRedistributableSprites()
        {
            var player=Player(); var pack=player.definition.appearancePack;
            Assert.That(pack.appearances.Length,Is.EqualTo(30));
            Assert.That(pack.appearances.Select(a=>a.id).Distinct().Count(),Is.EqualTo(30));
            foreach(var a in pack.appearances) Assert.That(a.sprite,Is.Not.Null);
            foreach(var kind in new[]{ItemKind.Player,ItemKind.Platform,ItemKind.Prize,ItemKind.Hazard,ItemKind.Goal,ItemKind.Enemy}) Assert.That(pack.DefaultFor(kind).kind,Is.EqualTo(kind));
        }
        [Test] public void PreparedCustomUndoRedoPreservePhysics()
        {
            var item=Player(); var visual=item.GetComponent<ItemVisual>();
            var original=item.definition.appearancePack.appearances.First(a=>a.kind==ItemKind.Player && a.controller!=null); ItemAppearance.Choose(new[]{item},original,null); Undo.ClearAll(); var alternate=item.definition.appearancePack.appearances.First(a=>a.kind==ItemKind.Player && a.controller==null);
            string physics=EditorJsonUtility.ToJson(item.GetComponent<Rigidbody2D>()), collision=EditorJsonUtility.ToJson(item.GetComponent<Collider2D>());
            var scale=item.transform.localScale;
            ItemAppearance.Choose(new[]{item},alternate,null); Undo.FlushUndoRecordObjects();
            Assert.That(visual.renderer.sprite,Is.EqualTo(alternate.sprite)); Assert.That(visual.animator.enabled,Is.False);
            Undo.PerformUndo(); Assert.That(item.appearance,Is.EqualTo(original)); Assert.That(visual.renderer.sprite,Is.EqualTo(original.sprite));
            Undo.PerformRedo(); Assert.That(item.appearance,Is.EqualTo(alternate));
            ItemAppearance.Choose(new[]{item},alternate,original.sprite); Assert.That(item.customSprite,Is.EqualTo(original.sprite)); Assert.That(visual.animator.enabled,Is.False);
            ItemAppearance.Choose(new[]{item},original,null); Assert.That(item.customSprite,Is.Null); Assert.That(visual.animator.enabled,Is.True);
            Assert.That(EditorJsonUtility.ToJson(item.GetComponent<Rigidbody2D>()),Is.EqualTo(physics)); Assert.That(EditorJsonUtility.ToJson(item.GetComponent<Collider2D>()),Is.EqualTo(collision)); Assert.That(item.transform.localScale,Is.EqualTo(scale));
        }
        [Test] public void AppearancePersistsInSceneAndDuplicate()
        {
            var definition=ItemService.Catalog().First(d=>d.kind==ItemKind.Enemy);
            var item=ItemService.Create(definition,Vector3.zero); var a=definition.appearancePack.appearances.Last(x=>x.kind==ItemKind.Enemy);
            ItemAppearance.Choose(new[]{item},a,null); var copy=ItemService.Duplicate(item);
            Assert.That(copy.appearance,Is.EqualTo(a)); Assert.That(ItemVisual.Resolve(copy).sprite,Is.EqualTo(a.sprite));
            EditorSceneManager.SaveScene(item.gameObject.scene,"Assets/AppearancePersistenceTest.unity"); EditorSceneManager.OpenScene("Assets/AppearancePersistenceTest.unity");
            Assert.That(Object.FindObjectsByType<GameItem>().Count(x=>x.appearance==a),Is.GreaterThanOrEqualTo(2));
        }
        [Test] public void LegacyFallbackAndExplicitUpgradeAreUndoable()
        {
            var player=Player(); var root=new GameObject("Antiguo"); var item=root.AddComponent<GameItem>(); item.definition=player.definition;
            var renderer=root.AddComponent<SpriteRenderer>(); renderer.sprite=player.appearance.sprite;
            Assert.That(ItemVisual.Resolve(item),Is.EqualTo(renderer));
            ItemAppearance.Choose(new[]{item},player.appearance,null); Undo.FlushUndoRecordObjects(); Assert.That(renderer.enabled,Is.False);
            Undo.PerformUndo(); Assert.That(item.GetComponent<ItemVisual>(),Is.Null); Assert.That(renderer.enabled,Is.True);
            var custom=player.definition.appearancePack.appearances.Last(a=>a.kind==ItemKind.Player).sprite;
            ItemAppearance.Choose(new[]{item},null,custom); Assert.That(ItemVisual.Resolve(item).sprite,Is.EqualTo(custom));
            ItemAppearance.Choose(new[]{item},null,null); Assert.That(ItemVisual.Resolve(item).sprite,Is.EqualTo(renderer.sprite));
            Object.DestroyImmediate(root);
        }
        [Test] public void SelectorShowsOnlyCompatibleThumbnailsAndCustomSpriteField()
        {
            var player=Player(); Selection.activeGameObject=player.gameObject;
            var view=new EducationalPropertiesView();
            try
            {
                view.ShowSelection();
                var selector=UnityEngine.UIElements.UQueryExtensions.Q<AppearanceSelector>(view);
                Assert.That(selector,Is.Not.Null);
                int expected=player.definition.appearancePack.CategoryFor(ItemKind.Player).options.Count(a=>a.Preview!=null);
                Assert.That(UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Image>(selector).ToList().Count(image=>image.sprite!=null),Is.EqualTo(expected));
                Assert.That(UnityEngine.UIElements.UQueryExtensions.Q<UnityEditor.UIElements.ObjectField>(selector).objectType,Is.EqualTo(typeof(Sprite)));
            }
            finally { view.Dispose(); }
        }
        [UnityTest] public IEnumerator StaticCustomSpriteStillUsesPlaygroundMovement()
        {
            var item=Player(); var a=item.definition.appearancePack.appearances.Last(x=>x.kind==ItemKind.Player);
            ItemAppearance.Choose(new[]{item},a,a.sprite);
            yield return new EnterPlayMode();
            item=Player(); var visual=item.GetComponent<ItemVisual>(); Assert.That(visual.animator.enabled,Is.False);
            yield return null;
            var move=item.GetComponent<Playground.Movement.Move>(); move.movementSource=()=>Vector2.right;
            float x=item.transform.position.x, until=Time.realtimeSinceStartup+.3f;
            while(Time.realtimeSinceStartup<until) yield return null;
            Assert.That(item.transform.position.x,Is.GreaterThan(x+.02f));
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator OptionalAnimatorMissingStatesAndFacingDoNotChangeRoot()
        {
            yield return new EnterPlayMode();
            var item=Player(); var visual=item.GetComponent<ItemVisual>();
            var profile=ScriptableObject.CreateInstance<AnimationProfile>(); profile.idle="Missing"; profile.move="Missing"; profile.jump="Missing";
            var appearance=Object.Instantiate(item.appearance); appearance.animationProfile=profile; item.appearance=appearance; visual.Apply();
            yield return null;
            Object.Destroy(visual.animator); yield return null;
            var originalScale=item.transform.localScale; var originalRotation=item.transform.rotation;
            item.transform.position+=Vector3.left; yield return null;
            Assert.That(visual.renderer.flipX,Is.True); Assert.That(item.transform.localScale,Is.EqualTo(originalScale)); Assert.That(item.transform.rotation,Is.EqualTo(originalRotation));
            LogAssert.NoUnexpectedReceived(); yield return new ExitPlayMode();
        }
    }
}
