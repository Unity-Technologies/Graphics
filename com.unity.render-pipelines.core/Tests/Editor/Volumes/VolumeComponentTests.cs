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
            editor.showAdditionalProperties = true;

            // Everything is false
            component.SetAllOverridesTo(false);

            // Disable the advance mode on the editor
            editor.showAdditionalProperties = false;

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
    }
}
