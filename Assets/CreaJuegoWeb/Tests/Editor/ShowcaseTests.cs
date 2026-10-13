using System.Collections;
using System.Linq;
using CreaJuego.Web;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.TestTools;

namespace CreaJuego.Web.Tests
{
    public sealed class ShowcaseTests
    {
        [Test]
        public void ShowcaseIsRichEditableAndExcludedFromMainBuild()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(global::CreaJuego.Web.Editor.ShowcaseBuilder.ScenePath),Is.Not.Null);
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.ShowcaseBuilder.ScenePath,OpenSceneMode.Single);
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();
            Assert.That(controller,Is.Not.Null);
            Assert.That(controller.contentPack.id,Is.EqualTo("creajuego-showcase"));
            Assert.That(controller.HasEditableScene,Is.True);
            var authored=controller.ReadEditableSceneProject();
            Assert.That(authored.levelSize,Is.EqualTo(RuntimeLevelSize.Large));
            Assert.That(authored.objects.Count,Is.GreaterThanOrEqualTo(30));
            foreach(var required in new[]{"jugador","plataforma","movil","premio","peligro","enemigo","meta","fondo"})
                Assert.That(authored.objects.Any(o=>o.definitionId==required),Is.True,required);
            Assert.That(controller.contentPack.CategoryFor(ItemKind.MovingPlatform).Find("showcase-2dkit-moving-platform")?.Preview,Is.Not.Null);
            Assert.That(controller.contentPack.CategoryFor(ItemKind.Hazard).Find("showcase-2dkit-spikes")?.Preview,Is.Not.Null);
            Assert.That(controller.contentPack.CategoryFor(ItemKind.Goal).Find("showcase-2dkit-door")?.Preview,Is.Not.Null);
            Assert.That(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==global::CreaJuego.Web.Editor.ShowcaseBuilder.ScenePath),Is.False);
        }

        [UnityTest]
        public IEnumerator ShowcaseEntersPlayWithItsAuthoredProject()
        {
            EditorSceneManager.OpenScene(global::CreaJuego.Web.Editor.ShowcaseBuilder.ScenePath,OpenSceneMode.Single);
            yield return new EnterPlayMode();
            var controller=Object.FindAnyObjectByType<RuntimeAuthoringController>();
            Assert.That(controller.Project.projectName,Is.EqualTo("El santuario perdido"));
            Assert.That(controller.Project.objects.Count,Is.GreaterThanOrEqualTo(30));
            Assert.That(controller.EnterPlay(),Is.Null);
            yield return null;
            Assert.That(controller.Mode,Is.EqualTo(AuthoringMode.Play));
            Assert.That(controller.PlayPlayer,Is.Not.Null);
            controller.ExitPlay();
            yield return new ExitPlayMode();
        }

        [Test]
        public void RecommendedWorkshopOptionsHaveStableIdsAndSprites()
        {
            var categories=AssetDatabase.FindAssets("t:AppearanceCategory",new[]{"Assets/CreaJuegoPacks/MundoMisterioso/Categorías"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<AppearanceCategory>(AssetDatabase.GUIDToAssetPath(g))).Where(c=>c!=null).ToArray();
            Assert.That(categories.Single(c=>c.kind==ItemKind.Background).Find("platformer-sky-day")?.Preview,Is.Not.Null);
            Assert.That(categories.Single(c=>c.kind==ItemKind.Platform).Find("platformer-stone-tile")?.Preview,Is.Not.Null);
            Assert.That(categories.Single(c=>c.kind==ItemKind.MovingPlatform).Find("plains-moving-ground")?.Preview,Is.Not.Null);
            Assert.That(categories.Single(c=>c.kind==ItemKind.Hazard).Find("plains-spikes")?.Preview,Is.Not.Null);
            Assert.That(categories.Single(c=>c.kind==ItemKind.Decoration).Find("platformer-tree")?.Preview,Is.Not.Null);
        }
    }
}