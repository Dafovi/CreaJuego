using System;
using System.Collections;
using System.Linq;
using CreaJuego.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CreaJuego.Starter.Tests
{
    public sealed class EducationalLayerTests
    {
        [SetUp] public void Setup() => EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        [TearDown] public void Cleanup()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<CreaJuegoWindow>()) window.Close();
            Undo.ClearAll(); EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        }

        [Test] public void WorkshopPublishesFiveP0AndValidatedEnemyWithDistinctIcons()
        {
            var catalog = ItemService.WorkshopCatalog();
            Assert.That(catalog.Where(d => d.kind != ItemKind.Enemy).Select(d => d.kind), Is.EquivalentTo(new[] { ItemKind.Player, ItemKind.Platform, ItemKind.Prize, ItemKind.Hazard, ItemKind.Goal }));
            Assert.That(catalog.All(d => d.icon != null), Is.True);
            Assert.That(catalog.Select(d => d.icon).Distinct().Count(), Is.EqualTo(6));
            Assert.That(ItemService.Catalog().Length, Is.EqualTo(8), "Experimental content is preserved");
        }

        [Test] public void SinglePlayerAndAssetEditingAreGuarded()
        {
            var d = ItemService.Catalog().Single(i => i.kind == ItemKind.Player);
            var player = Object.FindObjectsByType<GameItem>().Single(i => i.definition == d);
            Assert.Throws<InvalidOperationException>(() => ItemService.Create(d, Vector3.zero));
            Assert.Throws<InvalidOperationException>(() => ItemService.Duplicate(player));
            Assert.Throws<InvalidOperationException>(() => ItemService.Delete(d.prefab.GetComponent<GameItem>()));
            ItemService.Delete(player);
            Assert.That(ItemService.CanCreate(d), Is.True);
            Undo.PerformUndo();
            Assert.That(ItemService.CanCreate(d), Is.False);
        }

        [Test] public void DuplicateAndDeletePreserveValuesPrefabAndUndo()
        {
            var source = Object.FindObjectsByType<GameItem>().First(i => i.definition.kind == ItemKind.Prize);
            using (var serialized = new SerializedObject(source))
            {
                serialized.FindProperty(nameof(GameItem.points)).intValue = 17;
                serialized.ApplyModifiedProperties();
            }
            Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
            var copy = ItemService.Duplicate(source);
            Assert.That(copy.points, Is.EqualTo(17));
            Assert.That(copy.definition, Is.EqualTo(source.definition));
            Assert.That(copy.transform.position, Is.Not.EqualTo(source.transform.position));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(copy), Is.True);
            Assert.That(Selection.activeGameObject, Is.EqualTo(copy.gameObject));
            string name = copy.name;
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(copy == null, Is.True);
            Undo.PerformRedo();
            copy = Object.FindObjectsByType<GameItem>().Single(i => i.name == name);
            Assert.That(copy.points, Is.EqualTo(17));
            Undo.IncrementCurrentGroup(); ItemService.Delete(copy); Undo.PerformUndo();
            Assert.That(Object.FindObjectsByType<GameItem>().Any(i => i.name == name && i.points == 17), Is.True);
        }

        [UnityTest] public IEnumerator EducationalControlsBindAndClampWithoutTechnicalFields()
        {
            var window = EditorWindow.GetWindow<CreaJuegoWindow>(); window.CreateGUI();
            var prize = Object.FindObjectsByType<GameItem>().First(i => i.definition.kind == ItemKind.Prize);
            Selection.activeGameObject = prize.gameObject;
            yield return null;
            Assert.That(window.rootVisualElement.Q<Button>("crear-movil"), Is.Null);
            Assert.That(window.rootVisualElement.Q<Button>("crear-enemigo"), Is.Not.Null);
            Assert.That(window.rootVisualElement.Q<Image>("icono-premio").sprite, Is.EqualTo(prize.definition.icon));
            var number = window.rootVisualElement.Q<IntegerField>("propiedad-points");
            number.value = 500;
            yield return null;
            Assert.That(prize.points, Is.EqualTo(100));
            number.value = -10;
            yield return null;
            Assert.That(prize.points, Is.EqualTo(1));
            Assert.That(window.rootVisualElement.Q<VisualElement>("propiedad-speed"), Is.Null);
            Assert.That(window.rootVisualElement.Q<Button>("duplicar").enabledSelf, Is.True);
            Selection.activeGameObject = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Player).gameObject;
            yield return null;
            Assert.That(window.rootVisualElement.Q<Button>("duplicar").enabledSelf, Is.False);
        }

        [UnityTest] public IEnumerator DescriptorBooleanVisibilityUpdatesWhenToggleChanges()
        {
            var hazard = Object.FindObjectsByType<GameItem>().Single(i => i.definition.kind == ItemKind.Hazard);
            // A transient descriptor tests the mechanism without changing the content pack.
            var definition = Object.Instantiate(hazard.definition);
            try
            {
                definition.properties.Single(p => p.path == nameof(GameItem.damage)).visibleWhen = nameof(GameItem.disappear);
                hazard.definition = definition;
                Selection.activeGameObject = hazard.gameObject;
                var window = EditorWindow.GetWindow<CreaJuegoWindow>(); window.CreateGUI();
                yield return null;
                var toggle = window.rootVisualElement.Q<Toggle>("propiedad-disappear");
                toggle.value = false;
                yield return null; yield return null;
                Assert.That(window.rootVisualElement.Q<VisualElement>("fila-damage").style.display.value, Is.EqualTo(DisplayStyle.None));
                toggle.value = true;
                yield return null; yield return null;
                Assert.That(window.rootVisualElement.Q<VisualElement>("fila-damage").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            }
            finally { Object.DestroyImmediate(definition); }
        }
    }
}

