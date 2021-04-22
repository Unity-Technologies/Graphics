using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Tests
{
    public class VolumeComponentEditorTests
    {
        class VolumeComponentNoAdditionalAttributes : VolumeComponent
        {
            public MinFloatParameter parameter = new MinFloatParameter(0f, 0f);
        }

        class VolumeComponentAllAdditionalAttributes : VolumeComponent
        {
            [AdditionalProperty]
            public MinFloatParameter parameter1 = new MinFloatParameter(0f, 0f);

            [AdditionalProperty]
            public FloatParameter parameter2 = new MinFloatParameter(0f, 0f);
        }

        class VolumeComponentMixedAdditionalAttributes : VolumeComponent
        {
            public MinFloatParameter parameter1 = new MinFloatParameter(0f, 0f);

            [AdditionalProperty]
            public FloatParameter parameter2 = new MinFloatParameter(0f, 0f);

            public MinFloatParameter parameter3 = new MinFloatParameter(0f, 0f);

            [AdditionalProperty]
            public FloatParameter parameter4 = new MinFloatParameter(0f, 0f);
        }

        [Test]
        public void TestOverridesChanges()
        {
            var component = ScriptableObject.CreateInstance<VolumeComponentMixedAdditionalAttributes>();
            var editor = (VolumeComponentEditor)Activator.CreateInstance(typeof(VolumeComponentEditor));
            editor.Invoke("Init", component, null);

            component.SetAllOverridesTo(false);
            bool allOverridesState = (bool)editor.Invoke("AreAllOverridesTo", false);
            Assert.True(allOverridesState);

            component.SetAllOverridesTo(true);

            // Was the change correct?
            allOverridesState = (bool)editor.Invoke("AreAllOverridesTo", true);
            Assert.True(allOverridesState);

            // Enable the advance mode on the editor
            component.SetField("m_AdvancedMode", true);

            // Everything is false
            component.SetAllOverridesTo(false);

            // Disable the advance mode on the editor
            component.SetField("m_AdvancedMode", false);

            // Now just set to true the overrides of non additional properties
            editor.Invoke("SetOverridesTo", true);

            // Check that the non additional properties must be false
            allOverridesState = (bool)editor.Invoke("AreAllOverridesTo", true);
            Assert.False(allOverridesState);

            ScriptableObject.DestroyImmediate(component);
        }

        static TestCaseData[] s_AdditionalAttributesTestCaseDatas =
        {
            new TestCaseData(typeof(VolumeComponentNoAdditionalAttributes))
                .Returns(Array.Empty<string>())
                .SetName("VolumeComponentNoAdditionalAttributes"),
            new TestCaseData(typeof(VolumeComponentAllAdditionalAttributes))
                .Returns(new string[2] {"parameter1", "parameter2"})
                .SetName("VolumeComponentAllAdditionalAttributes"),
            new TestCaseData(typeof(VolumeComponentMixedAdditionalAttributes))
                .Returns(new string[2] {"parameter2", "parameter4"})
                .SetName("VolumeComponentMixedAdditionalAttributes"),
        };

        [Test, TestCaseSource(nameof(s_AdditionalAttributesTestCaseDatas))]
        public string[] AdditionalProperties(Type volumeComponentType)
        {
            var component = (VolumeComponent)ScriptableObject.CreateInstance(volumeComponentType);
            var editor = (VolumeComponentEditor)Activator.CreateInstance(typeof(VolumeComponentEditor));
            editor.Invoke("Init", component, null);

            var fields = component
                .GetFields()
                .Where(f => f.GetCustomAttribute<AdditionalPropertyAttribute>() != null)
                .Select(f => f.Name)
                .ToArray();

            var notAdditionalParameters = editor.GetField("m_VolumeNotAdditionalParameters") as List<VolumeParameter>;
            Assert.True(fields.Count() + notAdditionalParameters.Count == component.parameters.Count);

            ScriptableObject.DestroyImmediate(component);

            return fields;
        }

        #region Decorators Handling Test

        class VolumeComponentDecorators : VolumeComponent
        {
            [Tooltip("Increase to make the noise texture appear bigger and less")]
            public FloatParameter _NoiseTileSize = new FloatParameter(25.0f);

            [InspectorName("Color")]
            public ColorParameter _FogColor = new ColorParameter(Color.grey);

            [InspectorName("Size and occurrence"), Tooltip("Increase to make patches SMALLER, and frequent")]
            public ClampedFloatParameter _HighNoiseSpaceFreq = new ClampedFloatParameter(0.1f, 0.1f, 1f);
        }

        readonly (string displayName, string tooltip)[] k_ExpectedResults =
        {
            (string.Empty, "Increase to make the noise texture appear bigger and less"),
            ("Color", string.Empty),
            ("Size and occurrence", "Increase to make patches SMALLER, and frequent")
        };

        [Test]
        public void TestHandleParameterDecorators()
        {
            var component = ScriptableObject.CreateInstance<VolumeComponentDecorators>();
            var editor = (VolumeComponentEditor)Activator.CreateInstance(typeof(VolumeComponentEditor));
            editor.Invoke("Init", component, null);

            var parameters =
                editor.GetField("m_Parameters") as List<(GUIContent displayName, int displayOrder,
                    SerializedDataParameter param)>;

            Assert.True(parameters != null && parameters.Count() == k_ExpectedResults.Count());

            for (int i = 0; i < k_ExpectedResults.Count(); ++i)
            {
                var property = parameters[i].param;
                var title = new GUIContent(parameters[i].displayName);

                editor.Invoke("HandleDecorators", property, title);

                Assert.True(k_ExpectedResults[i].displayName == title.text);
                Assert.True(k_ExpectedResults[i].tooltip == title.tooltip);
            }

            ScriptableObject.DestroyImmediate(component);
        }

        #endregion
    }
}
