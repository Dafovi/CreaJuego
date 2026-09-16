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
        [Test] public void CreateMovePropertyUndoAndModesWork()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.WebSpikeBuilder.ScenePath);
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();controller.NewProject(false);
            int before=controller.Project.objects.Count;var created=controller.Create("decoracion",Vector3.zero);Assert.That(controller.Project.objects.Count,Is.EqualTo(before+1));
            controller.MoveSelected(new Vector3(4,2),true);Assert.That(controller.SelectedData().position,Is.EqualTo(new Vector3(4,2)));
            controller.Undo();Assert.That(controller.Project.objects.Count,Is.EqualTo(before+1));
            var player=controller.Project.objects.Single(o=>o.definitionId=="jugador");var marker=Object.FindObjectsByType<RuntimeAuthoredItem>().Single(m=>m.instanceId==player.instanceId);controller.Selection.Select(marker.gameObject);controller.SetFloat("health",7);Assert.That(controller.SelectedData().health,Is.EqualTo(7));
            Assert.That(controller.EnterPlay(),Is.Null);Assert.That(controller.Mode,Is.EqualTo(AuthoringMode.Play));controller.ExitPlay();Assert.That(controller.Mode,Is.EqualTo(AuthoringMode.Build));Assert.That(controller.Project.objects.Single(o=>o.definitionId=="jugador").health,Is.EqualTo(7));
        }
        [Test] public void PreflightExplainsMissingPlayer(){var data=new CreaJuegoProjectData();Assert.That(RuntimePreflight.Validate(data,_=>null),Does.Contain("Jugador"));}
    }
}