using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // UUM-158120: a ShadowCaster2D added before its Sprite Renderer kept casting the Shape Editor path,
    // while the Casting Source dropdown read "Sprite Renderer". Awake picked ShapeEditor on a GameObject
    // with no renderer but left m_SelectionSources.selectedHashCode at 0. Once the Sprite Renderer was
    // added, the drawer found no entry for hash 0 and fell back to entry 0, which is "Sprite Renderer".
    class ShadowCaster2DCastingSourceTests
    {
        GameObject m_GameObject;
        Texture2D m_Texture;
        Sprite m_Sprite;

        [SetUp]
        public void SetUp()
        {
            m_GameObject = new GameObject("ShadowCaster2DCastingSourceTests");
            m_Texture = new Texture2D(4, 4);
            m_Sprite = Sprite.Create(m_Texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_GameObject);
            Object.DestroyImmediate(m_Sprite);
            Object.DestroyImmediate(m_Texture);
        }

        // Asks the real Casting Source drawer what it shows, with the Shape Editor entry the inspector adds.
        static string CastingSourceShownInInspector(ShadowCaster2D caster)
        {
            var selectionSources = new SerializedObject(caster).FindProperty("m_SelectionSources");
            var additionalSources = new List<SelectionSource>
            {
                new Shadow2DSource_ShapeEditor(new GUIContent("Shape Editor"), int.MinValue),
            };
            Shadow2DProviderSources.SetAdditionalSources(selectionSources, additionalSources);

            var drawer = new Shadow2DProviderSources_PropertyDrawer();
            drawer.CollectTargetSources(new Object[] { caster }, new List<GUIContent[]>(), out var shown, out _);
            return shown?.text;
        }

        [Test]
        public void CasterAddedBeforeSpriteRenderer_DropdownShowsTheSourceInUse()
        {
            var caster = m_GameObject.AddComponent<ShadowCaster2D>();
            m_GameObject.AddComponent<SpriteRenderer>().sprite = m_Sprite;

            Assert.That(caster.shadowCastingSource, Is.EqualTo(ShadowCaster2D.ShadowCastingSources.ShapeEditor),
                "With no renderer at Awake, the caster should fall back to the Shape Editor.");

            Assert.AreEqual("Shape Editor", CastingSourceShownInInspector(caster),
                "The Casting Source dropdown does not show the source the shadow is built from.");
        }

        [Test]
        public void CasterWithNoRenderer_StoresShapeEditorHash()
        {
            var caster = m_GameObject.AddComponent<ShadowCaster2D>();

            Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeEditor, caster.shadowCastingSource);
            Assert.AreEqual((int)ShadowCaster2D.ShadowCastingSources.ShapeEditor, caster.m_SelectionSources.selectedHashCode);
            Assert.AreEqual("Shape Editor", CastingSourceShownInInspector(caster));
        }

        [Test]
        public void CasterAddedAfterSpriteRenderer_StoresProviderHashAndShowsSpriteRenderer()
        {
            m_GameObject.AddComponent<SpriteRenderer>().sprite = m_Sprite;
            var caster = m_GameObject.AddComponent<ShadowCaster2D>();

            Assert.That(caster.shadowCastingSource, Is.EqualTo(ShadowCaster2D.ShadowCastingSources.ShapeProvider),
                "With a renderer at Awake, the caster should use it as the shape provider.");

            Assert.AreEqual(LightUtility.ProviderToHash(caster.shadowShape2DProvider, caster.shadowShape2DComponent),
                caster.m_SelectionSources.selectedHashCode);
            Assert.AreEqual("SpriteRenderer", CastingSourceShownInInspector(caster));
        }

        [Test]
        public void ShapeEditorCasterSavedWithoutHash_IsRepairedOnLoad()
        {
            var caster = m_GameObject.AddComponent<ShadowCaster2D>();
            m_GameObject.AddComponent<SpriteRenderer>().sprite = m_Sprite;

            // Recreate what the old Awake saved: a Version_8 Shape Editor caster with a selected hash of 0.
            var serializedCaster = new SerializedObject(caster);
            serializedCaster.FindProperty("m_ComponentVersion").intValue = 8;
            serializedCaster.FindProperty("m_SelectionSources.m_SelectedHashCode").intValue = 0;
            serializedCaster.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual((int)ShadowCaster2D.ShadowCastingSources.ShapeEditor, caster.m_SelectionSources.selectedHashCode);
            Assert.AreEqual("Shape Editor", CastingSourceShownInInspector(caster));
        }

        [Test]
        public void ShapeProviderCasterSavedWithoutHash_IsRepairedOnLoad()
        {
            m_GameObject.AddComponent<SpriteRenderer>().sprite = m_Sprite;
            var caster = m_GameObject.AddComponent<ShadowCaster2D>();
            int providerHash = LightUtility.ProviderToHash(caster.shadowShape2DProvider, caster.shadowShape2DComponent);

            // Recreate what the old Awake saved for a provider caster: a Version_8 caster with a selected hash of 0.
            var serializedCaster = new SerializedObject(caster);
            serializedCaster.FindProperty("m_ComponentVersion").intValue = 8;
            serializedCaster.FindProperty("m_SelectionSources.m_SelectedHashCode").intValue = 0;
            serializedCaster.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeProvider, caster.shadowCastingSource);
            Assert.AreEqual(providerHash, caster.m_SelectionSources.selectedHashCode);
        }

        [Test]
        public void CasterSavedWithNonZeroHash_KeepsItsHashOnLoad()
        {
            m_GameObject.AddComponent<SpriteRenderer>().sprite = m_Sprite;
            var caster = m_GameObject.AddComponent<ShadowCaster2D>();
            const int storedHash = 12345;

            // The Version_9 repair only touches a hash of 0, so any other stored hash must survive it.
            var serializedCaster = new SerializedObject(caster);
            serializedCaster.FindProperty("m_ComponentVersion").intValue = 8;
            serializedCaster.FindProperty("m_SelectionSources.m_SelectedHashCode").intValue = storedHash;
            serializedCaster.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(storedHash, caster.m_SelectionSources.selectedHashCode);
        }
    }
}
