using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.PlaygroundBackend;
using CreaJuego.Starter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace CreaJuego.Starter.Tests
{
    public sealed class ExternalEnemyAppearanceIntegrationTests
    {
        static readonly string[] Names={"Gobbat","Gobbler","MawFlower","Naga","Scarecrow"};
        GameItem enemy;

        [SetUp] public void Setup()
        {
            if(Application.isPlaying) return;
            if(AssetDatabase.LoadAssetAtPath<GameObject>(ExternalAppearanceIntegration.EnemyPrefabRoot+"Gobbat.prefab")==null)
                Assert.Ignore("Platformer Game Kit no está instalado en esta copia.");
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            ExternalAppearanceIntegration.IntegrateEnemies();
            enemy=Object.FindObjectsByType<GameItem>().First(i=>i.definition.kind==ItemKind.Enemy);
            enemy.name="Enemigo animado de prueba";
            var category=enemy.definition.appearancePack.CategoryFor(ItemKind.Enemy);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ExternalAppearanceIntegration.EnemyPrefabRoot+"Gobbler.prefab");
            ItemAppearance.ChooseOption(new[]{enemy},category,category.options.Single(o=>o.prefab==prefab).id);
        }

        [UnityTearDown] public IEnumerator LeavePlay(){if(Application.isPlaying) yield return new ExitPlayMode();}

        [Test] public void RecipeAddsFiveAnimatedEnemiesAndSpikesWithAutomaticFit()
        {
            var category=enemy.definition.appearancePack.CategoryFor(ItemKind.Enemy);
            var collider=enemy.definition.prefab.GetComponent<BoxCollider2D>();
            foreach(string name in Names)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ExternalAppearanceIntegration.EnemyPrefabRoot+name+".prefab");
                var option=category.options.Single(o=>o.prefab==prefab);
                Assert.That(option.ValidationError,Is.Null,name);
                Assert.That(option.idleClip,Is.Not.Null,name);
                Assert.That(option.moveClip,Is.Not.Null,name);
                Assert.That(option.attackClip,Is.Not.Null,name);
                Assert.That(option.scale.x,Is.EqualTo(option.scale.y).Within(.0001f),name);
                Assert.That(option.Preview.bounds.size.y*option.scale.y,Is.EqualTo(collider.size.y).Within(.001f),name);
                foreach(var clip in new[]{option.idleClip,option.moveClip,option.jumpClip,option.attackClip}.Where(c=>c!=null))
                {
                    var bindings=AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    Assert.That(bindings.Length,Is.EqualTo(1),clip.name);
                    Assert.That(bindings[0].type,Is.EqualTo(typeof(SpriteRenderer)),clip.name);
                    Assert.That(bindings[0].path,Is.Empty,clip.name);
                }
            }
            var hazard=ItemService.Catalog().Single(d=>d.kind==ItemKind.Hazard);
            var spikes=AssetDatabase.LoadAssetAtPath<GameObject>(ExternalAppearanceIntegration.EnemyPrefabRoot+"Spikes.prefab");
            Assert.That(hazard.appearancePack.CategoryFor(ItemKind.Hazard).options.Count(o=>o.prefab==spikes),Is.EqualTo(1));
            var selected=enemy.SelectedAppearance;
            ItemAppearance.Apply(enemy,false);
            Assert.That(ItemVisual.Resolve(enemy).sprite,Is.EqualTo(selected.sprite));
            Assert.That(ItemVisual.Resolve(enemy).transform.localScale.x,Is.EqualTo(selected.scale.x).Within(.001f));
            Assert.That(ItemVisual.Resolve(enemy).transform.localScale.y,Is.EqualTo(selected.scale.y).Within(.001f));
        }

        [UnityTest] public IEnumerator GobblerPatrolAndContactUseMoveAndAttackAnimations()
        {
            yield return new EnterPlayMode();
            enemy=Object.FindObjectsByType<GameItem>().Single(i=>i.name=="Enemigo animado de prueba");
            var visual=enemy.GetComponent<ItemVisual>();
            var movingFrames=new HashSet<Sprite>();
            float until=Time.realtimeSinceStartup+.35f;
            while(Time.realtimeSinceStartup<until){movingFrames.Add(visual.renderer.sprite);yield return null;}
            Assert.That(movingFrames.Count,Is.GreaterThan(1));

            enemy.GetComponent<Playground.Movement.Patrol>().enabled=false;
            var player=Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player);
            var body=player.GetComponent<Rigidbody2D>(); body.gravityScale=0; body.position=enemy.transform.position; body.linearVelocity=Vector2.zero;
            int health=player.GetComponent<Playground.Attributes.HealthSystemAttribute>().health;
            until=Time.realtimeSinceStartup+.25f;
            while(Time.realtimeSinceStartup<until && player.GetComponent<Playground.Attributes.HealthSystemAttribute>().health==health) yield return null;
            Assert.That(player.GetComponent<Playground.Attributes.HealthSystemAttribute>().health,Is.LessThan(health));
            var attack=enemy.GetComponent<PlaygroundContactDamage>();
            Assert.That(attack.IsVisuallyAttacking,Is.True);
            var attackFrames=new HashSet<Sprite>();
            until=Time.realtimeSinceStartup+Mathf.Min(.3f,enemy.SelectedAppearance.attackClip.length*.8f);
            while(Time.realtimeSinceStartup<until){attackFrames.Add(visual.renderer.sprite);yield return null;}
            Assert.That(attackFrames.Count,Is.GreaterThan(1));
            yield return new ExitPlayMode();
        }
    }
}