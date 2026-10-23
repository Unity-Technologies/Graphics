using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // UUM-152952: the Light Type dropdown wrote its selection inside m_SelectionSources without recording it
    // for Undo. Undoing a pick reverted m_LightType but left the dropdown selection on the picked entry.
    class Light2DLightTypeUndoTests
    {
        GameObject m_GameObject;

        [SetUp]
        public void SetUp()
        {
            m_GameObject = new GameObject("Light2DLightTypeUndoTests");
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            Object.DestroyImmediate(m_GameObject);
        }

        static int GetSelectedHashCode(Light2D light)
        {
            var serializedObject = new SerializedObject(light);
            return serializedObject.FindProperty("m_SelectionSources.m_SelectedHashCode").intValue;
        }

        static int IndexOfSourceName(GUIContent[] sourceNames, string wanted)
        {
            for (int i = 0; i < sourceNames.Length; i++)
            {
                if (sourceNames[i].text == wanted)
                    return i;
            }

            return -1;
        }

        // Picks an entry in the Light Type dropdown, as DrawLightTypePopup does.
        static void PickLightType(Light2D light, string entry)
        {
            var serializedObject = new SerializedObject(light);
            var selectionSources = serializedObject.FindProperty("m_SelectionSources");
            var sources = selectionSources.boxedValue as Light2DProviderSources;

            // The dropdown is built from the sources on the GameObject plus the built-in light types.
            Light2DEditorUtility.RefreshLightSources(light, sources);

            var index = IndexOfSourceName(sources.GetSourceNames(), entry);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), $"The Light Type dropdown has no \"{entry}\" entry.");

            Light2DProviderSources.ApplySelectionFromIndex(selectionSources, index, Light2DEditorUtility.k_ChangeLightTypeUndoName);
            Undo.IncrementCurrentGroup();
        }

        [Test]
        public void UndoAndRedoLightTypePick_KeepSelectedSourceInSync()
        {
            var light = m_GameObject.AddComponent<Light2D>();
            PickLightType(light, "Spot");

            var initialHashCode = GetSelectedHashCode(light);
            Assert.That(light.lightType, Is.EqualTo(Light2D.LightType.Point), "Picking Spot did not set the light type.");

            PickLightType(light, "Sprite");

            var pickedHashCode = GetSelectedHashCode(light);
            Assert.That(light.lightType, Is.EqualTo(Light2D.LightType.Sprite), "Picking Sprite did not set the light type.");
            Assert.That(pickedHashCode, Is.Not.EqualTo(initialHashCode), "Picking Sprite did not change the selected source.");

            Undo.PerformUndo();

            Assert.AreEqual(Light2D.LightType.Point, light.lightType, "Undo did not revert the light type.");
            Assert.AreEqual(initialHashCode, GetSelectedHashCode(light), "Undo reverted the light type but not the source selected in the Light Type dropdown.");

            Undo.PerformRedo();

            Assert.AreEqual(Light2D.LightType.Sprite, light.lightType, "Redo did not restore the light type.");
            Assert.AreEqual(pickedHashCode, GetSelectedHashCode(light), "Redo restored the light type but not the source selected in the Light Type dropdown.");
        }
    }
}
