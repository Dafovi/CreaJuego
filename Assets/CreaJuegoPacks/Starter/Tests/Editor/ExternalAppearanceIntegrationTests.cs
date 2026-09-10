using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CreaJuego.Editor;
using CreaJuego.Starter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace CreaJuego.Starter.Tests
{
    public sealed class ExternalAppearanceIntegrationTests
    {
        static GameItem Player()=>Object.FindObjectsByType<GameItem>().Single(i=>i.definition.kind==ItemKind.Player);
        [SetUp] public void Setup()
        {
            if(Application.isPlaying) return;
            if(AssetDatabase.LoadAssetAtPath<GameObject>(ExternalAppearanceIntegration.GinoPrefabPath)==null)
                Assert.Ignore("Platformer Game Kit no está instalado en esta copia.");
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            ExternalAppearanceIntegration.IntegrateGino();
            var category=Player().definition.appearancePack.CategoryFor(ItemKind.Player);
            var option=category.options.Single(o=>o.prefab==AssetDatabase.LoadAssetAtPath<GameObject>(ExternalAppearanceIntegration.GinoPrefabPath));
            ItemAppearance.ChooseOption(new[]{Player()},category,option.id);
        }
        [UnityTearDown] public IEnumerator LeavePlay(){if(Application.isPlaying) yield return new ExitPlayMode();}

        [Test] public void GinoUsesPrefabAndFourDirectSpriteClips()
        {
            var option=Player().SelectedAppearance;
            Assert.That(option.sprite,Is.Not.Null);
            Assert.That(option.idleClip,Is.Not.Null);
            Assert.That(option.moveClip,Is.Not.Null);
            Assert.That(option.jumpClip,Is.Not.Null);
            Assert.That(option.attackClip,Is.Not.Null);
            foreach(var clip in new[]{option.idleClip,option.moveClip,option.jumpClip,option.attackClip}) {
                var bindings=AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Assert.That(bindings.Length,Is.EqualTo(1));
                Assert.That(bindings[0].type,Is.EqualTo(typeof(SpriteRenderer)));
                Assert.That(bindings[0].path,Is.Empty);
            }
        }

        [UnityTest] public IEnumerator GinoMovesJumpsAndCompletesAttackClipWithPlayground()
        {
            yield return new EnterPlayMode();
            var player=Player();
            var visual=player.GetComponent<ItemVisual>();
            var body=player.GetComponent<Rigidbody2D>();
            var move=player.GetComponent<Playground.Movement.Move>();
            yield return null;
            move.movementSource=()=>Vector2.right;
            float start=body.position.x,until=Time.realtimeSinceStartup+.35f;
            var frames=new HashSet<Sprite>();
            while(Time.realtimeSinceStartup<until){frames.Add(visual.renderer.sprite);yield return null;}
            Assert.That(body.position.x,Is.GreaterThan(start+.05f));
            Assert.That(frames.Count,Is.GreaterThan(1));
            move.movementSource=()=>Vector2.zero;
            var attack=player.GetComponent<PlayerAttack>();
            Assert.That(attack.TryAttack(),Is.True);
            float duration=player.SelectedAppearance.attackClip.length;
            yield return null;
            Assert.That(attack.IsAttacking,Is.True);
            until=Time.realtimeSinceStartup+duration-.08f;
            while(Time.realtimeSinceStartup<until) yield return null;
            Assert.That(attack.IsAttacking,Is.True);
            until=Time.realtimeSinceStartup+.12f;
            while(Time.realtimeSinceStartup<until) yield return null;
            Assert.That(attack.IsAttacking,Is.False);
            yield return new ExitPlayMode();
        }
    }
}
