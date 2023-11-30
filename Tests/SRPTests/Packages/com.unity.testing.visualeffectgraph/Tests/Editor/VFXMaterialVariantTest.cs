using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXMaterialVariantTest
    {
        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        private static List<(string name, float value)> ExtractMaterialSettings(VFXModel model)
        {
            var materialSettingsWithoutSerialized = new SerializedObject(model);
            var shading = materialSettingsWithoutSerialized.FindProperty("m_Shading");
            var materialSettings = shading.FindPropertyRelative("materialSettings");
            var propertyNames = materialSettings.FindPropertyRelative("m_PropertyNames");
            var propertyValues = materialSettings.FindPropertyRelative("m_PropertyValues");

            var outputList = new List<(string name, float value)>();
            Assert.AreEqual(propertyNames.arraySize, propertyValues.arraySize);

            for (int index = 0; index < propertyNames.arraySize; ++index)
            {
                outputList.Add((
                    propertyNames.GetArrayElementAtIndex(index).stringValue,
                    propertyValues.GetArrayElementAtIndex(index).floatValue));
            }

            return outputList;
        }

        [Test]
        public void Migration_Material_Settings()
        {
            var packagePath = @"Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/VFXMaterialVariant_Upgrade_Asset_";
            var vfxPath = VFXTestCommon.tempBasePath + "/Upgradable_VFX_";

#if VFX_TESTS_HAS_URP
            packagePath += "URP.unitypackage";
            vfxPath += "URP.vfx";
#endif

#if VFX_TESTS_HAS_HDRP
            packagePath += "HDRP.unitypackage";
            vfxPath += "HDRP.vfx";
#endif
            AssetDatabase.ImportPackageImmediately(packagePath);
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            var vfxGraph = vfxAsset.GetOrCreateResource().GetOrCreateGraph();

            var withoutOverride = vfxGraph.children.OfType<VFXAbstractComposedParticleOutput>().FirstOrDefault(o => o.label == "Without override");
            var withOverride = vfxGraph.children.OfType<VFXAbstractComposedParticleOutput>().FirstOrDefault(o => o.label == "With override");

            Assert.IsNotNull(withoutOverride);
            Assert.IsNotNull(withOverride);

            //Functional check
            Assert.AreEqual(true, withoutOverride.isBlendModeOpaque);
            Assert.AreEqual(false, withOverride.isBlendModeOpaque);

            //Serialized data and override check
            var materialSettingsWithout = ExtractMaterialSettings(withoutOverride);
            var materialSettingsWith = ExtractMaterialSettings(withOverride);

            //See UUM-31690, this data could have been saved with inconsistent values
            //Ignoring SRP fields which are impact by this dangling behavior
#if VFX_TESTS_HAS_HDRP
            materialSettingsWithout.RemoveAll(o => o.name == "_ZTestDepthEqualForOpaque");
            materialSettingsWith.RemoveAll(o => o.name == "_ZTestDepthEqualForOpaque");
#endif
#if VFX_TESTS_HAS_URP
            materialSettingsWithout.RemoveAll(o => o.name == "_QueueControl");
            materialSettingsWith.RemoveAll(o => o.name == "_QueueControl");
#endif
            Assert.IsEmpty(materialSettingsWithout);
            Assert.IsNotEmpty(materialSettingsWith);

            List<(string name, float value)> expectedRemainder;

#if VFX_TESTS_HAS_HDRP
            expectedRemainder = new()
            {
                ("_AlphaDstBlend", 1),
                ("_BlendMode", 1),
                ("_DstBlend", 1),
                ("_RenderQueueType", 4),
                ("_SurfaceType", 1),
                ("_ZTestTransparent", 3),
                ("_ZWrite", 0)
            };
#endif

#if VFX_TESTS_HAS_URP
            expectedRemainder = new()
            {
                ("_Blend", 2),
                ("_DstBlend", 1),
                ("_SrcBlend", 5),
                ("_Surface", 1),
                ("_ZWrite", 0)
            };
#endif
            materialSettingsWith.Sort((x, y) => String.CompareOrdinal(x.name, y.name));

            Assert.AreEqual(expectedRemainder.Count, materialSettingsWith.Count);
            for (int i = 0; i < expectedRemainder.Count; ++i)
            {
                Assert.AreEqual(expectedRemainder[i].name, materialSettingsWith[i].name);
                Assert.AreEqual(expectedRemainder[i].value, materialSettingsWith[i].value);
            }
        }

        public struct Check_Material_Override_Behavior_Test_Case
        {
            internal string name;
            internal string initialShaderGraph;
            internal string replacementShaderGraph;

            internal Action<VFXAbstractParticleOutput> initialCheck;
            internal Action<VFXAbstractParticleOutput> afterSwichShaderGraphCheck;
            internal Action<VFXAbstractParticleOutput, Material> injectOverride;

            public override string ToString()
            {
                return name;
            }
        }

        private static Check_Material_Override_Behavior_Test_Case[] s_Check_Material_Override_Behavior_Test_Case = new[]
        {
            new Check_Material_Override_Behavior_Test_Case()
            {
                name = "Surface",
                initialShaderGraph = "SG_Unlit_Alpha",
                replacementShaderGraph = "SG_Unlit_Opaque",

                initialCheck = (output) =>
                {
                    Assert.IsFalse(output.isBlendModeOpaque);
                },

                afterSwichShaderGraphCheck = (output) =>
                {
                    Assert.IsTrue(output.isBlendModeOpaque);
                },

                injectOverride = (output, materialVariant) =>
                {
#if VFX_TESTS_HAS_URP
                    materialVariant.SetFloat("_Surface", 1.0f);
                    materialVariant.SetFloat("_Blend", 666.0f); //Trick to force override
                    materialVariant.SetFloat("_Blend", 0.0f);

                    Assert.IsTrue(materialVariant.IsPropertyOverriden("_Surface"));
                    Assert.IsTrue(materialVariant.IsPropertyOverriden("_Blend"));
#endif

#if VFX_TESTS_HAS_HDRP
                    materialVariant.SetFloat("_SurfaceType", 1.0f);
                    materialVariant.SetFloat("_BlendMode", 666.0f); //Trick to force override
                    materialVariant.SetFloat("_BlendMode", 0.0f);

                    Assert.IsTrue(materialVariant.IsPropertyOverriden("_SurfaceType"));
                    Assert.IsTrue(materialVariant.IsPropertyOverriden("_BlendMode"));
#endif

                    var materialSettings = new VFXMaterialSerializedSettings();
                    materialSettings.SyncFromMaterial(materialVariant);
                    output.SetSettingValue("materialSettings", materialSettings);
                    Assert.IsFalse(output.isBlendModeOpaque);
                }
            },

#if VFX_TESTS_HAS_URP
            //Specific URP
            new Check_Material_Override_Behavior_Test_Case()
            {
                name = "Shadow",
                initialShaderGraph = "SG_Unlit_Shadow_Off",
                replacementShaderGraph = "SG_Unlit_Shadow_On",

                initialCheck = (output) =>
                {
                    Assert.IsFalse(output.hasShadowCasting);
                },

                afterSwichShaderGraphCheck = (output) =>
                {
                    Assert.IsTrue(output.hasShadowCasting);
                },

                injectOverride = (output, materialVariant) =>
                {
                    materialVariant.SetFloat("_CastShadows", 0.0f);
                    Assert.IsTrue(materialVariant.IsPropertyOverriden("_CastShadows"));

                    var materialSettings = new VFXMaterialSerializedSettings();
                    materialSettings.SyncFromMaterial(materialVariant);
                    output.SetSettingValue("materialSettings", materialSettings);
                    Assert.IsFalse(output.hasShadowCasting);
                }
            }
#endif
        };

        //Cover wrong declaration preventing material settings to be transferred with ConvertContext
        //See: https://docs.google.com/document/d/1roK6yOrc8AP5E6pSYIIK4Ep_CSGc7DovR9kW2Kr32VE/edit?disco=AAAAtjOzCYA
        [Test]
        public void Verify_MaterialSettings_Visibility()
        {
            var particleOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
            particleOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
            particleOutput.SetSettingValue("m_Shading", new ParticleShadingShaderGraph());
            Assert.IsNotNull(particleOutput.GetSetting("materialSettings"));

            var allSettings = particleOutput
                .GetSettings(true, VFXSettingAttribute.VisibleFlags.Default)
                .Select(o => o.name)
                .ToArray();
            Assert.Contains("materialSettings", allSettings);
        }

        [Test]
        public void Check_Material_Override_Behavior([ValueSource("s_Check_Material_Override_Behavior_Test_Case")] Check_Material_Override_Behavior_Test_Case testCase)
        {
            var baseDataPath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/";
            var shaderGraph = VFXTestCommon.CopyTemporaryShaderGraph(baseDataPath + testCase.initialShaderGraph + ".shadergraph");

            var graph = VFXTestCommon.MakeTemporaryGraph();
            //Create VFXAsset
            {
                var particleOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
                particleOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());

                particleOutput.SetSettingValue("shaderGraph", shaderGraph);
                graph.AddChild(particleOutput);

                var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
                contextInitialize.LinkTo(particleOutput);
                graph.AddChild(contextInitialize);

                var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                spawner.LinkTo(contextInitialize);
                graph.AddChild(spawner);

                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            }

            //Basic First check
            {
                var output = graph.children.OfType<VFXAbstractParticleOutput>().First();
                testCase.initialCheck(output);
            }

            //Switch SG Content to Opaque
            {
                var content = File.ReadAllBytes(baseDataPath + testCase.replacementShaderGraph + ".shadergraph");
                Assert.AreNotEqual(0u, content.Length);
                var shaderGraphPath = AssetDatabase.GetAssetPath(shaderGraph);
                File.WriteAllBytes(shaderGraphPath, content);
                AssetDatabase.ImportAsset(shaderGraphPath, ImportAssetOptions.ForceUpdate);

                var output = graph.children.OfType<VFXAbstractParticleOutput>().First();
                Assert.IsNotNull(VFXShaderGraphHelpers.GetShaderGraph(output));
                testCase.afterSwichShaderGraphCheck(output);
            }

            //Explicitly inject override
            {
                var output = graph.children.OfType<VFXAbstractParticleOutput>().First();

                var shaderGraphPath = AssetDatabase.GetAssetPath(shaderGraph);
                var referenceMaterial = AssetDatabase.LoadAssetAtPath<Material>(shaderGraphPath);

                var materialVariant = new Material(referenceMaterial);
                materialVariant.parent = referenceMaterial;
                Assert.IsTrue(materialVariant.isVariant);

                Assert.IsNotNull(VFXShaderGraphHelpers.GetShaderGraph(output));
                testCase.injectOverride(output, materialVariant);
            }

            //Revert material settings & Restore SG initial content
            {
                var output = graph.children.OfType<VFXAbstractParticleOutput>().First();
                output.SetSettingValue("materialSettings", new VFXMaterialSerializedSettings());

                var content = File.ReadAllBytes(baseDataPath + testCase.initialShaderGraph + ".shadergraph");
                Assert.AreNotEqual(0u, content.Length);
                var shaderGraphPath = AssetDatabase.GetAssetPath(shaderGraph);
                File.WriteAllBytes(shaderGraphPath, content);
                AssetDatabase.ImportAsset(shaderGraphPath, ImportAssetOptions.ForceUpdate);

                testCase.initialCheck(output);
            }
        }

        public struct Cross_Pipeline_VFX_Override_Test_Case
        {
            public override string ToString()
            {
                return compilationMode.ToString() + (createEditor ? "_With_Inspector" : string.Empty);
            }
            internal VFXCompilationMode compilationMode;
            internal bool createEditor;
        }

        public static Cross_Pipeline_VFX_Override_Test_Case[] k_Cross_Pipeline_Cases = new[]
        {
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Edition, createEditor = false },
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Runtime, createEditor = false },
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Edition, createEditor = true },
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Runtime, createEditor = true },
        };

        private static System.Reflection.PropertyInfo kGetAllowLocking = typeof(Material).GetProperty("allowLocking", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        [UnityTest, Description("Cover behavior from UUM-29663, in editor, both settings from HDRP & URP must be kept")]
        public IEnumerator Cross_Pipeline_VFX_Override([ValueSource(nameof(k_Cross_Pipeline_Cases))] Cross_Pipeline_VFX_Override_Test_Case testCase)
        {
            var path = "Packages/com.unity.testing.visualeffectgraph/Scenes/CrossPipeline_MaterialOverride.vfx";

            if (testCase.compilationMode == VFXCompilationMode.Edition)
            {
                //Had to open the VFX View to switch the compilation to edition
                var initialAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                var window = VFXViewWindow.GetWindow<VFXViewWindow>();
                window.LoadAsset(initialAsset, null);
                for (int i = 0; i < 4; ++i) //Wait for VFX to be load in view
                    yield return null;
            }

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            var visualEffectAsset = allAssets.OfType<VisualEffectAsset>().FirstOrDefault();
            Assert.IsNotNull(visualEffectAsset);

            VFXGraph graph = visualEffectAsset.GetOrCreateResource().GetOrCreateGraph();
            Assert.IsNotNull(graph);
            Assert.AreEqual(testCase.compilationMode, graph.GetCompilationMode());

            var output = graph.children.OfType<VFXComposedParticleOutput>().FirstOrDefault();
            Assert.IsNotNull(output);

            if (testCase.createEditor)
            {
                //Insure from potential side effect from editor creation & inspector gui invocation
                var inspector = EditorWindow.GetWindow<InspectorWindow>();
                Selection.SetActiveObjectWithContext(output, null);
                yield return null;
                for (int i = 0; i < 3; ++i)
                {
                    Assert.IsTrue(inspector.tracker.activeEditors.OfType<VFXComposedParticleOutputEditor>().Any());
                    yield return null;
                }
            }

            var settings = (VFXMaterialSerializedSettings)output.GetSetting("materialSettings").value;
            //Suppose to be overridden, no need to send a material (if not, it will throw a NRE)
            settings.TryGetFloat("_SurfaceType", null, out var _surfaceType);
            settings.TryGetFloat("_Surface", null, out var _surface);
            Assert.AreEqual(1.0f, _surfaceType);
            Assert.AreEqual(1.0f, _surface);

            var materials = allAssets.OfType<Material>().ToArray();
            Material actualMaterial;
            if (testCase.compilationMode == VFXCompilationMode.Edition)
            {
                var parentMaterial = materials.FirstOrDefault(o => o.isVariant == false);
                actualMaterial = materials.FirstOrDefault(o => o.isVariant);
                Assert.AreEqual(2, materials.Length);
                Assert.IsNotNull(actualMaterial);
                Assert.IsNotNull(parentMaterial);
                Assert.IsNotNull(kGetAllowLocking);

                Assert.IsTrue(actualMaterial.enableInstancing);
                Assert.IsTrue(parentMaterial.enableInstancing);

                Assert.IsFalse((bool)kGetAllowLocking.GetValue(actualMaterial));
                Assert.IsTrue((bool)kGetAllowLocking.GetValue(parentMaterial));
            }
            else
            {
                Assert.AreEqual(1, materials.Length);
                actualMaterial = materials.FirstOrDefault();
                Assert.IsNotNull(actualMaterial);
                Assert.IsFalse(actualMaterial.isVariant);

                Assert.IsTrue(actualMaterial.enableInstancing);

                Assert.IsNotNull(kGetAllowLocking);
                Assert.IsTrue((bool)kGetAllowLocking.GetValue(actualMaterial));
            }

            var serializedMaterial = new SerializedObject(actualMaterial);
            var propertyBase = serializedMaterial.FindProperty("m_SavedProperties");
            propertyBase = propertyBase.FindPropertyRelative("m_Floats");

            List<(string name, float value)> savedProperties = new();
            for (int index = 0; index < propertyBase.arraySize; ++index)
            {
                var currentElement = propertyBase.GetArrayElementAtIndex(index);
                var name = currentElement.FindPropertyRelative("first").stringValue;
                var floatValue = currentElement.FindPropertyRelative("second").floatValue;
                savedProperties.Add((name, floatValue));
            }

            //HDRP only
            Assert.IsTrue(savedProperties.Contains(("_SurfaceType", 1.0f)));
            Assert.IsTrue(savedProperties.Contains(("_AlphaDstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)));

            //URP only
            Assert.IsTrue(savedProperties.Contains(("_Surface", 1.0f)));

            //_ScrBlend enters into conflict between HDRP & URP which mean it will switch every time we are reimporting this VFX between these two pipelines
            //See https://unity.slack.com/archives/C04LN9QHESV/p1678450545454359?thread_ts=1678445386.054769&cid=C04LN9QHESV
#if VFX_TESTS_HAS_HDRP && VFX_TESTS_HAS_URP
            Assert.Fail("This suite doesn't support both pipeline yet.");
#elif VFX_TESTS_HAS_HDRP
            Assert.IsTrue(savedProperties.Contains(("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One)));
#elif VFX_TESTS_HAS_URP
            Assert.IsTrue(savedProperties.Contains(("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha)));
#endif

            if (actualMaterial.isVariant)
            {
                //Check saved properties does *not* have any not overriden property (arbitrary unrelated values)
                Assert.IsFalse(savedProperties.Any(o => o.name == "_Dummy"));

                //HDRP
                Assert.IsFalse(savedProperties.Any(o => o.name == "_CullMode"));
                Assert.IsFalse(savedProperties.Any(o => o.name == "_StencilRef"));

                //URP
                Assert.IsFalse(savedProperties.Any(o => o.name == "_Cull"));
                Assert.IsFalse(savedProperties.Any(o => o.name == "_ZTest"));
            }
            else
            {
                Assert.IsTrue(savedProperties.Any(o => o.name == "_Dummy"));

#if VFX_TESTS_HAS_HDRP && VFX_TESTS_HAS_URP
            Assert.Fail("This suite doesn't support both pipeline yet.");
#elif VFX_TESTS_HAS_HDRP
                Assert.IsTrue(savedProperties.Any(o => o.name == "_CullMode"));
                Assert.IsTrue(savedProperties.Any(o => o.name == "_StencilRef"));
#elif VFX_TESTS_HAS_URP
                Assert.IsTrue(savedProperties.Any(o => o.name == "_Cull"));
                Assert.IsTrue(savedProperties.Any(o => o.name == "_ZTest"));
#endif
            }

            yield return null;
            VFXViewWindow.GetWindow<VFXViewWindow>().Close();
            VFXTestCommon.CloseAllUnecessaryWindows();
        }
    }
}
