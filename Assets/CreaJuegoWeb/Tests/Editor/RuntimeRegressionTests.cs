using System.Collections;
using System.Linq;
using CreaJuego.Web;
using CreaJuego.PlaygroundBackend;
using Playground.Attributes;
using Playground.Movement;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEditor.TestTools;
using UnityEngine.UI;

namespace CreaJuego.Web.Tests
{
    public sealed class RuntimeRegressionTests
    {
        RuntimeAuthoringController OpenPrepared()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath,OpenSceneMode.Single);
            return Object.FindAnyObjectByType<RuntimeAuthoringController>();
        }
        RuntimeAuthoringController Open()
        {
            var controller=OpenPrepared();controller.LoadStarterLevel(false);return controller;
        }
        static void AssertOnlyEducationalVisual(GameItem item,string spritePrefix)
        {
            var expected=ItemVisual.Resolve(item);
            var enabled=item.GetComponentsInChildren<SpriteRenderer>(true).Where(renderer=>renderer.enabled).ToArray();
            Assert.That(enabled.Length,Is.EqualTo(1),item.name+" debe mostrar un único SpriteRenderer.");
            Assert.That(enabled[0],Is.SameAs(expected));
            Assert.That(expected.sprite,Is.Not.Null);
            Assert.That(expected.sprite.name,Does.StartWith(spritePrefix));
        }

        [UnityTest]
        public IEnumerator PreparedSceneContainsEditableLayoutAndInitialItems()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath,OpenSceneMode.Single);
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();var ui=Object.FindAnyObjectByType<RuntimeAuthoringUI>();var authoredCameraRect=controller.buildCamera.rect;
            yield return null;
            controller.LoadStarterLevel(false);ui.Refresh();
            Assert.That(ui.HasPreparedLayout,Is.True);Assert.That(controller.HasEditableScene,Is.True);Assert.That(controller.contentPack,Is.Not.Null);
            Assert.That(ui.BrandText,Does.Contain("Gamer").And.Contain("Crea tu propia aventura"));
            Assert.That(controller.GetComponents<RuntimeGameplayCamera>().Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),Is.EqualTo(0));
            Assert.That(Object.FindObjectsByType<RuntimeItemRow>(FindObjectsInactive.Include),Is.All.Matches<RuntimeItemRow>(row=>UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(row)).EndsWith("RuntimeItemRow.cs")),"Las filas de Mi juego deben apuntar a un script real para sobrevivir al build WebGL.");
            var overlayCanvases=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Where(value=>value.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();Assert.That(overlayCanvases.Length,Is.EqualTo(1));Assert.That(overlayCanvases[0].GetComponent<Image>(),Is.Null,"El Canvas de interfaz no debe tapar la cámara con un fondo opaco.");
            Assert.That(controller.buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>(true).Length,Is.GreaterThanOrEqualTo(35));
            var hierarchy=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Select(transform=>transform.name).ToArray();
            Assert.That(hierarchy,Does.Contain("Cabecera"));Assert.That(hierarchy,Does.Contain("Panel de elementos"));Assert.That(hierarchy,Does.Contain("Panel de propiedades"));Assert.That(hierarchy,Does.Contain("Herramientas"));Assert.That(hierarchy,Does.Contain("Estado para jugar"));
            Assert.That(ui.VisibleCatalogIconCount,Is.GreaterThanOrEqualTo(5));Assert.That(ui.VisibleSceneItemIconCount,Is.EqualTo(controller.Project.objects.Count));
            Assert.That(ui.FlowText,Does.Contain("Seleccionar"));Assert.That(ui.FlowText,Does.Contain("Personalizar"));Assert.That(ui.FlowText,Does.Contain("Jugar"));Assert.That(ui.ReadinessText,Does.Contain("listo"));
            Assert.That(controller.buildCamera.rect,Is.EqualTo(authoredCameraRect),"Entrar en ejecución no debe reemplazar el encuadre guardado en la escena.");
        }

        [UnityTest]
        public IEnumerator EditableSceneTransformsAreReadBeforePlay()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath,OpenSceneMode.Single);
            yield return null;
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();controller.LoadStarterLevel(false);var player=controller.buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>(true).Single(x=>x.GetComponent<GameItem>().definition.id=="jugador");
            player.transform.position+=new Vector3(1.25f,.5f);var data=controller.ReadEditableSceneProject().objects.Single(x=>x.definitionId=="jugador");
            Assert.That(data.position,Is.EqualTo(player.transform.position));
        }

        [Test]
        public void EditableSceneUsesGinoAndScarecrowWithAnimationClipsByDefault()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath,OpenSceneMode.Single);
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();var playerDefault=controller.contentPack.DefaultFor(ItemKind.Player);var enemyDefault=controller.contentPack.DefaultFor(ItemKind.Enemy);
            var playerAppearance=controller.contentPack.CategoryFor(ItemKind.Player).Find(playerDefault);var enemyAppearance=controller.contentPack.CategoryFor(ItemKind.Enemy).Find(enemyDefault);
            Assert.That(playerAppearance.displayName,Is.EqualTo("Gino"));Assert.That(playerAppearance.idleClip,Is.Not.Null);Assert.That(playerAppearance.moveClip,Is.Not.Null);Assert.That(playerAppearance.jumpClip,Is.Not.Null);Assert.That(playerAppearance.attackClip,Is.Not.Null);
            Assert.That(enemyAppearance.id,Is.EqualTo("platformer-kit-scarecrow"));Assert.That(enemyAppearance.idleClip,Is.Not.Null);Assert.That(enemyAppearance.moveClip,Is.Not.Null);Assert.That(enemyAppearance.jumpClip,Is.Not.Null);Assert.That(enemyAppearance.attackClip,Is.Not.Null);
            var items=controller.buildRoot.GetComponentsInChildren<GameItem>(true);Assert.That(items.Single(x=>x.definition.kind==ItemKind.Player).SelectedAppearance.displayName,Is.EqualTo("Gino"));Assert.That(items.Any(x=>x.definition.kind==ItemKind.Enemy&&x.SelectedAppearance.id=="platformer-kit-scarecrow"),Is.True);
            var playerDefinition=controller.Find("jugador");var enemyDefinition=controller.Find("enemigo");
            Assert.That(playerDefinition.icon.name,Does.StartWith("Gino-"));Assert.That(enemyDefinition.icon.name,Does.StartWith("Scarecrow-"));
            var playerVisual=playerDefinition.prefab.GetComponent<ItemVisual>();var enemyVisual=enemyDefinition.prefab.GetComponent<ItemVisual>();
            Assert.That(playerVisual.renderer.sprite.name,Does.StartWith("Gino-"));Assert.That(enemyVisual.renderer.sprite.name,Does.StartWith("Scarecrow-"));
            Assert.That(playerVisual.geometrySource.enabled,Is.False);Assert.That(enemyVisual.geometrySource.enabled,Is.False);
        }
        [Test]
        public void PreparedSceneKeepsDefaultCharactersSelectableAcrossPlayRoundTrip()
        {
            var c=Open();
            var playerData=c.Project.objects.Single(o=>o.definitionId=="jugador");
            var enemyData=c.Project.objects.Single(o=>o.definitionId=="enemigo"&&o.appearanceId==c.contentPack.DefaultFor(ItemKind.Enemy));
            Assert.That(playerData.appearanceId,Is.EqualTo(c.contentPack.DefaultFor(ItemKind.Player)));
            Assert.That(enemyData.appearanceId,Is.EqualTo(c.contentPack.DefaultFor(ItemKind.Enemy)));
            var buildPlayer=c.Selection.Select(playerData.instanceId);var buildEnemy=c.Selection.Select(enemyData.instanceId);
            Assert.That(buildPlayer,Is.Not.Null);Assert.That(buildEnemy,Is.Not.Null);
            Assert.That(buildPlayer.SelectedAppearance.displayName,Is.EqualTo("Gino"));AssertOnlyEducationalVisual(buildPlayer,"Gino-");
            Assert.That(buildEnemy.SelectedAppearance.id,Is.EqualTo("platformer-kit-scarecrow"));AssertOnlyEducationalVisual(buildEnemy,"Scarecrow-");
            var playerVisual=buildPlayer.GetComponent<ItemVisual>();playerVisual.geometrySource.enabled=true;playerVisual.Apply();AssertOnlyEducationalVisual(buildPlayer,"Gino-");
            Assert.That(RuntimeAuthoringHitTest.Pick(ItemVisual.Resolve(buildPlayer).bounds.center,c.buildRoot.GetComponentsInChildren<GameItem>()),Is.SameAs(buildPlayer));

            Assert.That(c.EnterPlay(),Is.Null);
            Assert.That(c.PlayPlayer.SelectedAppearance.displayName,Is.EqualTo("Gino"));AssertOnlyEducationalVisual(c.PlayPlayer,"Gino-");
            var playEnemy=c.PlayRoot.GetComponentsInChildren<GameItem>().First(i=>i.definition.kind==ItemKind.Enemy&&i.SelectedAppearance.id=="platformer-kit-scarecrow");
            Assert.That(playEnemy.SelectedAppearance.id,Is.EqualTo("platformer-kit-scarecrow"));AssertOnlyEducationalVisual(playEnemy,"Scarecrow-");

            c.ExitPlay();
            buildPlayer=c.Selection.Select(playerData.instanceId);buildEnemy=c.Selection.Select(enemyData.instanceId);
            Assert.That(buildPlayer,Is.Not.Null);Assert.That(buildEnemy,Is.Not.Null);
            Assert.That(buildPlayer.SelectedAppearance.displayName,Is.EqualTo("Gino"));AssertOnlyEducationalVisual(buildPlayer,"Gino-");
            Assert.That(buildEnemy.SelectedAppearance.id,Is.EqualTo("platformer-kit-scarecrow"));AssertOnlyEducationalVisual(buildEnemy,"Scarecrow-");
            playerVisual=buildPlayer.GetComponent<ItemVisual>();playerVisual.geometrySource.enabled=true;playerVisual.Apply();AssertOnlyEducationalVisual(buildPlayer,"Gino-");
            Assert.That(RuntimeAuthoringHitTest.Pick(ItemVisual.Resolve(buildEnemy).bounds.center,c.buildRoot.GetComponentsInChildren<GameItem>()),Is.SameAs(buildEnemy));
            c.Selection.Select(playerData.instanceId);c.DeleteSelected();
            var addedPlayer=c.Create("jugador",new Vector3(-5,-1.25f));
            Assert.That(addedPlayer.appearanceId,Is.EqualTo(c.contentPack.DefaultFor(ItemKind.Player)));
            AssertOnlyEducationalVisual(addedPlayer,"Gino-");
            var addedPlayerId=c.SelectedId();var addedEnemy=c.Create("enemigo",new Vector3(5,-1.15f));
            addedPlayer=c.Selection.Select(addedPlayerId);
            AssertOnlyEducationalVisual(addedPlayer,"Gino-");
            Assert.That(addedEnemy.appearanceId,Is.EqualTo(c.contentPack.DefaultFor(ItemKind.Enemy)));
            AssertOnlyEducationalVisual(addedEnemy,"Scarecrow-");
        }
        [Test]
        public void RuntimePackPrefersGinoAndScarecrowEvenWithStaleSerializedDefaults()
        {
            var c=OpenPrepared();var pack=Object.Instantiate(c.contentPack);
            pack.defaults=new[]{new RuntimeAppearanceDefault{kind=ItemKind.Player,appearanceId="tiny-dungeon-84"},new RuntimeAppearanceDefault{kind=ItemKind.Enemy,appearanceId="tiny-dungeon-120"}};
            Assert.That(pack.DefaultFor(ItemKind.Player),Is.EqualTo("5b84b5bdbf244cecb12b976bbf0aa6c7"));
            Assert.That(pack.DefaultFor(ItemKind.Enemy),Is.EqualTo("platformer-kit-scarecrow"));
            Object.DestroyImmediate(pack);
        }
        [Test]
        public void SchemaTwoProjectMigratesFormerDefaultsBeforePlay()
        {
            var c=Open();c.Project.schemaVersion=2;var player=c.Project.objects.Single(o=>o.definitionId=="jugador");var enemy=c.Project.objects.First(o=>o.definitionId=="enemigo");var enemyId=enemy.instanceId;
            player.appearanceId="tiny-dungeon-84";player.appearanceChosen=true;enemy.appearanceId="tiny-dungeon-120";enemy.appearanceChosen=true;
            var migrated=ProjectSerializer.FromJson(ProjectSerializer.ToJson(c.Project));
            Assert.That(migrated.schemaVersion,Is.EqualTo(4));player=migrated.objects.Single(o=>o.definitionId=="jugador");enemy=migrated.objects.Single(o=>o.instanceId==enemyId);
            Assert.That(player.appearanceChosen,Is.False);Assert.That(enemy.appearanceChosen,Is.False);
            c.Project.objects=migrated.objects;c.Rebuild();
            Assert.That(player.appearanceId,Is.EqualTo(c.contentPack.DefaultFor(ItemKind.Player)));
            Assert.That(enemy.appearanceId,Is.EqualTo(c.contentPack.DefaultFor(ItemKind.Enemy)));
            AssertOnlyEducationalVisual(c.buildRoot.GetComponentsInChildren<GameItem>().Single(i=>i.definition.kind==ItemKind.Player),"Gino-");
            AssertOnlyEducationalVisual(c.buildRoot.GetComponentsInChildren<GameItem>().First(i=>i.definition.kind==ItemKind.Enemy&&i.SelectedAppearance.id=="platformer-kit-scarecrow"),"Scarecrow-");
            Assert.That(c.EnterPlay(),Is.Null);
            AssertOnlyEducationalVisual(c.PlayPlayer,"Gino-");
            AssertOnlyEducationalVisual(c.PlayRoot.GetComponentsInChildren<GameItem>().First(i=>i.definition.kind==ItemKind.Enemy&&i.SelectedAppearance.id=="platformer-kit-scarecrow"),"Scarecrow-");
        }
        [Test]
        public void SelectionUsesStableInstanceIdAcrossRebuild()
        {
            var c=Open();var data=c.Project.objects.First(o=>o.definitionId=="premio");
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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath,OpenSceneMode.Single);
            yield return new EnterPlayMode();yield return null;
            var c=Object.FindAnyObjectByType<RuntimeAuthoringController>();var ui=Object.FindAnyObjectByType<RuntimeAuthoringUI>();var runtimeHeader=GameObject.Find("Cabecera").GetComponent<RectTransform>();
            Assert.That(runtimeHeader.anchorMin,Is.EqualTo(Vector2.zero));Assert.That(runtimeHeader.anchorMax,Is.EqualTo(Vector2.one));Assert.That(runtimeHeader.anchoredPosition,Is.EqualTo(new Vector2(0,334.54327f)).Using(Vector2ComparerWithEqualsOperator.Instance));Assert.That(runtimeHeader.sizeDelta,Is.EqualTo(new Vector2(0,-669.08655f)).Using(Vector2ComparerWithEqualsOperator.Instance),"Play Mode debe conservar el layout ajustado manualmente en WebAuthoringSpike.");
            var leftPanel=GameObject.Find("Panel de elementos").GetComponent<RectTransform>();var rightPanel=GameObject.Find("Panel de propiedades").GetComponent<RectTransform>();var canvasRect=leftPanel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            Assert.That(leftPanel.anchorMin.x,Is.EqualTo(0));Assert.That(leftPanel.anchorMax.x,Is.EqualTo(0));Assert.That(leftPanel.rect.width,Is.EqualTo(260).Within(.1f));
            Assert.That(rightPanel.anchorMin.x,Is.EqualTo(1));Assert.That(rightPanel.anchorMax.x,Is.EqualTo(1));Assert.That(rightPanel.rect.width,Is.EqualTo(280).Within(.1f));
            Assert.That(c.buildCamera.rect.x,Is.EqualTo(leftPanel.rect.width/canvasRect.rect.width).Within(.002f));Assert.That(c.buildCamera.rect.xMax,Is.EqualTo(1-rightPanel.rect.width/canvasRect.rect.width).Within(.002f));
            c.LoadStarterLevel(false);ui.Refresh();yield return null;
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
            Assert.That(GameObject.Find("Panel de elementos").activeInHierarchy,Is.True);Assert.That(GameObject.Find("Panel de propiedades").activeInHierarchy,Is.True);
            Assert.That(Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude).All(s=>s.scrollSensitivity>=20),Is.True);

            Assert.That(c.EnterPlay(),Is.Null);yield return null;Assert.That(c.Project.objects.Count,Is.EqualTo(before));Assert.That(ui.GameplayHudVisible,Is.True);Assert.That(ui.GameplayHudText,Does.Contain("VIDAS").And.Contain("PUNTOS").And.Contain("ENEMIGOS").And.Contain("OBJETIVO"));
            var metrics=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<IWorkshopSessionMetrics>().Single();Assert.That(((MonoBehaviour)metrics).GetComponentInChildren<Canvas>(true).enabled,Is.False,"El marcador técnico anterior debe quedar oculto.");
            c.ExitPlay();yield return null;Assert.That(ui.VisibleItemRowCount,Is.EqualTo(before));
            var player=c.Project.objects.Single(o=>o.definitionId=="jugador");Assert.That(ui.ClickItemRow(player.instanceId),Is.True);yield return null;
            Assert.That(ui.VisiblePropertiesText,Does.Contain("JUGADOR").And.Contain("Fuerza de salto"));
            yield return new ExitPlayMode();
        }

        [Test]
        public void BackgroundSelectionAndCreationShowImageControls()
        {
            var c=Open();var ui=c.GetComponent<RuntimeAuthoringUI>();ui.PrepareEditableLayout();c.LoadStarterLevel(false);ui.Refresh();
            var background=c.Project.objects.Single(o=>c.Find(o.definitionId)?.kind==ItemKind.Background);var item=c.Selection.Select(background.instanceId);ui.Refresh();
            Assert.That(ui.VisiblePropertiesText,Does.Contain("FONDO").And.Contain("APARIENCIA").And.Contain("Elegir imagen"));
            Assert.That(ui.VisibleSelectionIcon,Is.SameAs(item.SelectedAppearance.sprite));
            c.DeleteSelected();Assert.That(c.Project.objects.Any(o=>c.Find(o.definitionId)?.kind==ItemKind.Background),Is.False);
            var recreated=c.Create("fondo",Vector3.zero);Assert.That(recreated,Is.Not.Null);ui.Refresh();
            Assert.That(ui.VisiblePropertiesText,Does.Contain("FONDO").And.Contain("Elegir imagen"));Assert.That(ui.VisibleSelectionIcon,Is.Not.Null);
        }

        [Test]
        public void MovingPlatformRowUsesItsSelectedAppearance()
        {
            var c=Open();var moving=c.Project.objects.First(o=>c.Find(o.definitionId)?.kind==ItemKind.MovingPlatform);var definition=c.Find(moving.definitionId);var appearances=c.contentPack.CategoryFor(ItemKind.MovingPlatform);
            Assert.That(appearances,Is.Not.Null);var chosen=appearances.options.First(option=>option.Preview!=null&&option.id!=moving.appearanceId);moving.appearanceId=chosen.id;
            var marker=c.buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>().Single(value=>value.instanceId==moving.instanceId);var item=marker.GetComponent<GameItem>();
            Assert.That(RuntimeAuthoringUI.ResolvePreview(moving,definition,c.contentPack,item),Is.SameAs(chosen.Preview));
        }

        [Test]
        public void PreparedPackIncludesUniqueBackgroundAndMovingPlatform()
        {
            var c=Open();var backgroundDefinition=c.contentPack.definitions.Single(d=>d.kind==ItemKind.Background);var movingDefinition=c.contentPack.definitions.Single(d=>d.kind==ItemKind.MovingPlatform);
            Assert.That(backgroundDefinition.allowMultiple,Is.False);Assert.That(backgroundDefinition.icon,Is.Not.Null);Assert.That(movingDefinition.prefab,Is.Not.Null);
            Assert.That(c.Project.objects.Count(o=>o.definitionId==backgroundDefinition.id),Is.EqualTo(1));Assert.That(c.Project.objects.Count(o=>o.definitionId==movingDefinition.id),Is.EqualTo(1));
            var background=c.buildRoot.GetComponentsInChildren<GameItem>().Single(i=>i.definition.kind==ItemKind.Background);var canvasBackground=background.GetComponent<RuntimeCanvasBackground>();
            Assert.That(background.GetComponent<SpriteRenderer>(),Is.Null);Assert.That(canvasBackground,Is.Not.Null);Assert.That(canvasBackground.TargetCamera,Is.SameAs(c.buildCamera));Assert.That(canvasBackground.Image,Is.Not.Null);Assert.That(canvasBackground.Image.sprite,Is.SameAs(background.SelectedAppearance.sprite));
            var imageRect=canvasBackground.Image.rectTransform;Assert.That(imageRect.anchorMin,Is.EqualTo(Vector2.zero));Assert.That(imageRect.anchorMax,Is.EqualTo(Vector2.one));Assert.That(imageRect.offsetMin,Is.EqualTo(Vector2.zero));Assert.That(imageRect.offsetMax,Is.EqualTo(Vector2.zero));
            var canvas=canvasBackground.Image.GetComponentInParent<Canvas>();Assert.That(canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceCamera));Assert.That(canvas.worldCamera,Is.SameAs(c.buildCamera));
            int before=c.Project.objects.Count;Assert.That(c.Create(backgroundDefinition.id,Vector3.zero),Is.Null);Assert.That(c.Project.objects.Count,Is.EqualTo(before));
        }

        [Test]
        public void WorkshopCatalogHasStructuresAndThreePreparedChoices()
        {
            var c=Open();
            foreach(var kind in new[]{ItemKind.Player,ItemKind.Platform,ItemKind.MovingPlatform,ItemKind.Prize,ItemKind.Hazard,ItemKind.Enemy,ItemKind.Goal,ItemKind.Background})
                Assert.That(c.contentPack.OptionsFor(kind).Length,Is.GreaterThanOrEqualTo(3),kind+" debe ofrecer al menos tres apariencias.");
            Assert.That(c.contentPack.Find("muro"),Is.Not.Null);Assert.That(c.contentPack.Find("rampa"),Is.Not.Null);Assert.That(c.contentPack.Find("escalera"),Is.Null);
            Assert.That(c.contentPack.OptionsFor(ItemKind.Platform,null,"plataforma").Any(o=>o.id=="platformer-stone-tile"));Assert.That(c.contentPack.OptionsFor(ItemKind.Platform,null,"muro").All(o=>o.definitionIds.Contains("muro")));Assert.That(c.contentPack.OptionsFor(ItemKind.Platform,null,"muro").Any(o=>o.id=="creajuego-wall-light"));Assert.That(c.contentPack.OptionsFor(ItemKind.Platform,null,"rampa").All(o=>o.definitionIds.Contains("rampa")));
            Assert.That(c.contentPack.OptionsFor(ItemKind.Goal).Any(o=>o.id=="platformer-finish-chest"||o.id=="web-meta-cofre"),Is.False);
            var platformData=c.Project.objects.First(o=>o.definitionId=="plataforma");c.Selection.Select(platformData.instanceId);c.SetAppearance("platformer-stone-tile");var platformItem=c.Selection.SelectedItem;
            Assert.That(ItemVisual.Resolve(platformItem).bounds.center.x,Is.EqualTo(platformItem.GetComponent<Collider2D>().bounds.center.x).Within(.01f));
        }

        [Test]
        public void ScaleAndStructureDirectionPersistInEducationalData()
        {
            var c=Open();var moving=c.Project.objects.Single(o=>o.definitionId=="movil");c.Selection.Select(moving.instanceId);var visual=ItemVisual.Resolve(c.Selection.SelectedItem);float ratio=Mathf.Abs(visual.transform.localScale.x/visual.transform.localScale.y),beforeX=Mathf.Abs(visual.transform.localScale.x);c.SetFloat("visualScale",2.25f);
            Assert.That(moving.visualScale,Is.EqualTo(2.25f));Assert.That(Mathf.Abs(visual.transform.localScale.x/visual.transform.localScale.y),Is.EqualTo(ratio).Within(.001f));Assert.That(Mathf.Abs(visual.transform.localScale.x),Is.EqualTo(beforeX*2.25f).Within(.01f));
            var ramp=c.Create("rampa",Vector3.zero);Assert.That(ramp,Is.Not.Null);var rampData=c.SelectedData();c.SetStructureDirection(false);
            Assert.That(rampData.rotationZ,Is.EqualTo(-18));Assert.That(c.Selection.SelectedItem.transform.eulerAngles.z,Is.EqualTo(342).Within(.01f));
            var roundTrip=ProjectSerializer.FromJson(ProjectSerializer.ToJson(c.Project)).objects.Single(o=>o.instanceId==rampData.instanceId);Assert.That(roundTrip.rotationZ,Is.EqualTo(-18));
            Assert.That(RuntimeLevelBounds.For(RuntimeLevelSize.ExtraLarge).right,Is.EqualTo(240));
        }

        [UnityTest]
        public IEnumerator MovingPlatformAppliesItsActualSprite()
        {
            OpenPrepared();
            yield return new EnterPlayMode();
            var c=Object.FindAnyObjectByType<RuntimeAuthoringController>();
            Assert.That(c!=null,Is.True,"La escena debe conservar su controlador activo.");
            c.LoadStarterLevel(false);

            var moving=c.Project.objects.Single(o=>o.definitionId=="movil");
            c.Selection.Select(moving.instanceId);
            var movingOption=c.contentPack.OptionsFor(ItemKind.MovingPlatform)[1];
            c.SetAppearance(movingOption.id);
            yield return null;
            Assert.That(c.Selection.SelectedItem,Is.Not.Null);
            Assert.That(c.Selection.SelectedItem.GetComponent<ItemVisual>(),Is.Not.Null);
            Assert.That(ItemVisual.Resolve(c.Selection.SelectedItem).sprite,Is.SameAs(movingOption.Preview));

        }

        [UnityTest]
        public IEnumerator GameplayHudRemovesAHeartWhenPlayablePlayerTakesDamage()
        {
            OpenPrepared();
            yield return new EnterPlayMode();
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();
            var ui=Object.FindAnyObjectByType<RuntimeAuthoringUI>();
            controller.LoadStarterLevel(false);
            Assert.That(controller.EnterPlay(),Is.Null);
            yield return null;

            var health=controller.PlayPlayer.GetComponent<HealthSystemAttribute>();
            var metrics=Object.FindObjectsByType<MonoBehaviour>().OfType<IWorkshopSessionMetrics>().Single();
            int initial=health.health;
            Assert.That(metrics.CurrentHealth,Is.EqualTo(initial));
            Assert.That(ui.GameplayHeartCount,Is.EqualTo(initial));

            health.ModifyHealth(-1);
            yield return null;
            Assert.That(metrics.CurrentHealth,Is.EqualTo(initial-1),"El marcador debe leer al jugador temporal activo, no al jugador editable oculto.");
            Assert.That(ui.GameplayHeartCount,Is.EqualTo(initial-1),"El HUD debe retirar un corazón después de recibir daño.");

            controller.ExitPlay();
            yield return new ExitPlayMode();
        }

        [Test]
        public void WitchAppearanceReplacesEveryVisibleGinoSprite()
        {
            var c=Open();
            var player=c.Project.objects.Single(o=>o.definitionId=="jugador");
            Assert.That(c.Selection.Select(player.instanceId),Is.Not.Null);
            var defaultPlayerSprite=ItemVisual.Resolve(c.Selection.SelectedItem).sprite;
            c.SetAppearance("tiny-dungeon-84");
            var selectedPlayer=c.Selection.SelectedItem;
            var selectedRenderer=ItemVisual.Resolve(selectedPlayer);
            Assert.That(selectedRenderer,Is.Not.Null);
            Assert.That(selectedRenderer.sprite,Is.Not.Null);
            Assert.That(selectedRenderer.sprite,Is.Not.SameAs(defaultPlayerSprite),"La apariencia Brujita no puede conservar el sprite de Gino.");
            Assert.That(selectedPlayer.appearanceId,Is.EqualTo("tiny-dungeon-84"));
            Assert.That(selectedPlayer.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.enabled).All(r=>r.sprite==selectedRenderer.sprite),Is.True,"Ningún renderer visible debe conservar sprites de Gino.");
        }

        [Test]
        public void PlayerClimbsRampAndRemainsSupported()
        {
            var c=Open();
            var player=c.Project.objects.Single(o=>o.definitionId=="jugador");
            player.position=new Vector3(-.8f,.5f);
            player.speed=3;
            c.Project.objects.RemoveAll(o=>c.Find(o.definitionId)?.kind==ItemKind.Platform);
            c.Rebuild();
            Assert.That(c.Create("rampa",Vector3.zero),Is.Not.Null);
            var ramp=c.SelectedData();
            ramp.position=Vector3.zero;
            ramp.platformWidth=4;
            ramp.rotationZ=18;
            c.Rebuild();
            Assert.That(c.EnterPlay(),Is.Null);

            var runtimePlayer=c.PlayPlayer;
            var movement=runtimePlayer.GetComponent<Move>();
            var body=runtimePlayer.GetComponent<Rigidbody2D>();
            var gate=runtimePlayer.GetComponent<GroundedJumpGate>();
            var slopeAssist=runtimePlayer.GetComponent<SlopeMovementAssist>();
            Assert.That(movement,Is.Not.Null);
            Assert.That(body,Is.Not.Null);
            Assert.That(gate,Is.Not.Null);
            Assert.That(slopeAssist,Is.Not.Null);
            movement.rigidbody2D=body;
            gate.Initialize();
            movement.enabled=true;
            movement.movementSource=()=>Vector2.right;
            body.simulated=true;
            body.WakeUp();
            var start=body.position;
            bool supported=false;
            var end=start;
            var previousMode=Physics2D.simulationMode;
            try
            {
                Physics2D.simulationMode=SimulationMode2D.Script;
                for(int i=0;i<30;i++)
                {
                    slopeAssist.Apply();
                    Physics2D.Simulate(Time.fixedDeltaTime);
                    gate.EvaluateSupport();
                    supported|=gate.Supported;
                }
                end=body.position;
            }
            finally
            {
                Physics2D.simulationMode=previousMode;
                c.ExitPlay();
            }
            Assert.That(supported,Is.True,"El jugador debe reconocer la pendiente como suelo.");
            Assert.That(end.x,Is.GreaterThan(start.x+.5f));
            Assert.That(end.y,Is.GreaterThan(start.y+.15f));
        }
        [Test]
        public void RampCanBeCrossedFromBelowAndSupportsFromAbove()
        {
            var surface=new GameObject("Rampa unidireccional",typeof(BoxCollider2D),typeof(PlatformEffector2D),typeof(OneWayRampSurface));
            var actor=new GameObject("Participante",typeof(BoxCollider2D),typeof(Rigidbody2D));
            surface.transform.position=new Vector3(1000,0);
            surface.GetComponent<BoxCollider2D>().size=new Vector2(4,.4f);
            surface.GetComponent<OneWayRampSurface>().Configure();
            var body=actor.GetComponent<Rigidbody2D>();body.gravityScale=0;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var previousMode=Physics2D.simulationMode;
            try
            {
                Physics2D.simulationMode=SimulationMode2D.Script;
                actor.transform.position=new Vector3(1000,-1);body.position=actor.transform.position;body.linearVelocity=Vector2.up*5;Physics2D.SyncTransforms();
                for(int i=0;i<30;i++)Physics2D.Simulate(Time.fixedDeltaTime);
                Assert.That(body.position.y,Is.GreaterThan(.7f),"La rampa debe poder atravesarse desde abajo.");

                body.position=new Vector2(1000,1);body.linearVelocity=Vector2.down*5;Physics2D.SyncTransforms();
                for(int i=0;i<30;i++)Physics2D.Simulate(Time.fixedDeltaTime);
                Assert.That(body.position.y,Is.EqualTo(.7f).Within(.08f),"La cara superior debe sostener al personaje.");
            }
            finally
            {
                Physics2D.simulationMode=previousMode;
                Object.DestroyImmediate(actor);Object.DestroyImmediate(surface);
            }
        }
        [Test]
        public void MovementGuideMatchesConfiguredMovementDistance()
        {
            var c=Open();var movingData=c.Project.objects.Single(o=>c.Find(o.definitionId)?.kind==ItemKind.MovingPlatform);
            var moving=c.buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>().Single(marker=>marker.instanceId==movingData.instanceId).GetComponent<GameItem>();
            Assert.That(RuntimeMovementGuide.TryGetRoute(moving,out var start,out var end),Is.True);Assert.That(end.x-start.x,Is.EqualTo(movingData.distance).Within(.01f));
            moving.distance=7;Assert.That(RuntimeMovementGuide.TryGetRoute(moving,out start,out end),Is.True);Assert.That(end.x-start.x,Is.EqualTo(7).Within(.01f));
            var platform=c.buildRoot.GetComponentsInChildren<GameItem>().First(item=>item.definition.kind==ItemKind.Platform);Assert.That(RuntimeMovementGuide.TryGetRoute(platform,out _,out _),Is.False);
        }
    }
}
