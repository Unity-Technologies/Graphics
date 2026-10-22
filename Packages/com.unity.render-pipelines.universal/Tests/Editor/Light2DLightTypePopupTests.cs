using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.Tests
{
    /// <summary>
    /// Covers the Light Type dropdown that the Inspector and the Light Explorer share
    /// (Light2DEditorUtility.DrawLightTypePopup).
    /// </summary>
    [TestFixture]
    class Light2DLightTypePopupTests
    {
        const string k_DuplicateGlobalLightError = "More than one global light on layer Default for light blend style index";

        readonly List<GameObject> m_Created = new List<GameObject>();
        readonly List<string> m_Errors = new List<string>();

        Light2D CreateLight(string name, Light2D.LightType lightType)
        {
            var go = new GameObject(name);
            m_Created.Add(go);

            // The type is set after OnEnable has registered the light, so creating it logs nothing.
            var light = go.AddComponent<Light2D>();
            light.lightType = lightType;
            return light;
        }

        int DuplicateGlobalLightErrorCount()
        {
            return m_Errors.FindAll(e => e.StartsWith(k_DuplicateGlobalLightError)).Count;
        }

        void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error)
                m_Errors.Add(condition);
        }

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            m_Errors.Clear();
            Application.logMessageReceived += OnLogMessage;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLogMessage;
            foreach (var go in m_Created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            m_Created.Clear();
        }

        // UUM-144607: picking Global in the Light Explorer's Light Type column must report a second
        // global light on the same sorting layer, as the Inspector does. Only the light that just
        // became Global reports, so one change logs one error for each sorting layer that clashes.
        [Test]
        public void ApplyLightTypeSelection_SecondGlobalLightOnSameLayer_LogsDuplicateGlobalLightError()
        {
            CreateLight("First Light", Light2D.LightType.Global);
            var second = CreateLight("Second Light", Light2D.LightType.Point);

            LogAssert.ignoreFailingMessages = true;
            Light2DEditorUtility.ApplyLightTypeSelection(new SerializedObject(second), new GUIContent("Global"));

            Assert.AreEqual(Light2D.LightType.Global, second.lightType, "The Global selection was not applied.");
            Assert.AreEqual(1, DuplicateGlobalLightErrorCount(),
                "Setting a second light to Global must log the duplicate global light error exactly once.");
        }

        [Test]
        public void ApplyLightTypeSelection_OnlyGlobalLight_LogsNoError()
        {
            var first = CreateLight("First Light", Light2D.LightType.Point);

            LogAssert.ignoreFailingMessages = true;
            Light2DEditorUtility.ApplyLightTypeSelection(new SerializedObject(first), new GUIContent("Global"));

            Assert.AreEqual(Light2D.LightType.Global, first.lightType);
            Assert.AreEqual(0, DuplicateGlobalLightErrorCount());
        }

        // Picking Global again on a light that is already Global is not a new clash.
        [Test]
        public void ApplyLightTypeSelection_AlreadyGlobal_LogsNoError()
        {
            CreateLight("First Light", Light2D.LightType.Global);
            var second = CreateLight("Second Light", Light2D.LightType.Global);

            LogAssert.ignoreFailingMessages = true;
            Light2DEditorUtility.ApplyLightTypeSelection(new SerializedObject(second), new GUIContent("Global"));

            Assert.AreEqual(Light2D.LightType.Global, second.lightType);
            Assert.AreEqual(0, DuplicateGlobalLightErrorCount());
        }

        // A disabled light is not registered, so it cannot clash. It reports when it is enabled.
        // Two other global lights already clash, so only the enabled check keeps this one quiet.
        [Test]
        public void ApplyLightTypeSelection_DisabledLight_LogsNoError()
        {
            CreateLight("First Light", Light2D.LightType.Global);
            CreateLight("Third Light", Light2D.LightType.Global);

            var second = CreateLight("Second Light", Light2D.LightType.Point);
            second.enabled = false;

            LogAssert.ignoreFailingMessages = true;
            Light2DEditorUtility.ApplyLightTypeSelection(new SerializedObject(second), new GUIContent("Global"));

            Assert.AreEqual(Light2D.LightType.Global, second.lightType);
            Assert.AreEqual(0, DuplicateGlobalLightErrorCount());
        }

        // Switching a light away from Global must not report a clash.
        [Test]
        public void ApplyLightTypeSelection_GlobalToSpot_LogsNoError()
        {
            CreateLight("First Light", Light2D.LightType.Global);
            var second = CreateLight("Second Light", Light2D.LightType.Global);

            LogAssert.ignoreFailingMessages = true;
            Light2DEditorUtility.ApplyLightTypeSelection(new SerializedObject(second), new GUIContent("Spot"));

            Assert.AreNotEqual(Light2D.LightType.Global, second.lightType);
            Assert.AreEqual(0, DuplicateGlobalLightErrorCount());
        }
    }
}
