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
        [Test] public void HistoryUndoRedoRestoresMove(){var data=new CreaJuegoProjectData();data.objects.Add(new RuntimeItemData{instanceId="1",position=Vector3.zero});var history=new RuntimeHistory();history.Reset(data);data.objects[0].position=Vector3.right*4;history.Record(data);Assert.That(history.Undo().objects[0].position,Is.EqualTo(Vector3.zero));Assert.That(history.Redo().objects[0].position,Is.EqualTo(Vector3.right*4));}
        [Test] public void FileStorageSavesAndLoads(){var path=Path.Combine(Path.GetTempPath(),"creajuego-storage-test.json");try{var storage=new FileProjectStorage(path);storage.Save("{\"ok\":true}");Assert.That(storage.Exists);Assert.That(storage.Load(),Does.Contain("true"));}finally{if(File.Exists(path))File.Delete(path);}}
        [Test] public void StarterLevelIsLayeredAndNewProjectIsCompletelyEmpty()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();controller.LoadStarterLevel(false);
            Assert.That(controller.Project.levelSize,Is.EqualTo(RuntimeLevelSize.Small));Assert.That(controller.Project.objects.Count,Is.GreaterThanOrEqualTo(35));
            Assert.That(controller.Project.objects.Count(o=>o.definitionId=="jugador"),Is.EqualTo(1));Assert.That(controller.Project.objects.Count(o=>o.definitionId=="meta"),Is.EqualTo(1));Assert.That(controller.Project.objects.Count(o=>o.definitionId=="plataforma"),Is.GreaterThanOrEqualTo(12));
            Assert.That(controller.Project.objects.Where(o=>o.definitionId=="plataforma").Select(o=>o.position.y).Distinct().Count(),Is.GreaterThanOrEqualTo(4));
            Assert.That(controller.Project.objects.Single(o=>o.definitionId=="fondo").appearanceId,Is.EqualTo("platformer-sky-evening"));
            Assert.That(controller.Project.objects.Where(o=>o.definitionId=="plataforma").Select(o=>o.appearanceId).Distinct().Count(),Is.GreaterThanOrEqualTo(3));
            Assert.That(controller.Project.objects.Where(o=>o.definitionId=="premio").Select(o=>o.appearanceId),Is.EquivalentTo(new[]{"tiny-dungeon-116","tiny-dungeon-1027","platformer-treasure","tiny-dungeon-116","tiny-dungeon-1027","platformer-treasure"}));
            Assert.That(controller.Project.objects.Single(o=>o.definitionId=="jugador").appearanceId,Is.EqualTo("5b84b5bdbf244cecb12b976bbf0aa6c7"));
            Assert.That(controller.Project.objects.Where(o=>o.definitionId=="enemigo").All(o=>o.appearanceId.StartsWith("platformer-kit-")),Is.True);
            var platforms=controller.Project.objects.Where(o=>o.definitionId=="plataforma").ToArray();
            foreach(var ramp in controller.Project.objects.Where(o=>o.definitionId=="rampa"))
            {
                var direction=new Vector2(Mathf.Cos(ramp.rotationZ*Mathf.Deg2Rad),Mathf.Sin(ramp.rotationZ*Mathf.Deg2Rad));var half=direction*ramp.platformWidth*.5f;
                foreach(var edge in new[]{(Vector2)ramp.position-half,(Vector2)ramp.position+half})Assert.That(platforms.Any(platform=>Mathf.Abs(platform.position.y-edge.y)<.05f&&Mathf.Abs(Mathf.Abs(platform.position.x-edge.x)-platform.platformWidth*.5f)<.05f),Is.True,$"La rampa en {ramp.position} no toca una plataforma en {edge}.");
            }
            foreach(var platform in controller.buildRoot.GetComponentsInChildren<GameItem>().Where(item=>item.definition.id=="plataforma"||item.definition.id=="movil"||item.definition.id=="rampa"))
            {
                var collider=platform.GetComponent<Collider2D>();var effector=platform.GetComponent<PlatformEffector2D>();var oneWay=platform.GetComponent<OneWayPlatformSurface>();
                Assert.That(oneWay,Is.Not.Null);Assert.That(collider.usedByEffector,Is.True);Assert.That(effector,Is.Not.Null);Assert.That(effector.useOneWay,Is.True);Assert.That(effector.useOneWayGrouping,Is.True);Assert.That(effector.surfaceArc,Is.EqualTo(160f));Assert.That(effector.useSideFriction,Is.False);Assert.That(effector.useSideBounce,Is.False);
            }
            var playerItem=controller.buildRoot.GetComponentsInChildren<GameItem>().Single(item=>item.definition.kind==ItemKind.Player);var playerCollider=playerItem.GetComponent<Collider2D>();var support=controller.buildRoot.GetComponentsInChildren<GameItem>().Where(item=>item.definition.id=="plataforma").Select(item=>item.GetComponent<Collider2D>()).First(collider=>playerCollider.bounds.center.x>=collider.bounds.min.x&&playerCollider.bounds.center.x<=collider.bounds.max.x);
            Assert.That(Mathf.Abs(playerCollider.bounds.min.y-support.bounds.max.y),Is.LessThan(.05f));
            controller.NewProject(false);Assert.That(controller.Project.objects,Is.Empty);Assert.That(controller.buildRoot.childCount,Is.EqualTo(0));
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
    }
}
