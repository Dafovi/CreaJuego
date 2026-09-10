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
    public sealed class AppearanceCategoryTests
    {
        const string Folder="Assets/CategoryTestData";
        AppearanceCategory category;
        static GameItem Player()=>Object.FindObjectsByType<GameItem>().First(i=>i.definition.kind==ItemKind.Player);
        [SetUp] public void Setup()
        {
            if(Application.isPlaying) return;
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets","CategoryTestData");
            category=ScriptableObject.CreateInstance<AppearanceCategory>(); category.kind=ItemKind.Player;
            category.options.Add(new AppearanceOption{displayName="Uno",sprite=Player().appearance.sprite});
            category.options.Add(new AppearanceOption{displayName="Dos",sprite=Player().definition.appearancePack.appearances.First(a=>a.kind==ItemKind.Player && a.sprite!=Player().appearance.sprite).sprite});
            category.EnsureIds(); AssetDatabase.CreateAsset(category,Folder+"/Jugador.asset");
        }
        [TearDown] public void Cleanup()
        {
            if(Application.isPlaying) return;
            Undo.ClearAll(); EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            AssetDatabase.DeleteAsset(Folder);
        }
        [UnityTearDown] public IEnumerator LeavePlay(){if(Application.isPlaying) yield return new ExitPlayMode();}
        [Test] public void PackHasOneEditableListPerCategoryAndMigrationIsIdempotent()
        {
            var pack=Player().definition.appearancePack;
            Assert.That(pack.categories.Length,Is.EqualTo(9));
            Assert.That(pack.categories.Select(c=>c.kind).Distinct().Count(),Is.EqualTo(9));
            Assert.That(pack.categories.Sum(c=>c.options.Count),Is.GreaterThanOrEqualTo(33));
            var before=pack.categories.Select(c=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c))).ToArray();
            AppearanceCategoryMigration.Run();
            Assert.That(pack.categories.Select(c=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c))),Is.EqualTo(before));
        }
        [Test] public void SelectionUsesStableIdAfterRenameReorderAndReload()
        {
            var player=Player(); var chosen=category.options[1]; string id=chosen.id;
            ItemAppearance.ChooseOption(new[]{player},category,id);
            category.options.Reverse(); chosen.displayName="Renombrada"; category.EnsureIds();
            Assert.That(player.SelectedAppearance.id,Is.EqualTo(id));
            Assert.That(player.SelectedAppearance.displayName,Is.EqualTo("Renombrada"));
            EditorUtility.SetDirty(category); AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(player.gameObject.scene,Folder+"/Persistencia.unity");
            EditorSceneManager.OpenScene(Folder+"/Persistencia.unity");
            Assert.That(Player().SelectedAppearance.id,Is.EqualTo(id));
            Assert.That(ItemVisual.Resolve(Player()).sprite,Is.EqualTo(chosen.sprite));
        }
        [Test] public void InlineChoiceAndCustomSpritePreserveUndoAndPhysics()
        {
            var player=Player(); var old=player.appearance;
            var body=EditorJsonUtility.ToJson(player.GetComponent<Rigidbody2D>());
            var collider=EditorJsonUtility.ToJson(player.GetComponent<Collider2D>());
            ItemAppearance.ChooseOption(new[]{player},category,category.options[0].id); Undo.FlushUndoRecordObjects();
            Undo.PerformUndo(); Assert.That(player.appearance,Is.EqualTo(old)); Assert.That(player.appearanceCategory,Is.Null);
            Undo.PerformRedo(); Assert.That(player.SelectedAppearance.id,Is.EqualTo(category.options[0].id));
            ItemAppearance.SetCustomSprite(new[]{player},category.options[1].sprite);
            ItemAppearance.SetCustomSprite(new[]{player},null);
            Assert.That(player.appearanceCategory,Is.EqualTo(category));
            Assert.That(ItemVisual.Resolve(player).sprite,Is.EqualTo(category.options[0].sprite));
            Assert.That(EditorJsonUtility.ToJson(player.GetComponent<Rigidbody2D>()),Is.EqualTo(body));
            Assert.That(EditorJsonUtility.ToJson(player.GetComponent<Collider2D>()),Is.EqualTo(collider));
        }
        [Test] public void PrefabReferenceExtractsOnlyVisualData()
        {
            var root=new GameObject("Fuente",typeof(SpriteRenderer),typeof(BoxCollider2D),typeof(Rigidbody2D));
            root.GetComponent<SpriteRenderer>().sprite=category.options[1].sprite;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Fuente.prefab"); Object.DestroyImmediate(root);
            category.options[0].sprite=null; category.options[0].prefab=prefab;
            var player=Player(); int count=player.GetComponents<Component>().Length;
            ItemAppearance.ChooseOption(new[]{player},category,category.options[0].id);
            Assert.That(ItemVisual.Resolve(player).sprite,Is.EqualTo(category.options[1].sprite));
            Assert.That(player.GetComponents<Component>().Length,Is.EqualTo(count));
            Assert.That(player.GetComponentsInChildren<Collider2D>().Length,Is.EqualTo(1));
        }
        [Test] public void DuplicatingAnOptionGetsNewIdAndRemovingSelectionIsExplicit()
        {
            string original=category.options[0].id;
            category.options.Add(new AppearanceOption{id=original}); category.EnsureIds();
            Assert.That(category.options.Select(o=>o.id).Distinct().Count(),Is.EqualTo(3));
            Assert.That(category.options[0].id,Is.EqualTo(original));
            var player=Player(); ItemAppearance.ChooseOption(new[]{player},category,original);
            category.options.RemoveAt(0);
            Assert.That(player.SelectedAppearance,Is.Null);
            Selection.activeGameObject=player.gameObject;
            using(var view=new EducationalPropertiesView()){
                view.ShowSelection();
                Assert.That(view.Query<HelpBox>().ToList().Any(h=>h.text.Contains("ya no está")),Is.True);
            }
        }
        [Test] public void NewItemsUseFirstValidOptionAndDuplicateKeepsChoice()
        {
            var definition=ItemService.Catalog().First(d=>d.kind==ItemKind.Enemy);
            var list=definition.appearancePack.CategoryFor(ItemKind.Enemy);
            var item=ItemService.Create(definition,Vector3.zero);
            Assert.That(item.appearanceCategory,Is.EqualTo(list));
            Assert.That(item.appearanceId,Is.EqualTo(list.options[0].id));
            var copy=ItemService.Duplicate(item);
            Assert.That(copy.appearanceId,Is.EqualTo(item.appearanceId));
        }
        [Test] public void MovingPlatformIsPublishedWithThreePresetAppearances()
        {
            var definition=ItemService.WorkshopCatalog().Single(d=>d.kind==ItemKind.MovingPlatform);
            var list=definition.appearancePack.CategoryFor(ItemKind.MovingPlatform);
            Assert.That(list,Is.Not.Null);
            Assert.That(list.options.Count,Is.EqualTo(3));
            var item=ItemService.Create(definition,Vector3.zero);
            Assert.That(item.appearanceCategory,Is.EqualTo(list));
            Assert.That(item.SelectedAppearance,Is.EqualTo(list.options[0]));
        }
        [UnityTest] public IEnumerator InlineAppearanceSurvivesPlayAndUsesOriginalMovement()
        {
            ItemAppearance.ChooseOption(new[]{Player()},category,category.options[0].id);
            yield return new EnterPlayMode();
            var player=Player(); var chosen=player.SelectedAppearance;
            Assert.That(chosen,Is.Not.Null); Assert.That(ItemVisual.Resolve(player).sprite,Is.EqualTo(chosen.sprite));
            var move=player.GetComponent<Playground.Movement.Move>(); move.movementSource=()=>Vector2.right;
            float x=player.transform.position.x,until=Time.realtimeSinceStartup+.3f;
            while(Time.realtimeSinceStartup<until) yield return null;
            Assert.That(player.transform.position.x,Is.GreaterThan(x+.02f));
            yield return new ExitPlayMode();
            Assert.That(Player().appearanceCategory,Is.Not.Null);
        }
    }
}
