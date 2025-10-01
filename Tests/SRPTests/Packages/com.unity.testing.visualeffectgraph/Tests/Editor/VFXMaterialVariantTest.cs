using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using UnityEditor.ShaderGraph.Internal;

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
            while (EditorWindow.HasOpenInstances<VFXViewWindow>())
                EditorWindow.GetWindow<VFXViewWindow>().Close();
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

        [UnityTest, Description("Covers UUM-115004, N.B.: This coverage can't be full without cross SRP project")]
        public IEnumerator Failing_SG_Target_With_Current_Pipeline()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_115004.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            yield return null;

            var vfxPath = VFXTestCommon.tempBasePath + "Repro_UUM_115004.vfx";
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsNotNull(vfxAsset);

            var vfxResource = vfxAsset.GetResource();
            Assert.IsNotNull(vfxResource);

            var vfxGraph = vfxResource.GetOrCreateGraph();
            Assert.IsNotNull(vfxGraph);

            var window = VFXViewWindow.GetWindow(vfxGraph, true, true);
            window.LoadAsset(vfxAsset, null);
            window.Focus();
            yield return null; //No error preventing to open the graph

            var particleOutputs = vfxGraph.children.OfType<VFXComposedParticleOutput>().ToArray();
            Assert.AreEqual(4, particleOutputs.Length);
            foreach (var particleOutput in particleOutputs)
            {
                Assert.IsNotNull(particleOutput.inputFlowSlot[0].link[0].context);

                var serializedShaderGraph = particleOutput.GetSettingValue("shaderGraph") as ShaderGraphVfxAsset;
                var actualShaderGraph = particleOutput.GetShaderGraph();
                Assert.IsNotNull(actualShaderGraph);

                bool isCompatibleWithCurrentSRP = false;
#if VFX_TESTS_HAS_HDRP && VFX_TESTS_HAS_URP
                Assert.Fail("This suite doesn't support both pipeline yet.");
#elif VFX_TESTS_HAS_HDRP
                if (particleOutput.label.Contains("HDRP"))
                {
                    isCompatibleWithCurrentSRP = true;
                }
#elif VFX_TESTS_HAS_URP
                if (particleOutput.label.Contains("URP"))
                {
                    isCompatibleWithCurrentSRP = true;
                }
#endif
                if (isCompatibleWithCurrentSRP)
                {
                    Assert.AreEqual(1, particleOutput.inputSlots.Count);
                    Assert.IsTrue((bool)particleOutput.inputSlots[0].HasLink());
                    Assert.IsNotNull(serializedShaderGraph);
                }
                else
                {
                    //N.B.: This asset won't be null if both SRP are available, checking reference to missing data here (but known guid)
                    Assert.IsFalse(object.ReferenceEquals(serializedShaderGraph, null));
                    Assert.IsTrue(serializedShaderGraph == null);
                }
            }

            window.Close();
            yield return null;
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

        [UnityTest]
        public IEnumerator Switch_ShaderGraph_And_Undo()
        {
            var baseDataPath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/";
            var packagePath = baseDataPath + "/Repro_97849.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            yield return null;

            var shaderGraphPath_A = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(VFXTestCommon.tempBasePath + "/VFX_With_SG_A.shadergraph");
            var shaderGraphPath_B = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(VFXTestCommon.tempBasePath + "/VFX_With_SG_B.shadergraph");
            var vfxPath = VFXTestCommon.tempBasePath + "/VFX_With_SG.vfx";
            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsNotNull(shaderGraphPath_A);
            Assert.IsNotNull(shaderGraphPath_B);
            Assert.IsNotNull(vfx);

            var graph = vfx.GetOrCreateResource().GetOrCreateGraph();
            var window = VFXViewWindow.GetWindow(vfx, true);
            window.LoadAsset(vfx, null);
            window.Show();
            Assert.IsNotNull(graph);

            var output = graph.children.OfType<VFXComposedParticleOutput>().SingleOrDefault();
            Assert.IsNotNull(output);

            Undo.IncrementCurrentGroup();
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_A"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_B"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_C"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_1"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_2"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_3"));

            output.SetSettingValue("shaderGraph", shaderGraphPath_B);
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_A"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_B"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_C"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_1"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_2"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_3"));

            Undo.PerformUndo();
            yield return null;

            Assert.AreEqual(shaderGraphPath_A, output.GetShaderGraph());
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_A"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_B"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_C"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_1"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_2"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_3"));

            Undo.PerformRedo();
            yield return null;

            Assert.AreEqual(shaderGraphPath_B, output.GetShaderGraph());
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_A"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_B"));
            Assert.AreEqual(0, output.inputSlots.Count(o => o.name == "_C"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_1"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_2"));
            Assert.AreEqual(1, output.inputSlots.Count(o => o.name == "_3"));

            Undo.PerformUndo();
            yield return null;
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
                return $"{compilationMode}{(createEditor ? "_With_Inspector" : string.Empty)}{(usingImporter ? "_Using_Importer" : string.Empty)}";
            }
            internal VFXCompilationMode compilationMode;
            internal bool createEditor;
            internal bool usingImporter;
        }

        public static Cross_Pipeline_VFX_Override_Test_Case[] k_Cross_Pipeline_Cases = new[]
        {
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Edition, createEditor = false , usingImporter = false},
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Runtime, createEditor = false , usingImporter = false},
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Edition, createEditor = true  , usingImporter = false},
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Runtime, createEditor = true  , usingImporter = false},

            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Edition, createEditor = false , usingImporter = true},
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Runtime, createEditor = false , usingImporter = true},
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Edition, createEditor = true  , usingImporter = true},
            new Cross_Pipeline_VFX_Override_Test_Case() { compilationMode = VFXCompilationMode.Runtime, createEditor = true  , usingImporter = true},
        };

        private static System.Reflection.PropertyInfo kGetAllowLocking = typeof(Material).GetProperty("allowLocking", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        [UnityTest, Description("Cover behavior from UUM-29663 & UUM-77448, in editor, both settings from HDRP & URP must be kept")]
        public IEnumerator Cross_Pipeline_VFX_Override([ValueSource(nameof(k_Cross_Pipeline_Cases))] Cross_Pipeline_VFX_Override_Test_Case testCase)
        {
            var path = "Packages/com.unity.testing.visualeffectgraph/Scenes/CrossPipeline_MaterialOverride.vfx";

            var visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            Assert.IsNotNull(visualEffectAsset);

            VFXGraph graph = visualEffectAsset.GetOrCreateResource().GetOrCreateGraph();
            Assert.IsNotNull(graph);

            graph.SetCompilationMode(testCase.compilationMode);

            UnityEngine.Object[] allAssets;

            if (testCase.usingImporter)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            }
            else
            {
                allAssets = graph.CompileAndUpdateAsset(visualEffectAsset);
            }

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

        IEnumerator PrepareSimpleGraphWithSgOutput(VFXGraph graph)
        {
            var sgOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
            sgOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());

            var sgPath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/SG_Unlit_Opaque.shadergraph";
            var sgVfxAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(sgPath);
            Assert.IsNotNull(sgVfxAsset);
            sgOutput.SetSettingValue("shaderGraph", sgVfxAsset);

            var updateContext = graph.children.OfType<VFXBasicUpdate>().Single();
            updateContext.UnlinkTo(graph.children.OfType<VFXAbstractRenderedOutput>().Single());
            updateContext.LinkTo(sgOutput);
            graph.AddChild(sgOutput);
            graph.GetResource().WriteAssetWithSubAssets();
            Assert.IsFalse(EditorUtility.IsDirty(graph));
            Assert.AreEqual(VFXCompilationMode.Runtime, VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset));
            yield return null;

            var window = VFXViewWindow.GetWindow(graph, true, true);
            window.LoadAsset(graph.GetResource().asset, null);
            for (int i = 0; i < 8; ++i)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset) == VFXCompilationMode.Runtime)
                    yield return null;
            }
        }

        [UnityTest, Description("Cover UUM-115015")]
        public IEnumerator Modify_Material_And_Check_VFX_Dirty()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            yield return PrepareSimpleGraphWithSgOutput(graph);

            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset));
            Assert.IsFalse(EditorUtility.IsDirty(graph));

            graph.CompileAndUpdateAsset(graph.GetResource().asset);
            var allMaterials = VFXTestCommon.GetPreviewAssets(graph).OfType<Material>().ToArray();
            Assert.AreEqual(2, allMaterials.Length);
            Assert.IsFalse(EditorUtility.IsDirty(graph));
            yield return null;

            var writableMaterial = allMaterials.Single(o => o.isVariant);
#if VFX_TESTS_HAS_HDRP && VFX_TESTS_HAS_URP
            Assert.Fail("This suite doesn't support both pipeline yet.");
#elif VFX_TESTS_HAS_URP
            writableMaterial.SetFloat("_Cull", 123.0f);
#elif VFX_TESTS_HAS_HDRP
            writableMaterial.SetFloat("_CullMode", 123.0f);
#endif
            var sgOutput = graph.children.OfType<VFXComposedParticleOutput>().Single();
            Assert.AreEqual(sgOutput.FindMaterial(), writableMaterial);

            //Mimicking what would do the Material Inspector (aka modify material settings and invoke only materialChanged)
            var materialSettings = sgOutput.GetSettingValue("materialSettings") as VFXMaterialSerializedSettings;
            Assert.IsNotNull(materialSettings);
            materialSettings.SyncFromMaterial(writableMaterial);
            //Check expected dirty
            sgOutput.Invalidate(sgOutput, VFXModel.InvalidationCause.kMaterialChanged);
            Assert.IsTrue(EditorUtility.IsDirty(graph));
            yield return null;
        }

        public static bool[] kModify_SG_Output_And_Redo = { false, true };
        [UnityTest, Description("Cover UUM-115036")]
        public IEnumerator Modify_SG_Output_And_Redo([ValueSource(nameof(kModify_SG_Output_And_Redo))] bool modifySettings)
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            yield return PrepareSimpleGraphWithSgOutput(graph);
            var sgOutput = graph.children.OfType<VFXComposedParticleOutput>().Single();
            if (modifySettings)
            {
                graph.CompileAndUpdateAsset(graph.GetResource().asset);
                var allMaterials = VFXTestCommon.GetPreviewAssets(graph).OfType<Material>().ToArray();
                Assert.AreEqual(2, allMaterials.Length);
                Assert.IsFalse(EditorUtility.IsDirty(graph));
                var writableMaterial = allMaterials.Single(o => o.isVariant);
#if VFX_TESTS_HAS_HDRP && VFX_TESTS_HAS_URP
            Assert.Fail("This suite doesn't support both pipeline yet.");
#elif VFX_TESTS_HAS_URP
            writableMaterial.SetFloat("_Cull", 789.0f);
#elif VFX_TESTS_HAS_HDRP
                writableMaterial.SetFloat("_CullMode", 789.0f);
#endif

                var materialSettings = new VFXMaterialSerializedSettings();
                materialSettings.SyncFromMaterial(writableMaterial);
                sgOutput.SetSettingValues(new KeyValuePair<string, object>[] { new("materialSettings", materialSettings) }, true);
                graph.GetResource().WriteAsset();
            }

            Undo.IncrementCurrentGroup();
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            Assert.IsNotNull(blockAttributeDesc);
            var blockAttribute = blockAttributeDesc.variant.CreateInstance();
            blockAttribute.SetSettingValue("attribute", "alive");
            sgOutput.AddChild(blockAttribute);
            Assert.AreEqual(1, sgOutput.children.Count());

            Undo.PerformUndo();
            Assert.AreEqual(0, sgOutput.children.Count());
            yield return null;
            Assert.AreEqual(0, sgOutput.children.Count());

            Undo.PerformRedo();
            Assert.AreEqual(1, sgOutput.children.Count());
            yield return null;
            Assert.AreEqual(1, sgOutput.children.Count());

            Undo.PerformUndo();
            Assert.AreEqual(0, sgOutput.children.Count());
            yield return null;
            Assert.AreEqual(0, sgOutput.children.Count());

            yield return null;
        }

        [UnityTest, Description("Cover Undo/Redo of material properties in SG Output")]
        public IEnumerator Modify_SG_Output_Material_Property_Slow_Path()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            yield return PrepareSimpleGraphWithSgOutput(graph);
            var sgOutput = graph.children.OfType<VFXComposedParticleOutput>().Single();
            Assert.AreEqual(0, ExtractMaterialSettings(sgOutput).Count);

            var currentBlendMode = VFXLibrary.currentSRPBinder.GetBlendModeFromMaterial(sgOutput.GetShaderGraph(), (VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings"));
            Assert.AreEqual(VFXAbstractRenderedOutput.BlendMode.Opaque, currentBlendMode);

            Undo.IncrementCurrentGroup();
            {
                var material = sgOutput.FindMaterial();
#if VFX_TESTS_HAS_HDRP && VFX_TESTS_HAS_URP
#elif VFX_TESTS_HAS_HDRP
                material.SetFloat("_SurfaceType", (float)1.0f);
#elif VFX_TESTS_HAS_URP
                material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
                BaseShaderGUI.SetupMaterialBlendMode(material);
#endif
                ((VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings")).SyncFromMaterial(material);
                sgOutput.Invalidate(VFXModel.InvalidationCause.kSettingChanged); //See DoInspectorGUI "previousBlendMode != currentBlendMode"
            }
            yield return null;

            currentBlendMode = VFXLibrary.currentSRPBinder.GetBlendModeFromMaterial(sgOutput.GetShaderGraph(), (VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings"));
            Assert.AreEqual(VFXAbstractRenderedOutput.BlendMode.Alpha, currentBlendMode);

            Undo.PerformUndo();
            yield return null;

            currentBlendMode = VFXLibrary.currentSRPBinder.GetBlendModeFromMaterial(sgOutput.GetShaderGraph(), (VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings"));
            Assert.AreEqual(VFXAbstractRenderedOutput.BlendMode.Opaque, currentBlendMode);

            Undo.PerformRedo();
            yield return null;

            currentBlendMode = VFXLibrary.currentSRPBinder.GetBlendModeFromMaterial(sgOutput.GetShaderGraph(), (VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings"));
            Assert.AreEqual(VFXAbstractRenderedOutput.BlendMode.Alpha, currentBlendMode);
        }

#if VFX_TESTS_HAS_URP

        [UnityTest, Description("Cover Undo/Redo of material properties in SG Output")]
        public IEnumerator Modify_SG_Output_Material_Property_Fast_Path()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            yield return PrepareSimpleGraphWithSgOutput(graph);
            var sgOutput = graph.children.OfType<VFXComposedParticleOutput>().Single();
            Assert.AreEqual(0, ExtractMaterialSettings(sgOutput).Count);

            Assert.IsTrue(VFXLibrary.currentSRPBinder.TryGetCastShadowFromMaterial(sgOutput.GetShaderGraph(), (VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings"), out var castShadow));
            Assert.IsTrue(castShadow);

            Undo.IncrementCurrentGroup();
            {
                var material = sgOutput.FindMaterial();
                material.SetFloat("_CastShadows", 0.0f);

                Undo.RecordObject(sgOutput, "TODOPAUL: this isn't done by UX (yet)");
                ((VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings")).SyncFromMaterial(material);
                sgOutput.Invalidate(VFXModel.InvalidationCause.kMaterialChanged); //See DoInspectorGUI "previousBlendMode != currentBlendMode", fast path is available for _CastShadow
                //sgOutput.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }
            yield return null;

            Assert.IsTrue(VFXLibrary.currentSRPBinder.TryGetCastShadowFromMaterial(sgOutput.GetShaderGraph(), (VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings"), out castShadow));
            Assert.IsFalse(castShadow);

            Undo.PerformUndo();
            yield return null;

            Assert.IsTrue(VFXLibrary.currentSRPBinder.TryGetCastShadowFromMaterial(sgOutput.GetShaderGraph(), (VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings"), out castShadow));
            Assert.IsTrue(castShadow);

            Undo.PerformRedo();
            yield return null;

            Assert.IsTrue(VFXLibrary.currentSRPBinder.TryGetCastShadowFromMaterial(sgOutput.GetShaderGraph(), (VFXMaterialSerializedSettings)sgOutput.GetSettingValue("materialSettings"), out castShadow));
            Assert.IsFalse(castShadow);
        }
#endif
    }
}
