using System;
using System.Collections;
using System.Linq;
using CreaJuego.Web;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.TestTools;

namespace CreaJuego.Web.Tests
{
    public sealed class RuntimeParityTests
    {
        RuntimeAuthoringController Open()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);var controller=UnityEngine.Object.FindAnyObjectByType<RuntimeAuthoringController>();controller.NewProject(false);return controller;
        }
        static RuntimeAuthoredItem Select(RuntimeAuthoringController controller,string id)
        {
            var data=controller.Project.objects.First(o=>o.definitionId==id);var marker=UnityEngine.Object.FindObjectsByType<RuntimeAuthoredItem>(FindObjectsInactive.Exclude).First(m=>m.instanceId==data.instanceId);controller.Selection.Select(marker.gameObject);return marker;
        }
        [Test] public void SafePrefabCreatesDataWithExplicitDefinition(){var c=Open();var def=c.Find("jugador");Assert.That(def.prefab.GetComponent<GameItem>().definition,Is.Null);var data=RuntimeItemData.From(def.prefab.GetComponent<GameItem>(),def.id);Assert.That(data.definitionId,Is.EqualTo("jugador"));}
        [Test] public void SchemaV2RoundTripPreservesWidthSnapBoundsAndAppearance()
        {
            var data=new CreaJuegoProjectData{alignAutomatically=false,levelSize=RuntimeLevelSize.Large,bounds=RuntimeLevelBounds.For(RuntimeLevelSize.Large)};data.objects.Add(new RuntimeItemData{definitionId="plataforma",platformWidth=8.25f,appearanceId="stone"});var restored=ProjectSerializer.FromJson(ProjectSerializer.ToJson(data));
            Assert.That(restored.schemaVersion,Is.EqualTo(2));Assert.That(restored.alignAutomatically,Is.False);Assert.That(restored.bounds.right,Is.EqualTo(50));Assert.That(restored.objects[0].platformWidth,Is.EqualTo(8.25f));Assert.That(restored.objects[0].appearanceId,Is.EqualTo("stone"));
        }
        [Test] public void SpikeV1JsonMigratesWithoutSilentLoss(){var restored=ProjectSerializer.FromJson("{\"version\":1,\"projectName\":\"Anterior\",\"objects\":[{\"definitionId\":\"plataforma\"}]}");Assert.That(restored.schemaVersion,Is.EqualTo(2));Assert.That(restored.alignAutomatically,Is.True);Assert.That(restored.bounds.IsValid);Assert.That(restored.objects[0].platformWidth,Is.EqualTo(3));}
        [Test] public void PlatformWidthChangesVisualColliderPersistsAndUndoRedo()
        {
            var c=Open();var marker=Select(c,"plataforma");var data=c.SelectedData();float original=data.platformWidth;float height=marker.GetComponent<BoxCollider2D>().size.y;c.ResizeSelected(8,data.position.x,true);
            Assert.That(marker.GetComponent<BoxCollider2D>().size.x,Is.EqualTo(8).Within(.01f));Assert.That(marker.GetComponent<BoxCollider2D>().size.y,Is.EqualTo(height).Within(.001f));Assert.That(ItemVisual.Resolve(marker.GetComponent<GameItem>()).bounds.size.x,Is.EqualTo(8).Within(.05f));Assert.That(ProjectSerializer.FromJson(ProjectSerializer.ToJson(c.Project)).objects.First(o=>o.instanceId==data.instanceId).platformWidth,Is.EqualTo(8));
            c.Undo();Assert.That(c.Project.objects.First(o=>o.instanceId==data.instanceId).platformWidth,Is.EqualTo(original).Within(.01f));c.Redo();Assert.That(c.Project.objects.First(o=>o.instanceId==data.instanceId).platformWidth,Is.EqualTo(8).Within(.01f));
        }
        [Test] public void SnapCanBeEnabledAndDisabled(){var c=Open();Select(c,"decoracion");c.SetSnap(true);c.MoveSelected(new Vector3(1.13f,2.19f),true);Assert.That(c.SelectedData().position,Is.EqualTo(new Vector3(1.25f,2.25f)));c.SetSnap(false);c.MoveSelected(new Vector3(1.13f,2.19f),true);Assert.That(c.SelectedData().position.x,Is.EqualTo(1.13f).Within(.001f));}
        [Test] public void PreflightChecksSupportRecoveryBoundsAndCamera()
        {
            var c=Open();Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Is.Null);var player=c.Project.objects.Single(o=>o.definitionId=="jugador");player.position.y+=5;Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Does.Contain("aire"));player.position.y-=5;c.Project.bounds.right=c.Project.bounds.left;Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Does.Contain("tamaño"));c.Project.bounds=RuntimeLevelBounds.For(RuntimeLevelSize.Medium);Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext{cameraAvailable=false}),Does.Contain("vista"));
        }
        [Test] public void RuntimePackFiltersTypeAndContainsNoExternalPrefabs(){var c=Open();var players=c.contentPack.OptionsFor(ItemKind.Player);var platforms=c.contentPack.OptionsFor(ItemKind.Platform);Assert.That(players.Length,Is.GreaterThanOrEqualTo(3));Assert.That(platforms.Length,Is.GreaterThanOrEqualTo(3));Assert.That(players.All(o=>o.prefab==null&&o.Preview!=null));Assert.That(c.contentPack.OptionsFor(ItemKind.Decoration,"roca").All(o=>o.displayName.IndexOf("roca",StringComparison.OrdinalIgnoreCase)>=0));}
        [Test] public void AppearancePersistsAndSupportsUndoRedo()
        {
            var c=Open();Select(c,"jugador");var data=c.SelectedData();var original=data.appearanceId;var option=c.contentPack.OptionsFor(ItemKind.Player).First(o=>o.id!=original);c.SetAppearance(option.id);Assert.That(c.Selection.SelectedItem.SelectedAppearance.id,Is.EqualTo(option.id));Assert.That(ProjectSerializer.FromJson(ProjectSerializer.ToJson(c.Project)).objects.First(o=>o.instanceId==data.instanceId).appearanceId,Is.EqualTo(option.id));c.Undo();Assert.That(c.Project.objects.First(o=>o.instanceId==data.instanceId).appearanceId??"",Is.EqualTo(original??""));c.Redo();Assert.That(c.Project.objects.First(o=>o.instanceId==data.instanceId).appearanceId,Is.EqualTo(option.id));
        }
        [Test] public void ImageProtectionAcceptsSmallPngAndRejectsOversizedDimensions()
        {
            const string onePixel="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl2nWQAAAAASUVORK5CYII=";Assert.That(RuntimeImageImport.ValidateDataUrl(onePixel,out _),Is.True);
            var bytes=new byte[24];bytes[0]=137;bytes[1]=80;bytes[2]=78;bytes[3]=71;bytes[16]=0;bytes[17]=0;bytes[18]=16;bytes[19]=0;bytes[20]=0;bytes[21]=0;bytes[22]=0;bytes[23]=1;var huge="data:image/png;base64,"+Convert.ToBase64String(bytes);Assert.That(RuntimeImageImport.ValidateDataUrl(huge,out var error),Is.False);Assert.That(error,Does.Contain("demasiado grande"));
        }
        [Test] public void FallRecoveryClearsVelocityAndAngularVelocity()
        {
            var go=new GameObject("Recovery",typeof(Rigidbody2D),typeof(PlayerFallRecovery));var recovery=go.GetComponent<PlayerFallRecovery>();var body=go.GetComponent<Rigidbody2D>();recovery.ConfigureWorldLimit(new Vector3(2,3),-5);body.position=new Vector2(8,-8);body.linearVelocity=new Vector2(4,-8);body.angularVelocity=4;recovery.ReturnToStart();Assert.That(body.position,Is.EqualTo(new Vector2(2,3)));Assert.That(body.linearVelocity,Is.EqualTo(Vector2.zero));Assert.That(body.angularVelocity,Is.EqualTo(0));UnityEngine.Object.DestroyImmediate(go);
        }        [Test] public void CameraPlayAndBuildRestoreAuthoringAndNewProperties()
        {
            var c=Open();Select(c,"plataforma");var id=c.SelectedData().instanceId;c.ResizeSelected(7,c.SelectedData().position.x,true);Select(c,"jugador");var option=c.contentPack.OptionsFor(ItemKind.Player).Last();c.SetAppearance(option.id);Assert.That(c.EnterPlay(),Is.Null);Assert.That(c.buildCamera.enabled,Is.False);Assert.That(c.gameCamera.enabled,Is.True);Assert.That(c.GameplayCamera.ActiveCamera.Follow,Is.EqualTo(c.PlayPlayer.transform));c.ExitPlay();Assert.That(c.buildCamera.enabled,Is.True);Assert.That(c.gameCamera.enabled,Is.False);Assert.That(c.Project.objects.First(o=>o.instanceId==id).platformWidth,Is.EqualTo(7));Assert.That(c.Project.objects.Single(o=>o.definitionId=="jugador").appearanceId,Is.EqualTo(option.id));
        }
        [Test] public void LargeStressScenarioContainsOneHundredObjects(){var c=Open();c.CreateLargeStressProject();Assert.That(c.Project.objects.Count,Is.EqualTo(100));Assert.That(c.Project.objects.Count(o=>o.definitionId=="plataforma"),Is.EqualTo(50));}
        [UnityTest] public IEnumerator HandlesHideInPlayAndFallRecoveryRestoresAuthoredStart()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);yield return new EnterPlayMode();var c=UnityEngine.Object.FindAnyObjectByType<RuntimeAuthoringController>();Select(c,"plataforma");yield return null;var handles=UnityEngine.Object.FindObjectsByType<RuntimeResizeHandle>(FindObjectsInactive.Include);Assert.That(handles.Length,Is.EqualTo(2));Assert.That(handles.All(h=>h.gameObject.activeInHierarchy));var authored=c.Project.objects.Single(o=>o.definitionId=="jugador").position;Assert.That(c.EnterPlay(),Is.Null);yield return null;Assert.That(UnityEngine.Object.FindObjectsByType<RuntimeResizeHandle>(FindObjectsInactive.Include),Is.Empty);var player=c.PlayPlayer;var body=player.GetComponent<Rigidbody2D>();body.position=new Vector2(authored.x,c.Project.bounds.bottom-2);body.linearVelocity=new Vector2(4,-8);body.angularVelocity=4;player.GetComponent<PlayerFallRecovery>().SendMessage("FixedUpdate");Assert.That(player.transform.position.x,Is.EqualTo(authored.x).Within(.1f));Assert.That(player.transform.position.y,Is.EqualTo(authored.y).Within(.1f));Assert.That(c.Project.objects.Single(o=>o.definitionId=="jugador").position,Is.EqualTo(authored));c.ExitPlay();yield return new ExitPlayMode();
        }
    }
}