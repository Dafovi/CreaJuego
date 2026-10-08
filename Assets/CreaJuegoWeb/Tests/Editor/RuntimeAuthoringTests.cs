using System.Collections.Generic;
using System.IO;
using System.Linq;
using CreaJuego.Web;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CreaJuego.Web.Tests
{
    public sealed class RuntimeAuthoringTests
    {
        [Test] public void SelectionResolvesVisualChild(){var go=new GameObject("Item",typeof(GameItem));var child=new GameObject("Visual");child.transform.SetParent(go.transform);var service=new RuntimeSelectionService();Assert.That(service.Select(child),Is.EqualTo(go.GetComponent<GameItem>()));Object.DestroyImmediate(go);}
        [Test] public void ProjectRoundTripPreservesAppearance(){var data=new CreaJuegoProjectData();data.objects.Add(new RuntimeItemData{instanceId="1",definitionId="jugador",appearanceId="gino",position=new Vector3(2,3)});var restored=ProjectSerializer.FromJson(ProjectSerializer.ToJson(data));Assert.That(restored.objects.Single().appearanceId,Is.EqualTo("gino"));Assert.That(restored.objects.Single().position,Is.EqualTo(new Vector3(2,3)));}
        [Test] public void SharedMediaRoundTripKeepsOneImageForSeveralObjects()
        {
            var data=new CreaJuegoProjectData();var media=MediaAssetData.Create("Dragón","data:image/png;base64,YWJj");data.mediaAssets.Add(media);data.objects.Add(new RuntimeItemData{instanceId="1",definitionId="jugador",mediaAssetId=media.id});data.objects.Add(new RuntimeItemData{instanceId="2",definitionId="enemigo",mediaAssetId=media.id});
            var restored=ProjectSerializer.FromJson(ProjectSerializer.ToJson(data));Assert.That(restored.schemaVersion,Is.EqualTo(10));Assert.That(restored.mediaAssets.Count,Is.EqualTo(1));Assert.That(restored.objects.Select(item=>item.mediaAssetId).Distinct().Single(),Is.EqualTo(restored.mediaAssets.Single().id));
        }
        [Test] public void LegacyEmbeddedImagesMigrateAndDeduplicate()
        {
            const string legacy="{\"schemaVersion\":4,\"objects\":[{\"instanceId\":\"1\",\"definitionId\":\"jugador\",\"customImageBase64\":\"data:image/png;base64,YWJj\"},{\"instanceId\":\"2\",\"definitionId\":\"enemigo\",\"customImageBase64\":\"data:image/png;base64,YWJj\"}]}";
            var restored=ProjectSerializer.FromJson(legacy);Assert.That(restored.mediaAssets.Count,Is.EqualTo(1));Assert.That(restored.objects.All(item=>item.mediaAssetId==restored.mediaAssets[0].id),Is.True);Assert.That(restored.objects.All(item=>string.IsNullOrEmpty(item.customImageBase64)),Is.True);
        }
        [Test] public void HistoryUndoRedoRestoresMove(){var data=new CreaJuegoProjectData();data.objects.Add(new RuntimeItemData{instanceId="1",position=Vector3.zero});var history=new RuntimeHistory();history.Reset(data);data.objects[0].position=Vector3.right*4;history.Record(data);Assert.That(history.Undo().objects[0].position,Is.EqualTo(Vector3.zero));Assert.That(history.Redo().objects[0].position,Is.EqualTo(Vector3.right*4));}
        [Test] public void FileStorageSavesAndLoads(){var path=Path.Combine(Path.GetTempPath(),"creajuego-storage-test.json");try{var storage=new FileProjectStorage(path);storage.Save("{\"ok\":true}");Assert.That(storage.Exists);Assert.That(storage.Load(),Does.Contain("true"));}finally{if(File.Exists(path))File.Delete(path);}}
        [Test] public void StarterLevelIsLayeredAndNewProjectIsCompletelyEmpty()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();controller.LoadStarterLevel(false);
            Assert.That(controller.Project.levelSize,Is.EqualTo(RuntimeLevelSize.Small));Assert.That(controller.Project.objects.Count,Is.GreaterThanOrEqualTo(60));
            Assert.That(controller.Project.objects.Count(o=>o.definitionId=="jugador"),Is.EqualTo(1));Assert.That(controller.Project.objects.Count(o=>o.definitionId=="meta"),Is.EqualTo(1));Assert.That(controller.Project.objects.Count(o=>o.definitionId=="plataforma"),Is.GreaterThanOrEqualTo(12));Assert.That(controller.Project.objects.Count(o=>o.definitionId=="muro"),Is.GreaterThanOrEqualTo(12));
            Assert.That(controller.Project.objects.Where(o=>o.definitionId=="plataforma").Select(o=>o.position.y).Distinct().Count(),Is.GreaterThanOrEqualTo(4));
            Assert.That(controller.Project.objects.Single(o=>o.definitionId=="fondo").appearanceId,Is.EqualTo("workshop-forest-background"));
            Assert.That(controller.Project.objects.Where(o=>o.definitionId=="plataforma").All(o=>o.appearanceId=="workshop-grass-platform"),Is.True);
            Assert.That(controller.Project.objects.Where(o=>o.definitionId=="premio").All(o=>o.appearanceId=="workshop-gold-coin"),Is.True);
            foreach(var pair in new[]{(ItemKind.Background,"workshop-forest-background"),(ItemKind.Platform,"workshop-grass-platform"),(ItemKind.Platform,"workshop-grass-wall"),(ItemKind.Platform,"workshop-grass-ramp"),(ItemKind.MovingPlatform,"workshop-grass-moving"),(ItemKind.Prize,"workshop-gold-coin"),(ItemKind.Hazard,"workshop-grass-spikes"),(ItemKind.Goal,"workshop-red-flag"),(ItemKind.Decoration,"workshop-large-tree"),(ItemKind.Decoration,"workshop-flower-bush")})Assert.That(controller.contentPack.CategoryFor(pair.Item1)?.Find(pair.Item2),Is.Not.Null,$"Falta la apariencia {pair.Item2} en {pair.Item1}.");
            var platformLooks=controller.contentPack.CategoryFor(ItemKind.Platform);var wallLook=platformLooks.Find("workshop-grass-wall");Assert.That(wallLook.sprite.rect.width/wallLook.sprite.rect.height,Is.EqualTo(1).Within(.01f),"La base debe usar una baldosa cuadrada para no repetirse como columnas.");
            Assert.That(controller.contentPack.CategoryFor(ItemKind.Prize).Find("workshop-gold-coin").scale.x,Is.LessThanOrEqualTo(.1f));Assert.That(controller.contentPack.CategoryFor(ItemKind.Hazard).Find("workshop-grass-spikes").scale.x,Is.LessThanOrEqualTo(.25f));Assert.That(controller.contentPack.CategoryFor(ItemKind.Decoration).Find("workshop-large-tree").scale.x,Is.LessThanOrEqualTo(.5f));
            Assert.That(controller.Project.objects.Single(o=>o.definitionId=="jugador").appearanceId,Is.EqualTo("5b84b5bdbf244cecb12b976bbf0aa6c7"));
            Assert.That(controller.Project.objects.Where(o=>o.definitionId=="enemigo").All(o=>o.appearanceId.StartsWith("platformer-kit-")),Is.True);
            var platforms=controller.Project.objects.Where(o=>o.definitionId=="plataforma").ToArray();
            foreach(var ramp in controller.Project.objects.Where(o=>o.definitionId=="rampa"))
            {
                var direction=new Vector2(Mathf.Cos(ramp.rotationZ*Mathf.Deg2Rad),Mathf.Sin(ramp.rotationZ*Mathf.Deg2Rad));var half=direction*ramp.platformWidth*.5f;
                foreach(var edge in new[]{(Vector2)ramp.position-half,(Vector2)ramp.position+half})Assert.That(platforms.Any(platform=>Mathf.Abs(platform.position.y-edge.y)<.08f&&Mathf.Abs(Mathf.Abs(platform.position.x-edge.x)-platform.platformWidth*.5f)<.08f),Is.True,$"La rampa en {ramp.position} no toca una plataforma en {edge}.");
            }
            foreach(var platform in controller.buildRoot.GetComponentsInChildren<GameItem>().Where(item=>item.definition.id=="plataforma"||item.definition.id=="movil"||item.definition.id=="rampa"))
            {
                var collider=platform.GetComponent<Collider2D>();var effector=platform.GetComponent<PlatformEffector2D>();var oneWay=platform.GetComponent<OneWayPlatformSurface>();
                Assert.That(oneWay,Is.Not.Null);Assert.That(collider.usedByEffector,Is.True);Assert.That(effector,Is.Not.Null);Assert.That(effector.useOneWay,Is.True);Assert.That(effector.useOneWayGrouping,Is.True);Assert.That(effector.surfaceArc,Is.EqualTo(160f));Assert.That(effector.useSideFriction,Is.False);Assert.That(effector.useSideBounce,Is.False);
            }
            foreach(var wall in controller.buildRoot.GetComponentsInChildren<GameItem>().Where(item=>item.definition.id=="muro"))
            {
                var collider=wall.GetComponent<Collider2D>();Assert.That(collider.usedByEffector,Is.False);Assert.That(wall.GetComponent<PlatformEffector2D>(),Is.Null);Assert.That(collider.bounds.size.y,Is.GreaterThanOrEqualTo(2.9f));Assert.That(collider.bounds.size.x,Is.GreaterThanOrEqualTo(5.9f));
            }
            var soilDefinition=controller.Find("suelo");Assert.That(soilDefinition,Is.Not.Null);var soil=controller.Create("suelo",new Vector3(0,-12));Assert.That(soil,Is.Not.Null);Assert.That(soil.GetComponent<OneWayPlatformSurface>(),Is.Null);Assert.That(soil.GetComponent<PlatformEffector2D>(),Is.Null);Assert.That(soil.GetComponent<Collider2D>().usedByEffector,Is.False);
            var playerItem=controller.buildRoot.GetComponentsInChildren<GameItem>().Single(item=>item.definition.kind==ItemKind.Player);var playerCollider=playerItem.GetComponent<Collider2D>();var support=controller.buildRoot.GetComponentsInChildren<GameItem>().Where(item=>item.definition.id=="plataforma").Select(item=>item.GetComponent<Collider2D>()).First(collider=>playerCollider.bounds.center.x>=collider.bounds.min.x&&playerCollider.bounds.center.x<=collider.bounds.max.x);
            Assert.That(Mathf.Abs(playerCollider.bounds.min.y-support.bounds.max.y),Is.LessThan(.05f));
            foreach(var marker in controller.buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>().Where(value=>controller.Project.objects.Single(data=>data.instanceId==value.instanceId).appearanceId.StartsWith("workshop-")&&value.GetComponent<GameItem>().definition.kind!=ItemKind.Background)){var renderer=ItemVisual.Resolve(marker.GetComponent<GameItem>());Assert.That(renderer,Is.Not.Null);Assert.That(renderer.sprite,Is.Not.Null);Assert.That(renderer.bounds.size.x,Is.InRange(.05f,15f));Assert.That(renderer.bounds.size.y,Is.InRange(.05f,15f));}
            AssertStarterRouteIsConnected(controller.Project);
            controller.NewProject(false);Assert.That(controller.Project.objects,Is.Empty);Assert.That(controller.buildRoot.childCount,Is.EqualTo(0));
        }
        static void AssertStarterRouteIsConnected(CreaJuegoProjectData project)
        {
            var platforms=project.objects.Where(item=>item.definitionId=="plataforma").ToArray();var ramps=project.objects.Where(item=>item.definitionId=="rampa").ToArray();var start=platforms.OrderBy(item=>Mathf.Abs(item.position.x+22)+Mathf.Abs(item.position.y+8)).First();var goal=platforms.OrderBy(item=>Mathf.Abs(item.position.x-21)+Mathf.Abs(item.position.y-14)).First();
            bool Touches(RuntimeItemData platform,Vector2 edge)=>Mathf.Abs(platform.position.y-edge.y)<.08f&&Mathf.Abs(Mathf.Abs(platform.position.x-edge.x)-platform.platformWidth*.5f)<.08f;
            bool Linked(RuntimeItemData a,RuntimeItemData b)
            {
                var overlap=Mathf.Min(a.position.x+a.platformWidth*.5f,b.position.x+b.platformWidth*.5f)-Mathf.Max(a.position.x-a.platformWidth*.5f,b.position.x-b.platformWidth*.5f);if(overlap>=0&&Mathf.Abs(a.position.y-b.position.y)<=2.1f)return true;
                foreach(var ramp in ramps){var direction=new Vector2(Mathf.Cos(ramp.rotationZ*Mathf.Deg2Rad),Mathf.Sin(ramp.rotationZ*Mathf.Deg2Rad));var half=direction*ramp.platformWidth*.5f;var first=(Vector2)ramp.position-half;var second=(Vector2)ramp.position+half;if(Touches(a,first)&&Touches(b,second)||Touches(a,second)&&Touches(b,first))return true;}return false;
            }
            var visited=new HashSet<string>{start.instanceId};var pending=new Queue<RuntimeItemData>();pending.Enqueue(start);
            while(pending.Count>0){var current=pending.Dequeue();foreach(var next in platforms)if(!visited.Contains(next.instanceId)&&Linked(current,next)){visited.Add(next.instanceId);pending.Enqueue(next);}}
            Assert.That(visited.Contains(goal.instanceId),Is.True,"No existe una ruta continua desde Gino hasta la bandera.");Assert.That(visited.Count,Is.EqualTo(platforms.Length),"Hay una superficie del mapa inicial aislada del recorrido principal.");
        }
        [Test] public void ManualSaveSurvivesNewProjectAndRecoveryAutosave()
        {
            string token=System.Guid.NewGuid().ToString("N"),manualPath=Path.Combine(Path.GetTempPath(),token+"-manual.creajuego"),recoveryPath=Path.Combine(Path.GetTempPath(),token+"-recovery.creajuego");
            try
            {
                EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();controller.ConfigureStorage(new FileProjectStorage(manualPath),new FileProjectStorage(recoveryPath));
                controller.LoadStarterLevel(false);controller.Project.projectName="Aventura guardada";controller.Project.objects.Single(o=>o.definitionId=="jugador").health=7;controller.SaveNow();
                controller.NewProject(false);controller.Project.projectName="Proyecto nuevo";controller.SaveRecovery();controller.LoadLast();
                Assert.That(controller.Project.projectName,Is.EqualTo("Aventura guardada"));Assert.That(controller.Project.objects.Single(o=>o.definitionId=="jugador").health,Is.EqualTo(7));Assert.That(ProjectSerializer.FromJson(File.ReadAllText(recoveryPath)).projectName,Is.EqualTo("Proyecto nuevo"));
            }
            finally{if(File.Exists(manualPath))File.Delete(manualPath);if(File.Exists(recoveryPath))File.Delete(recoveryPath);}
        }
        [Test] public void LevelSizesAreDoubledAndFrameThickensAtDistance()
        {
            var small=RuntimeLevelBounds.For(RuntimeLevelSize.Small);var medium=RuntimeLevelBounds.For(RuntimeLevelSize.Medium);var large=RuntimeLevelBounds.For(RuntimeLevelSize.Large);var huge=RuntimeLevelBounds.For(RuntimeLevelSize.ExtraLarge);
            Assert.That(small.right-small.left,Is.EqualTo(48));Assert.That(medium.right-medium.left,Is.EqualTo(100));Assert.That(large.right-large.left,Is.EqualTo(200));Assert.That(huge.right-huge.left,Is.EqualTo(480));
            const int height=1000;float near=RuntimeGuideScale.WorldWidth(8,height),far=RuntimeGuideScale.WorldWidth(100,height);Assert.That(near*height/(2*8),Is.EqualTo(4).Within(.01f));Assert.That(far*height/(2*100),Is.EqualTo(8).Within(.01f));Assert.That(far,Is.GreaterThan(near));
        }
        [Test] public void CreateMovePropertyUndoAndModesWork()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();controller.LoadStarterLevel(false);
            int before=controller.Project.objects.Count;var created=controller.Create("decoracion",Vector3.zero);Assert.That(controller.Project.objects.Count,Is.EqualTo(before+1));
            controller.MoveSelected(new Vector3(4,2),true);Assert.That(controller.SelectedData().position,Is.EqualTo(new Vector3(4,2)));
            controller.Undo();Assert.That(controller.Project.objects.Count,Is.EqualTo(before+1));
            var player=controller.Project.objects.Single(o=>o.definitionId=="jugador");var marker=Object.FindObjectsByType<RuntimeAuthoredItem>().Single(m=>m.instanceId==player.instanceId);controller.Selection.Select(marker.gameObject);controller.SetFloat("health",7);Assert.That(controller.SelectedData().health,Is.EqualTo(7));
            Assert.That(controller.EnterPlay(),Is.Null);Assert.That(controller.Mode,Is.EqualTo(AuthoringMode.Play));controller.ExitPlay();Assert.That(controller.Mode,Is.EqualTo(AuthoringMode.Build));Assert.That(controller.Project.objects.Single(o=>o.definitionId=="jugador").health,Is.EqualTo(7));
        }
        [Test] public void PreflightExplainsMissingPlayer(){var data=new CreaJuegoProjectData();Assert.That(RuntimePreflight.Validate(data,_=>null),Does.Contain("Jugador"));}
        [Test] public void NewProjectTypeSelectionCreatesBothAvailableModes()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();var ui=controller.GetComponent<RuntimeAuthoringUI>();ui.RefreshEditableLayout();controller.LoadStarterLevel(false);
            ui.ShowNewProjectDialog();Assert.That(ui.NewProjectDialogVisible,Is.True);Assert.That(ui.VisibleGameTypeCount,Is.GreaterThanOrEqualTo(2));Assert.That(ui.ChooseGameType("catch-and-dodge"),Is.True);Assert.That(controller.Project.gameTypeId,Is.EqualTo("catch-and-dodge"));Assert.That(controller.Project.objects,Is.Empty);Assert.That(controller.DefinitionAvailable(controller.Find("premio")));Assert.That(controller.DefinitionAvailable(controller.Find("peligro")));Assert.That(controller.DefinitionAvailable(controller.Find("plataforma")),Is.False);
            ui.ShowNewProjectDialog();
            Assert.That(ui.ChooseGameType("platformer"),Is.True);Assert.That(controller.Project.gameTypeId,Is.EqualTo("platformer"));Assert.That(controller.Project.objects,Is.Empty);Assert.That(ui.NewProjectDialogVisible,Is.False);
            var restored=ProjectSerializer.FromJson(ProjectSerializer.ToJson(controller.Project));Assert.That(restored.gameTypeId,Is.EqualTo("platformer"));Assert.That(restored.schemaVersion,Is.EqualTo(10));
        }
    }
}
