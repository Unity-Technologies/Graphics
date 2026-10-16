using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // UUM-152952: undoing a Casting Source pick on a ShadowCaster2D reverted m_ShadowCastingSource but left
    // the selection inside m_SelectionSources on the picked entry. The dropdown kept showing the pick while
    // the rest of the inspector drew the fields of the source it had been reverted to.
    class ShadowCaster2DCastingSourceUndoTests
    {
        GameObject m_GameObject;
        GameObject m_SecondGameObject;
        Texture2D m_Texture;
        Sprite m_Sprite;

        [SetUp]
        public void SetUp()
        {
            m_Texture = new Texture2D(4, 4);
            m_Sprite = Sprite.Create(m_Texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            m_GameObject = new GameObject("ShadowCaster2DCastingSourceUndoTests");
            m_GameObject.AddComponent<SpriteRenderer>().sprite = m_Sprite;
            m_SecondGameObject = new GameObject("ShadowCaster2DCastingSourceUndoTests (2)");
            m_SecondGameObject.AddComponent<SpriteRenderer>().sprite = m_Sprite;
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            Object.DestroyImmediate(m_GameObject);
            Object.DestroyImmediate(m_SecondGameObject);
            Object.DestroyImmediate(m_Sprite);
            Object.DestroyImmediate(m_Texture);
        }

        // Does what ShadowCaster2DEditor.OnInspectorGUI does before it draws the Casting Source dropdown.
        static void SetInspectorAdditionalSources(ShadowCaster2D shadowCaster)
        {
            var additionalSources = new List<SelectionSource> { new Shadow2DSource_ShapeEditor(new GUIContent("Shape Editor"), int.MinValue) };
            var serializedObject = new SerializedObject(shadowCaster);
            Shadow2DProviderSources.SetAdditionalSources(serializedObject.FindProperty("m_SelectionSources"), additionalSources);
        }

        static int GetSelectedHashCode(ShadowCaster2D shadowCaster)
        {
            var serializedObject = new SerializedObject(shadowCaster);
            return serializedObject.FindProperty("m_SelectionSources.m_SelectedHashCode").intValue;
        }

        // Picks an entry in the Casting Source dropdown, as the property drawer does.
        static void PickCastingSource(ShadowCaster2D shadowCaster, string entry)
        {
            PickCastingSource(new[] { shadowCaster }, entry);
        }

        // Picks an entry in the Casting Source dropdown with every caster in shadowCasters selected.
        static void PickCastingSource(ShadowCaster2D[] shadowCasters, string entry)
        {
            var drawer = new Shadow2DProviderSources_PropertyDrawer();
            var targets = (Object[])shadowCasters;
            var guiContent = new List<GUIContent[]>();
            var commonContent = drawer.CollectTargetSources(targets, guiContent, out _, out _);
            var picked = commonContent.Find(content => content.text == entry);
            Assert.That(picked, Is.Not.Null, $"The Casting Source dropdown has no \"{entry}\" entry.");
            drawer.ApplySelection(targets, guiContent, picked);
            Undo.IncrementCurrentGroup();
        }

        [Test]
        public void UndoCastingSourcePick_RevertsSelectedSource()
        {
            var shadowCaster = m_GameObject.AddComponent<ShadowCaster2D>();
            SetInspectorAdditionalSources(shadowCaster);
            PickCastingSource(shadowCaster, nameof(SpriteRenderer));

            var initialCastingSource = shadowCaster.shadowCastingSource;
            var initialHashCode = GetSelectedHashCode(shadowCaster);
            Assert.That(initialCastingSource, Is.EqualTo(ShadowCaster2D.ShadowCastingSources.ShapeProvider), "Picking SpriteRenderer did not set the casting source.");
            Assert.That(initialHashCode, Is.Not.EqualTo(0), "Picking SpriteRenderer did not set the selected source.");

            PickCastingSource(shadowCaster, "Shape Editor");

            var pickedHashCode = GetSelectedHashCode(shadowCaster);
            Assert.That(shadowCaster.shadowCastingSource, Is.EqualTo(ShadowCaster2D.ShadowCastingSources.ShapeEditor), "Picking Shape Editor did not set the casting source.");
            Assert.That(pickedHashCode, Is.Not.EqualTo(initialHashCode), "Picking Shape Editor did not change the selected source.");

            Undo.PerformUndo();

            Assert.AreEqual(initialCastingSource, shadowCaster.shadowCastingSource, "Undo did not revert the casting source.");
            Assert.AreEqual(initialHashCode, GetSelectedHashCode(shadowCaster), "Undo reverted the casting source but not the source selected in the Casting Source dropdown.");

            Undo.PerformRedo();

            Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeEditor, shadowCaster.shadowCastingSource, "Redo did not restore the casting source.");
            Assert.AreEqual(pickedHashCode, GetSelectedHashCode(shadowCaster), "Redo restored the casting source but not the source selected in the Casting Source dropdown.");
        }

        // The drawer applies a pick to each selected caster through that caster's own SerializedObject, so
        // each one has to be recorded for Undo, not just the first.
        [Test]
        public void UndoCastingSourcePick_WithMultipleCastersSelected_RevertsEverySelectedSource()
        {
            var shadowCasters = new[]
            {
                m_GameObject.AddComponent<ShadowCaster2D>(),
                m_SecondGameObject.AddComponent<ShadowCaster2D>(),
            };
            foreach (var shadowCaster in shadowCasters)
                SetInspectorAdditionalSources(shadowCaster);
            PickCastingSource(shadowCasters, nameof(SpriteRenderer));

            var initialHashCodes = new int[shadowCasters.Length];
            for (int i = 0; i < shadowCasters.Length; i++)
            {
                initialHashCodes[i] = GetSelectedHashCode(shadowCasters[i]);
                Assert.That(shadowCasters[i].shadowCastingSource, Is.EqualTo(ShadowCaster2D.ShadowCastingSources.ShapeProvider), $"Picking SpriteRenderer did not set the casting source of caster {i}.");
                Assert.That(initialHashCodes[i], Is.Not.EqualTo(0), $"Picking SpriteRenderer did not set the selected source of caster {i}.");
            }

            PickCastingSource(shadowCasters, "Shape Editor");

            for (int i = 0; i < shadowCasters.Length; i++)
                Assert.That(shadowCasters[i].shadowCastingSource, Is.EqualTo(ShadowCaster2D.ShadowCastingSources.ShapeEditor), $"Picking Shape Editor did not set the casting source of caster {i}.");

            Undo.PerformUndo();

            for (int i = 0; i < shadowCasters.Length; i++)
            {
                Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeProvider, shadowCasters[i].shadowCastingSource, $"Undo did not revert the casting source of caster {i}.");
                Assert.AreEqual(initialHashCodes[i], GetSelectedHashCode(shadowCasters[i]), $"Undo reverted the casting source of caster {i} but not its selected source.");
            }
        }
    }
}
