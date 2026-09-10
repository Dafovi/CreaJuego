using System.Collections;
using System.IO;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.PlaygroundBackend;
using NUnit.Framework;
using Playground.Attributes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace CreaJuego.Starter.Tests
{
    public sealed class P1GateTests
    {
        [SetUp] public void Setup() { if(!Application.isPlaying) EditorSceneManager.OpenScene(DemoBuilder.ScenePath); }
        [TearDown] public void Cleanup() { if(!Application.isPlaying){Undo.ClearAll();EditorSceneManager.OpenScene(DemoBuilder.ScenePath);} }
        [UnityTearDown] public IEnumerator Leave() {if(Application.isPlaying)yield return new ExitPlayMode();}
        private static IEnumerator Wait(float seconds){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until)yield return null;}
        private static GameItem Create(ItemKind kind,Vector3 p)=>ItemService.Create(ItemService.Catalog().Single(d=>d.kind==kind),p);
        private static GameItem Named(string name)=>Object.FindObjectsByType<GameItem>().Single(i=>i.name==name);
        [UnityTest] public IEnumerator PatrolExtremesRelativeDuplicationAndPassengerDiagnostic()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SceneService.Prepare();
            var slow=Create(ItemKind.MovingPlatform,new Vector3(20,0,0));slow.name="P1 lento";slow.speed=.2f;slow.distance=.5f;
            var fast=Create(ItemKind.MovingPlatform,new Vector3(30,0,0));fast.name="P1 rápido";fast.speed=3;fast.distance=.5f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(fast);
            var copy=ItemService.Duplicate(fast);copy.name="P1 copia";copy.transform.position=new Vector3(40,0,0);
            var carrier=Create(ItemKind.MovingPlatform,Vector3.zero);carrier.name="P1 pasajero";carrier.speed=3;carrier.distance=6;
            Create(ItemKind.Player,new Vector3(0,.75f,0));
            yield return new EnterPlayMode();
            slow=Named("P1 lento");fast=Named("P1 rápido");copy=Named("P1 copia");carrier=Named("P1 pasajero");
            var player=Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player);
            float maxSlow=20,maxFast=30,maxCopy=40,minSlowAfterTurn=100,peakPassengerError=0;
            float deadline=Time.realtimeSinceStartup+6;
            while(Time.realtimeSinceStartup<deadline)
            {
                maxSlow=Mathf.Max(maxSlow,slow.transform.position.x);maxFast=Mathf.Max(maxFast,fast.transform.position.x);maxCopy=Mathf.Max(maxCopy,copy.transform.position.x);
                if(maxSlow>20.35f)minSlowAfterTurn=Mathf.Min(minSlowAfterTurn,slow.transform.position.x);
                peakPassengerError=Mathf.Max(peakPassengerError,Mathf.Abs(carrier.transform.position.x-player.transform.position.x));
                Assert.That(fast.transform.position.x,Is.InRange(29.9f,30.65f),"No waypoint overshoot/oscillation");
                Assert.That(copy.transform.position.x,Is.InRange(39.9f,40.65f),"Duplicated path stays relative");
                yield return null;
            }
            Assert.That(maxSlow,Is.GreaterThan(20.35f));Assert.That(minSlowAfterTurn,Is.LessThan(20.15f));
            Assert.That(maxFast,Is.GreaterThan(30.35f));Assert.That(maxCopy,Is.GreaterThan(40.35f));
            bool passengerReady=peakPassengerError<.75f;
            Directory.CreateDirectory("Docs");
            File.WriteAllText("Docs/P1-platform-gate.txt","Round trip/ranges/relative duplication: PASS\nPassenger max separation: "+peakPassengerError.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"\nWorkshop ready: "+passengerReady);
            Debug.Log("P1_PLATFORM_PASSENGER_READY="+passengerReady+" separation="+peakPassengerError);
            Assert.That(passengerReady,Is.True,"The character must travel with the moving platform");
            Assert.That(ItemService.WorkshopCatalog().Any(d=>d.kind==ItemKind.MovingPlatform),Is.True);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator EnemyPatrolContactDefeatAndRelativeCopy()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SceneService.Prepare();
            var ground=Create(ItemKind.Platform,new Vector3(0,-1,0));ground.transform.localScale=new Vector3(10,1,1);
            Create(ItemKind.Player,Vector3.zero);
            var enemy=Create(ItemKind.Enemy,new Vector3(5,0,0));enemy.name="P1 enemigo";enemy.speed=1;enemy.distance=.5f;enemy.damage=2;
            PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
            var copy=ItemService.Duplicate(enemy);copy.name="P1 enemigo copia";copy.transform.position=new Vector3(10,0,0);
            yield return new EnterPlayMode();
            enemy=Named("P1 enemigo");copy=Named("P1 enemigo copia");
            float max=5,minAfter=100,copyMax=10,deadline=Time.realtimeSinceStartup+1.5f;
            while(Time.realtimeSinceStartup<deadline)
            {
                max=Mathf.Max(max,enemy.transform.position.x);copyMax=Mathf.Max(copyMax,copy.transform.position.x);
                if(max>5.3f)minAfter=Mathf.Min(minAfter,enemy.transform.position.x);
                Assert.That(copy.transform.position.x,Is.InRange(9.9f,10.65f));
                yield return null;
            }
            Assert.That(max,Is.GreaterThan(5.3f));Assert.That(minAfter,Is.LessThan(5.15f));Assert.That(copyMax,Is.GreaterThan(10.3f));
            var player=Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player);
            var body=player.GetComponent<Rigidbody2D>();body.gravityScale=0;body.position=enemy.transform.position;body.linearVelocity=Vector2.zero;
            yield return Wait(.1f);
            Assert.That(player.GetComponent<HealthSystemAttribute>().health,Is.EqualTo(1));
            Assert.That(enemy!=null,Is.True);Assert.That(enemy.GetComponent<ModifyHealthAttribute>().destroyWhenActivated,Is.False);
            body.position=new Vector2(-3,0);yield return Wait(PlayerDamageReceiver.GraceSeconds+.1f);body.position=enemy.transform.position;
            yield return Wait(.1f);
            var session=DemoSession.InScene(enemy.gameObject.scene);
            Assert.That(session.State,Is.EqualTo(GameSessionState.Lost));Assert.That(player==null,Is.True);
            Assert.That(enemy.GetComponent<ModifyHealthAttribute>().interactionAllowed(),Is.False);
            yield return new ExitPlayMode();
            yield return new EnterPlayMode();yield return Wait(.1f);
            enemy=Named("P1 enemigo");session=DemoSession.InScene(enemy.gameObject.scene);session.Complete("Final");
            Assert.That(enemy.GetComponent<ModifyHealthAttribute>().interactionAllowed(),Is.False);
            Assert.That(enemy.GetComponent<Rigidbody2D>().simulated,Is.False);
            yield return new ExitPlayMode();
        }
    }
}


