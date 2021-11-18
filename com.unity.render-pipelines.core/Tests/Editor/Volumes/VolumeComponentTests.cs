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
        [HideInInspector]
        [VolumeComponentMenuForRenderPipeline("Tests/No Additional", typeof(RenderPipeline))]
        class VolumeComponentNoAdditionalAttributes : VolumeComponent
        {
            public MinFloatParameter parameter = new MinFloatParameter(0f, 0f);
        }

        [HideInInspector]
        [VolumeComponentMenuForRenderPipeline("Tests/All Additional", typeof(RenderPipeline))]
        class VolumeComponentAllAdditionalAttributes : VolumeComponent
        {
            [AdditionalProperty]
            public MinFloatParameter parameter1 = new MinFloatParameter(0f, 0f);

            [AdditionalProperty]
            public FloatParameter parameter2 = new MinFloatParameter(0f, 0f);
        }

        [HideInInspector]
        [VolumeComponentMenuForRenderPipeline("Tests/Mixed Additional", typeof(RenderPipeline))]
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

        #region Decorators Handling Test

        [HideInInspector]
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

        [Test]
        public void TestSupportedOnAvoidedIfHideInInspector()
        {
            Type[] types = new[]
            {
                typeof(VolumeComponentNoAdditionalAttributes),
                typeof(VolumeComponentAllAdditionalAttributes),
                typeof(VolumeComponentMixedAdditionalAttributes)
            };

            Type volumeComponentProvider = ReflectionUtils.FindTypeByName("UnityEditor.Rendering.VolumeComponentProvider");
            var volumeComponents = volumeComponentProvider.InvokeStatic("FilterVolumeComponentTypes",
                types, typeof(RenderPipeline)) as List<(string, Type)>;

            Assert.NotNull(volumeComponents);
            Assert.False(volumeComponents.Any());
        }

        static private void TestAnimationCurveInterp(AnimationCurve lhsCurve, AnimationCurve rhsCurve, float t, float startTime, float endTime, int numSteps, float eps, bool debugPrint)
        {
            AnimationCurve midCurve = new AnimationCurve(lhsCurve.keys);
            KeyframeUtility.InterpAnimationCurve(ref midCurve, rhsCurve, t);

            for (int i = 0; i <= numSteps; i++)
            {
                float timeT = ((float)i) / ((float)numSteps);
                float currTime = Mathf.Lerp(startTime, endTime, timeT);

                float lhsVal = lhsCurve.Evaluate(currTime);
                float rhsVal = rhsCurve.Evaluate(currTime);

                float expectedVal = Mathf.Lerp(lhsVal, rhsVal, t);

                float actualVal = midCurve.Evaluate(currTime);

                float offset = actualVal - expectedVal;
                if (debugPrint)
                {
                    Debug.Log(i.ToString() + ": " + offset.ToString());
                }

                Assert.IsTrue(Mathf.Abs(offset) < eps);
            }
        }

        [Test]
        public void RenderInterpolateAnimationCurve()
        {
            AnimationCurve testCurve0 = new AnimationCurve();
            testCurve0.AddKey(new Keyframe(0.0f, 3.0f, 2.0f, 2.0f));
            testCurve0.AddKey(new Keyframe(4.0f, 2.0f, -1.0f, -1.0f));
            testCurve0.AddKey(new Keyframe(7.0f, 2.6f, -1.0f, -1.0f));

            AnimationCurve testCurve1 = new AnimationCurve();
            testCurve1.AddKey(new Keyframe(-1.0f, 3.0f, 2.0f, 2.0f));
            testCurve1.AddKey(new Keyframe(4.0f, 2.0f, 3.0f, 3.0f));
            testCurve1.AddKey(new Keyframe(5.0f, 2.6f, 0.0f, 0.0f));
            testCurve1.AddKey(new Keyframe(9.0f, 2.6f, -5.0f, -5.0f));

            // Test the same positions as curve 0 but different values and tangents
            AnimationCurve testCurve2 = new AnimationCurve();
            testCurve2.AddKey(new Keyframe(0.0f, 1.0f, -1.0f, 3.0f));
            testCurve2.AddKey(new Keyframe(4.0f, 6.0f, -9.0f, -2.0f));
            testCurve2.AddKey(new Keyframe(7.0f, 5.2f, -3.0f, -4.0f));

            // Test the case where two curves have no overlap
            AnimationCurve testCurve3 = new AnimationCurve();
            testCurve3.AddKey(new Keyframe(11.0f, 1.0f, -1.0f, 3.0f));
            testCurve3.AddKey(new Keyframe(14.0f, 6.0f, -9.0f, -2.0f));
            testCurve3.AddKey(new Keyframe(17.0f, 5.2f, -3.0f, -4.0f));

            TestAnimationCurveInterp(testCurve0, testCurve1, 0.5f, -2.0f, 10.0f, 100, 1e-5f, false);
            TestAnimationCurveInterp(testCurve1, testCurve2, 0.5f, -2.0f, 10.0f, 100, 1e-5f, false);
            TestAnimationCurveInterp(testCurve0, testCurve2, 0.5f, -2.0f, 10.0f, 100, 1e-5f, false);
            TestAnimationCurveInterp(testCurve0, testCurve3, 0.5f, -2.0f, 25.0f, 100, 1e-5f, false);
        }


    }
}
