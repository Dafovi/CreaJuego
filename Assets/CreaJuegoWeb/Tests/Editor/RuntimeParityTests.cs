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
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);var controller=UnityEngine.Object.FindAnyObjectByType<RuntimeAuthoringController>();controller.LoadStarterLevel(false);return controller;
        }
        static RuntimeAuthoredItem Select(RuntimeAuthoringController controller,string id)
        {
            var data=controller.Project.objects.First(o=>o.definitionId==id);var marker=UnityEngine.Object.FindObjectsByType<RuntimeAuthoredItem>(FindObjectsInactive.Exclude).First(m=>m.instanceId==data.instanceId);controller.Selection.Select(marker.gameObject);return marker;
        }
        static string Image(Color color)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.SetPixels(new[]{color,color,color,color});texture.Apply();var data="data:image/png;base64,"+Convert.ToBase64String(texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);return data;
        }
        [Test] public void SafePrefabCreatesDataWithExplicitDefinition(){var c=Open();var def=c.Find("jugador");Assert.That(def.prefab.GetComponent<GameItem>().definition,Is.Null);var data=RuntimeItemData.From(def.prefab.GetComponent<GameItem>(),def.id);Assert.That(data.definitionId,Is.EqualTo("jugador"));}
        [Test] public void CurrentSchemaRoundTripPreservesWidthSnapBoundsAndAppearance()
        {
            var data=new CreaJuegoProjectData{alignAutomatically=false,levelSize=RuntimeLevelSize.Large,bounds=RuntimeLevelBounds.For(RuntimeLevelSize.Large)};data.objects.Add(new RuntimeItemData{definitionId="plataforma",platformWidth=8.25f,appearanceId="stone"});var restored=ProjectSerializer.FromJson(ProjectSerializer.ToJson(data));
            Assert.That(restored.schemaVersion,Is.EqualTo(9));Assert.That(restored.alignAutomatically,Is.False);Assert.That(restored.bounds.right,Is.EqualTo(100));Assert.That(restored.objects[0].platformWidth,Is.EqualTo(8.25f));Assert.That(restored.objects[0].appearanceId,Is.EqualTo("stone"));
        }
        [Test] public void SpikeV1JsonMigratesWithoutSilentLoss(){var restored=ProjectSerializer.FromJson("{\"version\":1,\"projectName\":\"Anterior\",\"objects\":[{\"definitionId\":\"plataforma\"}]}");Assert.That(restored.schemaVersion,Is.EqualTo(9));Assert.That(restored.gameTypeId,Is.EqualTo("platformer"));Assert.That(restored.alignAutomatically,Is.True);Assert.That(restored.bounds.IsValid);Assert.That(restored.objects[0].platformWidth,Is.EqualTo(3));}
        [Test] public void PlatformWidthChangesVisualColliderPersistsAndUndoRedo()
        {
            var c=Open();var marker=Select(c,"plataforma");var data=c.SelectedData();float original=data.platformWidth;float height=marker.GetComponent<BoxCollider2D>().size.y;c.ResizeSelected(8,data.position.x,true);
            Assert.That(marker.GetComponent<BoxCollider2D>().size.x,Is.EqualTo(8).Within(.01f));Assert.That(marker.GetComponent<BoxCollider2D>().size.y,Is.EqualTo(height).Within(.001f));Assert.That(ItemVisual.Resolve(marker.GetComponent<GameItem>()).bounds.size.x,Is.EqualTo(8).Within(.05f));Assert.That(ProjectSerializer.FromJson(ProjectSerializer.ToJson(c.Project)).objects.First(o=>o.instanceId==data.instanceId).platformWidth,Is.EqualTo(8));
            c.Undo();Assert.That(c.Project.objects.First(o=>o.instanceId==data.instanceId).platformWidth,Is.EqualTo(original).Within(.01f));c.Redo();Assert.That(c.Project.objects.First(o=>o.instanceId==data.instanceId).platformWidth,Is.EqualTo(8).Within(.01f));
        }
        [Test] public void AnyAuthoredObjectCanResizeOnBothAxesAndPersist()
        {
            var c=Open();var marker=Select(c,"premio");var data=c.SelectedData();var before=c.SelectedWorldSize;var center=marker.GetComponent<Collider2D>().bounds.center;
            c.ResizeSelected(new Vector2(before.x*1.5f,before.y*2f),new Vector2(center.x,center.y),true);
            Assert.That(c.SelectedWorldSize.x,Is.EqualTo(RuntimeSnap.Width(before.x*1.5f,true)).Within(.05f));Assert.That(c.SelectedWorldSize.y,Is.EqualTo(RuntimeSnap.Width(before.y*2f,true)).Within(.05f));
            var restored=ProjectSerializer.FromJson(ProjectSerializer.ToJson(c.Project)).objects.Single(o=>o.instanceId==data.instanceId);Assert.That(restored.scale.x,Is.EqualTo(data.scale.x).Within(.001f));Assert.That(restored.scale.y,Is.EqualTo(data.scale.y).Within(.001f));
            c.Undo();Assert.That(c.SelectedWorldSize.x,Is.EqualTo(before.x).Within(.05f));c.Redo();Assert.That(c.SelectedWorldSize.y,Is.EqualTo(RuntimeSnap.Width(before.y*2f,true)).Within(.05f));
        }
        [Test] public void SnapCanBeEnabledAndDisabled(){var c=Open();Select(c,"decoracion");c.SetSnap(true);c.MoveSelected(new Vector3(1.13f,2.19f),true);Assert.That(c.SelectedData().position,Is.EqualTo(new Vector3(1.25f,2.25f)));c.SetSnap(false);c.MoveSelected(new Vector3(1.13f,2.19f),true);Assert.That(c.SelectedData().position.x,Is.EqualTo(1.13f).Within(.001f));}
        [Test] public void PreflightChecksSupportRecoveryBoundsAndCamera()
        {
            var c=Open();Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Is.Null);var player=c.Project.objects.Single(o=>o.definitionId=="jugador");player.position.y+=5;Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Does.Contain("aire"));player.position.y-=5;c.Project.bounds.right=c.Project.bounds.left;Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Does.Contain("tamaño"));c.Project.bounds=RuntimeLevelBounds.For(RuntimeLevelSize.Medium);Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext{cameraAvailable=false}),Does.Contain("vista"));
        }
        [Test] public void CatchAndDodgeUsesItsOwnCatalogPreflightAndSession()
        {
            var c=Open();Assert.That(c.NewProjectFor("catch-and-dodge",false),Is.True);Assert.That(c.Project.gameTypeId,Is.EqualTo("catch-and-dodge"));Assert.That(c.Project.targetScore,Is.EqualTo(10));
            Assert.That(c.DefinitionAvailable(c.Find("jugador")));Assert.That(c.DefinitionAvailable(c.Find("premio")));Assert.That(c.DefinitionAvailable(c.Find("peligro")));Assert.That(c.DefinitionAvailable(c.Find("plataforma")),Is.False);
            Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Does.Contain("Jugador"));var player=c.Create("jugador",Vector3.zero);Assert.That(player,Is.Not.Null);Assert.That(player.transform.position.y,Is.EqualTo(c.Project.bounds.bottom+1).Within(.01f));Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Does.Contain("Premio"));
            c.Create("premio",new Vector3(-2,0));c.Create("peligro",new Vector3(2,0));Assert.That(RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext()),Is.Null);Assert.That(c.EnterPlay(),Is.Null);
            var session=c.PlayRoot.GetComponentInChildren<RuntimeCatchSession>();Assert.That(session,Is.Not.Null);Assert.That(c.PlayRoot.GetComponentsInChildren<RuntimeCatchPlayer>().Length,Is.EqualTo(1));Assert.That(c.PlayRoot.GetComponentsInChildren<RuntimeFallingObject>().Length,Is.EqualTo(2));Assert.That(session.CurrentHealth,Is.EqualTo(c.Project.objects.Single(o=>o.definitionId=="jugador").health));Assert.That(session.Objective,Does.Contain("10"));
            Assert.That(session.Collect(4));Assert.That(session.Score,Is.EqualTo(4));Assert.That(session.Hit(1));Assert.That(session.CurrentHealth,Is.EqualTo(2));Assert.That(session.Collect(6));Assert.That(session.State,Is.EqualTo(GameSessionState.Won));c.ExitPlay();
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
        [Test] public void ImportedImageEntersLibraryAppliesToSelectionAndPersists()
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.SetPixels(new[]{Color.red,Color.green,Color.blue,Color.white});texture.Apply();var image="data:image/png;base64,"+Convert.ToBase64String(texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            var c=Open();Select(c,"jugador");var selectedId=c.SelectedData().instanceId;c.PickImage();c.ReceiveImageImport(JsonUtility.ToJson(new RuntimeImageImportPayload{name="Héroe dibujado",dataUrl=image}));c.ReceiveImageBatchComplete("1");
            Assert.That(c.Project.mediaAssets.Count,Is.EqualTo(1));Assert.That(c.Project.mediaAssets[0].displayName,Is.EqualTo("Héroe dibujado"));Assert.That(c.Project.objects.Single(item=>item.instanceId==selectedId).mediaAssetId,Is.EqualTo(c.Project.mediaAssets[0].id));Assert.That(c.Selection.SelectedItem.customSprite,Is.Not.Null);
            var restored=ProjectSerializer.FromJson(ProjectSerializer.ToJson(c.Project));Assert.That(restored.mediaAssets.Count,Is.EqualTo(1));Assert.That(restored.objects.Single(item=>item.instanceId==selectedId).mediaAssetId,Is.EqualTo(restored.mediaAssets[0].id));
        }
        [Test] public void CameraImageCanBeEditedWithoutBreakingSharedReferences()
        {
            var original=Image(Color.red);var edited=Image(Color.cyan);var c=Open();Select(c,"jugador");c.CaptureImage();c.ReceiveImageImport(JsonUtility.ToJson(new RuntimeImageImportPayload{name="Dibujo fotografiado",dataUrl=original}));c.ReceiveImageBatchComplete("1");
            var media=c.Project.mediaAssets.Single();var id=media.id;var enemy=c.Project.objects.First(item=>item.definitionId=="enemigo");enemy.mediaAssetId=id;c.Rebuild();
            c.EditMediaAsset(id);c.ReceiveImageImport(JsonUtility.ToJson(new RuntimeImageImportPayload{name="Dibujo recortado",dataUrl=edited,originalDataUrl=original,cropX=.1f,cropY=.2f,cropWidth=.6f,cropHeight=.7f,paintEnabled=true,paintColor="#123456",removeBackground=true,backgroundColor="#abcdef",backgroundTolerance=32}));c.ReceiveImageBatchComplete("1");
            Assert.That(c.Project.mediaAssets.Count,Is.EqualTo(1));Assert.That(media.id,Is.EqualTo(id));Assert.That(media.displayName,Is.EqualTo("Dibujo recortado"));Assert.That(media.dataUrl,Is.EqualTo(edited));Assert.That(media.originalDataUrl,Is.EqualTo(original));Assert.That(media.cropX,Is.EqualTo(.1f));Assert.That(media.cropHeight,Is.EqualTo(.7f));Assert.That(media.paintEnabled);Assert.That(media.paintColor,Is.EqualTo("#123456"));Assert.That(media.removeBackground);Assert.That(media.backgroundTolerance,Is.EqualTo(32));Assert.That(media.source,Is.EqualTo(MediaAssetSource.Camera));Assert.That(c.Project.objects.Count(item=>item.mediaAssetId==id),Is.EqualTo(2));
            var restored=ProjectSerializer.FromJson(ProjectSerializer.ToJson(c.Project)).mediaAssets.Single();Assert.That(restored.originalDataUrl,Is.EqualTo(original));Assert.That(restored.cropY,Is.EqualTo(.2f));Assert.That(restored.backgroundColor,Is.EqualTo("#abcdef"));
            var users=c.buildRoot.GetComponentsInChildren<GameItem>().Where(item=>item.customSprite!=null&&c.Project.objects.Any(data=>data.instanceId==item.GetComponent<RuntimeAuthoredItem>()?.instanceId&&data.mediaAssetId==id)).ToArray();Assert.That(users.Length,Is.EqualTo(2));Assert.That(users[0].customSprite,Is.SameAs(users[1].customSprite));
        }
        [Test] public void MediaLibraryCategorizesRenamesMeasuresAndProtectsUsedImages()
        {
            var c=Open();Select(c,"jugador");c.CaptureImage();c.ReceiveImageImport(JsonUtility.ToJson(new RuntimeImageImportPayload{name="Mi héroe",dataUrl=Image(Color.magenta)}));c.ReceiveImageBatchComplete("1");var media=c.Project.mediaAssets.Single();var id=media.id;
            Assert.That(media.categoryId,Is.EqualTo("personajes"));Assert.That(c.MediaBytes,Is.GreaterThan(0));Assert.That(c.MediaUsageCount(id),Is.EqualTo(1));c.RenameMediaAsset(id,"Heroína");Assert.That(media.displayName,Is.EqualTo("Heroína"));c.CycleMediaCategory(id);Assert.That(media.categoryId,Is.EqualTo("enemigos"));
            c.DeleteMediaAsset(id);Assert.That(c.Project.mediaAssets.Count,Is.EqualTo(1),"Una imagen usada no debe borrarse sin confirmación.");c.DeleteMediaAsset(id,true);Assert.That(c.Project.mediaAssets,Is.Empty);Assert.That(c.Project.objects.All(item=>item.mediaAssetId!=id));
            var restored=ProjectSerializer.FromJson("{\"schemaVersion\":6,\"mediaAssets\":[{\"id\":\"old\",\"displayName\":\"Anterior\",\"dataUrl\":\"data:image/png;base64,YWJj\"}]}");Assert.That(restored.schemaVersion,Is.EqualTo(9));Assert.That(restored.mediaAssets.Single().categoryId,Is.EqualTo("otros"));Assert.That(restored.mediaAssets.Single().originalDataUrl,Is.EqualTo(restored.mediaAssets.Single().dataUrl));
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
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);yield return new EnterPlayMode();var c=UnityEngine.Object.FindAnyObjectByType<RuntimeAuthoringController>();c.LoadStarterLevel(false);Select(c,"plataforma");yield return null;var handles=UnityEngine.Object.FindObjectsByType<RuntimeResizeHandle>(FindObjectsInactive.Include);Assert.That(handles.Length,Is.EqualTo(4));Assert.That(handles.All(h=>h.gameObject.activeInHierarchy));var authored=c.Project.objects.Single(o=>o.definitionId=="jugador").position;Assert.That(c.EnterPlay(),Is.Null);yield return null;Assert.That(UnityEngine.Object.FindObjectsByType<RuntimeResizeHandle>(FindObjectsInactive.Include),Is.Empty);var player=c.PlayPlayer;var recovery=player.GetComponent<PlayerFallRecovery>();var playStart=recovery.SafePosition;var body=player.GetComponent<Rigidbody2D>();body.position=new Vector2(playStart.x,c.Project.bounds.bottom-2);body.linearVelocity=new Vector2(4,-8);body.angularVelocity=4;recovery.SendMessage("FixedUpdate");Assert.That(body.position.x,Is.EqualTo(playStart.x).Within(.001f));Assert.That(body.position.y,Is.EqualTo(playStart.y).Within(.001f));Assert.That(c.Project.objects.Single(o=>o.definitionId=="jugador").position,Is.EqualTo(authored));c.ExitPlay();yield return new ExitPlayMode();
        }
    }
}
