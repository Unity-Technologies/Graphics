using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.Compilation
{
    [TestFixture]
    public class VFXShaderGraphPropertyAndKeywordTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        static IEnumerable<string> allCompilationMode
        {
            get
            {
                foreach (var mode in Enum.GetValues(typeof(VFXCompilationMode)))
                    yield return mode.ToString();
            }
        }

        public struct ShaderKeywordProperties
        {
            internal string Name;
            internal string Dump;

            public override string ToString()
            {
                return Name;
            }
        }


        static ShaderKeywordProperties[] kShaderKeywordCases = new []
        {
            new ShaderKeywordProperties()
            {
                Name = "025_ShaderKeywords_Constant_MultiCompile",
                Dump = "Smooth - {_SMOOTH}\r\nColor_A - {_COLOR_A_RED, _COLOR_A_SMASHED_PUMPKIN, _COLOR_A_RAJAH, _COLOR_A_PEACH_YELLOW, _COLOR_A_GREEN_SHEEN, _COLOR_A_DARK_CYAN, _COLOR_A_SEA_BLUE, _COLOR_A_OXFORD_BLUE} [Red, Smashed Pumpkin, Rajah, Peach Yellow, Green Sheen, Dark Cyan, Sea Blue, Oxford Blue]\r\nColor_B - {_COLOR_B_RED, _COLOR_B_SMASHED_PUMPKIN, _COLOR_B_RAJAH, _COLOR_B_PEACH_YELLOW, _COLOR_B_GREEN_SHEEN, _COLOR_B_DARK_CYAN, _COLOR_B_SEA_BLUE, _COLOR_B_OXFORD_BLUE} [Red, Smashed Pumpkin, Rajah, Peach Yellow, Green Sheen, Dark Cyan, Sea Blue, Oxford Blue]"
            },
            new ShaderKeywordProperties()
            {
                Name = "025_ShaderKeywords_Constant_ShaderFeature",
                Dump = "_Color_A\r\n_Color_B\r\n_Color_C\r\n_Color_D\r\nShape - {_SHAPE_HEXAGON, _SHAPE_CIRCLE} [Hexagon, Circle]"
            },
            new ShaderKeywordProperties()
            {
                Name = "025_ShaderKeywords_Dynamic",
                Dump = "_Color_A\r\n_Color_B\r\nCircle - {_CIRCLE}\r\nOrient - {_ORIENT_NORTH, _ORIENT_SOUTH, _ORIENT_EAST, _ORIENT_WEST} [North, South, East, West]"
            }
        };

        [Test]
        public void Slot_Naming_And_Content_ShaderKeyword([ValueSource(nameof(kShaderKeywordCases))] ShaderKeywordProperties testCase)
        {
            var shaderGraphPath = $"Packages/com.unity.testing.visualeffectgraph/Scenes/{testCase.Name}.shadergraph";
            var vfxShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(shaderGraphPath);

            var dump = new StringBuilder();
            foreach (var property in VFXShaderGraphHelpers.GetProperties(vfxShaderGraph))
            {
                dump.Append(property.property.property.name);
                if (property.keywordsMapping != null)
                {
                    dump.Append(" - {");
                    dump.AppendJoin(", ", property.keywordsMapping);
                    dump.Append("}");

                    var enumAttribute = property.property.property.attributes.attributes.OfType<EnumAttribute>().FirstOrDefault();
                    if (enumAttribute != null)
                    {
                        dump.Append(" [");
                        dump.AppendJoin(", ", enumAttribute.values);
                        dump.Append("]");
                    }
                }

                dump.AppendLine();
            }

            var expectedDump = new System.IO.StringReader(testCase.Dump);
            var actualDump = new System.IO.StringReader(dump.ToString());
            while (true)
            {
                var expected = expectedDump.ReadLine();
                var actual = actualDump.ReadLine();
                Assert.AreEqual(expected, actual, "Unexpected diff: {0}", dump);
                if (expected == null)
                    break;
            }
        }

        [Test]
        public void Constant_Folding_With_ShaderKeyword([ValueSource("allCompilationMode")] string compilationModeName)
        {
            VFXCompilationMode compilationMode;
            Enum.TryParse(compilationModeName, out compilationMode);

            string vfxPath = "Packages/com.unity.testing.visualeffectgraph/Scenes/025_ShaderKeywords_Constant_MultiCompile.vfx";
            Assert.IsTrue(AssetDatabase.AssetPathExists(vfxPath));

            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath).GetResource();
            vfx.GetGraph().PrepareGraph();
            vfx.GetGraph().SetCompilationMode(compilationMode);
            vfx.GetGraph().CompileAndUpdateAsset(vfx.asset);

            Assert.AreEqual(5, vfx.GetShaderSourceCount());

            int shaderCount = 0;
            for (int index = 0; index < 5; index++)
            {
                var source = vfx.GetShaderSource(index);
                if (source.Contains("#pragma kernel CSMain"))
                    continue;

                //Instancing is expected to be enabled for most of SG Output
                Assert.IsTrue(source.Contains("#pragma multi_compile_instancing"));

                var pragmaList = VFXTestShaderSrcUtils.GetPragmaListFromSource(source);

                var allVariants = pragmaList
                    .Where(o => o.type != null && (o.type.StartsWith("multi_compile") || o.type.StartsWith("shader_feature")))
                    .SelectMany(o => o.values)
                    .ToList();
                Assert.AreNotEqual(0, allVariants.Count);

                var matchingVariant = allVariants.Where(o => o.Contains("_SMOOTH") || o.Contains("_COLOR")).ToList();

                if (compilationMode == VFXCompilationMode.Runtime)
                {
                    Assert.AreEqual(0, matchingVariant.Count);
                }
                else
                {
                    //In edition, we are keeping locally matching variant (two colors with 8 choices + smooth)
                    Assert.AreEqual(8 + 8 + 1, matchingVariant.Count);
                }

                shaderCount++;
            }
            Assert.AreEqual(4, shaderCount);
            vfx.GetGraph().SetCompilationMode(VFXCompilationMode.Runtime);
        }

        static string[] kCheck_SG_Properties_SG_PromotedCase =
        {
            //"Repro_SG_SubProperties_With_Texture_Ref", //Only used as reference to cover the following integration
            "Repro_SG_SubProperties_With_Texture",

            //"Repro_SG_SubProperties_Hybrid_Ref",
            "Repro_SG_SubProperties_Hybrid",

            //"Repro_SG_SubProperties_Ref", //Only used as reference to cover the following integration
            "Repro_SG_SubProperties",
            "Repro_SG_SubProperties_Nested",
            "Repro_SG_SubProperties_Nested_Deep",
            "Repro_SG_SubProperties_TwoSubgraph"
        };

        static string[] kCheck_SG_Properties_SG_PromotedCaseNonExposed =
        {
            //"Repro_SG_SubProperties_NonExposed_Material_Ref", //Only used as reference to cover the following integration
            "Repro_SG_SubProperties_NonExposed_Material",
            "Repro_SG_SubProperties_NonExposed_Material_Deep",

            //"Repro_SG_SubProperties_NonExposed_Global_Ref", //Only used as reference to cover the following integration
            "Repro_SG_SubProperties_NonExposed_Global",
            "Repro_SG_SubProperties_NonExposed_Global_Deep",
        };

        static string[] kAll_Check_SG_Properties = kCheck_SG_Properties_SG_PromotedCase.Concat(kCheck_SG_Properties_SG_PromotedCaseNonExposed).ToArray();

        public enum SG_Promoted_Usage
        {
            Constant,
            Exposed,
            PerParticle
        }
        static SG_Promoted_Usage[] kCheck_SG_Properties_Source_PromotedCases = Enum.GetValues(typeof(SG_Promoted_Usage)).Cast<SG_Promoted_Usage>().ToArray();

        [UnityTest, Ignore("Only used as reference to cover the promoted properties integration")]
        public IEnumerator Check_SG_Properties_Promoted_RegularShaderCompiles([ValueSource(nameof(kAll_Check_SG_Properties))] string shaderGraphName)
        {
            var shaderGraphPath = $"Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/{shaderGraphName}.shadergraph";
            AssetDatabase.ImportAsset(shaderGraphPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            yield return null;

            var objets = AssetDatabase.LoadAllAssetsAtPath(shaderGraphPath);
            var shaders = objets.OfType<Shader>().ToArray();
            var materials = objets.OfType<Material>().ToArray();

            var relevantMaterialCount = 0;
            foreach (var material in materials)
            {
                var shaderData = ShaderUtil.GetShaderData(material.shader);
                if (shaders.Contains(material.shader))
                    relevantMaterialCount++;

                int passCount = shaderData.ActiveSubshader.PassCount;
                for (int pass = 0; pass < passCount; pass++)
                {
                    ShaderUtil.CompilePass(material, pass, true);
                }
            }
            yield return null;

            Assert.AreEqual(1, relevantMaterialCount);
            foreach (var shader in shaders)
            {
                var allMessages = ShaderUtil.GetShaderMessages(shader);

                Assert.AreEqual(0, allMessages.Length, allMessages.Length > 0 ? allMessages[0].message : string.Empty);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
                Assert.IsFalse(ShaderUtil.ShaderHasWarnings(shader));
            }
        }

        [UnityTest]
        public IEnumerator Check_SG_Properties_Promoted_With_VFX([ValueSource(nameof(kCheck_SG_Properties_SG_PromotedCase))] string shaderGraphName, [ValueSource(nameof(kCheck_SG_Properties_Source_PromotedCases))] SG_Promoted_Usage sourceCase)
        {
            var hasTexture = shaderGraphName.Contains("With_Texture");

            //Check SG content
            var shaderGraphPath = $"Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/{shaderGraphName}.shadergraph";
            AssetDatabase.ImportAsset(shaderGraphPath);
            var vfxShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(shaderGraphPath);
            var properties = VFXShaderGraphHelpers.GetProperties(vfxShaderGraph).ToArray();
            Assert.AreEqual(hasTexture ? 2 : 1, properties.Length);
            Assert.AreEqual("_MyExposedVector4", properties[0].property.property.name);
            Assert.AreEqual(typeof(Vector4), properties[0].property.property.type);
            Assert.AreEqual(new Vector4(1,0,1,0.78f), properties[0].property.value);

            //Check VFX integration
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var sgOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
            sgOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
            sgOutput.SetSettingValue("shaderGraph", vfxShaderGraph);
            graph.AddChild(sgOutput);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            spawner.LinkTo(contextInitialize);
            graph.AddChild(spawner);

            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            var blockAttribute = blockAttributeDesc.variant.CreateInstance();
            blockAttribute.SetSettingValue("attribute", "position");
            contextInitialize.AddChild(blockAttribute);

            contextInitialize.LinkTo(sgOutput);
            graph.AddChild(contextInitialize);

            var randomOperator = ScriptableObject.CreateInstance<Operator.Random>();
            randomOperator.SetOperandType(typeof(Vector4));
            randomOperator.SetSettingValue("constant", false);
            graph.AddChild(randomOperator);

            var exposedProperty = ScriptableObject.CreateInstance<VFXParameter>();
            exposedProperty.Init(typeof(Vector4));
            exposedProperty.SetSettingValue("m_Exposed", true);
            exposedProperty.SetSettingValue("m_ExposedName", "ExposedVector4");
            graph.AddChild(exposedProperty);

            var nonExposed = ScriptableObject.CreateInstance<VFXParameter>();
            nonExposed.Init(typeof(Vector4));
            nonExposed.SetSettingValue("m_Exposed", false);
            nonExposed.SetSettingValue("m_ExposedName", "NonExposedVector4");
            graph.AddChild(nonExposed);

            VFXSlot sourceSlot = null;
            if (sourceCase == SG_Promoted_Usage.Constant)
            {
                sourceSlot = nonExposed.outputSlots[0];
            }
            else if (sourceCase == SG_Promoted_Usage.Exposed)
            {
                sourceSlot = exposedProperty.outputSlots[0];
            }
            else if (sourceCase == SG_Promoted_Usage.PerParticle)
            {
                sourceSlot = randomOperator.outputSlots[0];
            }

            Assert.IsNotNull(sourceSlot);
            var sgPortSlot = sgOutput.inputSlots.First(o => o.name.Contains("_MyExposedVector4"));
            Assert.IsTrue(sgPortSlot.Link(sourceSlot));

            var resource = graph.GetResource();
            var vfxPath = AssetDatabase.GetAssetPath(graph);
            resource.WriteAsset();

            var sourceCount = resource.GetShaderSourceCount();
            var generatedCode = "";
            for (var source = 0; source < sourceCount; ++source)
            {
                var sourceName = resource.GetShaderSourceName(source);
                if (sourceName.Contains("Output Particle Shader Graph Quad"))
                {
                    generatedCode = resource.GetShaderSource(source);
                }
            }

            //Inspect generated code
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode));
            var expectVaryingProperty = sourceCase == SG_Promoted_Usage.PerParticle;

#if VFX_TESTS_HAS_HDRP
            const string k_GraphPropertiesStructName = "GraphProperties";
            const string k_SurfaceInputsStructName   = "FragInputsVFX";
            const string k_VaryingsStructName        = "VaryingsMeshToPS";
            const string k_PackedVaryingsStructName  = "PackedVaryingsMeshToPS";
#elif VFX_TESTS_HAS_URP
            const string k_GraphPropertiesStructName = "GraphProperties";
            const string k_SurfaceInputsStructName   = "SurfaceDescriptionInputs";
            const string k_VaryingsStructName        = "Varyings";
            const string k_PackedVaryingsStructName  = "PackedVaryings";
#else
#error this suite don't support both pipeline at the same time
#endif

            var graphPropertiesFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(generatedCode, k_GraphPropertiesStructName);
            var surfaceInputFields    = VFXTestShaderSrcUtils.GetStructFieldsFromSource(generatedCode, k_SurfaceInputsStructName);
            var varyingsFields        = VFXTestShaderSrcUtils.GetStructFieldsFromSource(generatedCode, k_VaryingsStructName);
            var packedVaryingFields   = VFXTestShaderSrcUtils.GetStructFieldsFromSource(generatedCode, k_PackedVaryingsStructName);

            Assert.IsNotNull(graphPropertiesFields, $"{k_GraphPropertiesStructName} struct not found");
            Assert.IsNotNull(surfaceInputFields, $"{k_SurfaceInputsStructName} struct not found");
            Assert.IsNotNull(varyingsFields, $"{k_VaryingsStructName} struct not found");
            Assert.IsNotNull(packedVaryingFields, $"{k_PackedVaryingsStructName} struct not found");

            Assert.IsTrue(graphPropertiesFields.Any(f => f.name == "_MyExposedVector4" && f.type == "float4"), $"[{sourceCase}]: {k_GraphPropertiesStructName} expected to contain 'float4 _MyExposedVector4'");
            Assert.IsTrue(surfaceInputFields.Any(f => f.name == "_MyExposedVector4" && f.type == "float4"), $"[{sourceCase}]: {k_SurfaceInputsStructName} expected to contain 'float4 _MyExposedVector4'");

            var varyingProp = varyingsFields.FirstOrDefault(f => f.name == "_MyExposedVector4");
            if (expectVaryingProperty)
            {
                Assert.IsNotNull(varyingProp.name, $"[PerParticle]: {k_VaryingsStructName} expected to contain '_MyExposedVector4'");
                Assert.AreEqual("float4", varyingProp.type, $"[PerParticle]: {k_VaryingsStructName}._MyExposedVector4 must be float4");
                StringAssert.Contains("nointerpolation", varyingProp.modifiers, $"[PerParticle]: {k_VaryingsStructName}._MyExposedVector4 must be 'nointerpolation' (per-particle, flat across primitive)");
            }
            else
            {
                Assert.IsFalse(varyingsFields.Any(f => f.name == "_MyExposedVector4"), $"[{sourceCase}]: {k_VaryingsStructName} should NOT carry '_MyExposedVector4' (value is uniform, not per-particle)");
            }

            var packedProp = packedVaryingFields.FirstOrDefault(f => f.name == "_MyExposedVector4");
            if (expectVaryingProperty)
            {
                Assert.IsNotNull(packedProp.name, $"[PerParticle]: {k_PackedVaryingsStructName} expected to contain '_MyExposedVector4'");
                Assert.AreEqual("float4", packedProp.type, $"[PerParticle]: {k_PackedVaryingsStructName}._MyExposedVector4 must be float4");
                StringAssert.Contains("nointerpolation", packedProp.modifiers, $"[PerParticle]: {k_PackedVaryingsStructName}._MyExposedVector4 must be 'nointerpolation'");
                StringAssert.StartsWith("INTERP", packedProp.semantics, $"[PerParticle]: {k_PackedVaryingsStructName}._MyExposedVector4 must have an INTERPx semantic");
            }
            else
            {
                Assert.IsFalse(packedVaryingFields.Any(f => f.name == "_MyExposedVector4"), $"[{sourceCase}]: {k_PackedVaryingsStructName} should NOT carry '_MyExposedVector4' (value is uniform)");
            }

            var cbufferStart = generatedCode.IndexOf("CBUFFER_START(UnityPerMaterial)", StringComparison.Ordinal);
            var cbufferEnd = cbufferStart >= 0 ? generatedCode.IndexOf("CBUFFER_END", cbufferStart, StringComparison.Ordinal) : -1;
            Assert.Greater(cbufferStart, -1, "Expected CBUFFER_START(UnityPerMaterial) in generated source");
            Assert.Greater(cbufferEnd, cbufferStart, "Expected CBUFFER_END after CBUFFER_START in generated source");
            var cbufferBlock = generatedCode.Substring(cbufferStart, cbufferEnd - cbufferStart);
            StringAssert.DoesNotContain("_MyExposedVector4", cbufferBlock, "Promoted subgraph property '_MyExposedVector4' should not be declared in the UnityPerMaterial cbuffer in VFX context (it is provided per-particle via GraphProperties).");

            if (hasTexture)
            {
                StringAssert.Contains("TEXTURE2D(_MyExposedTexture2D)", generatedCode, "Expected the promoted texture to be declared via 'TEXTURE2D(_MyExposedTexture2D)' as a raw global resource.");
                StringAssert.Contains("SAMPLER(sampler_MyExposedTexture2D)", generatedCode, "Expected 'SAMPLER(sampler_MyExposedTexture2D)' alongside the promoted texture declaration.");
                StringAssert.DoesNotContain("UnityTexture2D _MyExposedTexture2D", generatedCode, "'_MyExposedTexture2D' must not be declared as a UnityTexture2D wrapper - it must remain a raw Texture2D so UnityBuildTexture2DStructInternal can wrap it.");
                StringAssert.Contains("UnityBuildTexture2DStructInternal(_MyExposedTexture2D", generatedCode, "Expected at least one 'UnityBuildTexture2DStructInternal(_MyExposedTexture2D, ...)' call wrapping the raw promoted texture.");
            }

            //Insure there isn't any compilation error
            var objets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);
            var shaders = objets.OfType<Shader>().ToArray();
            var materials = objets.OfType<Material>().ToArray();

            var relevantMaterialCount = 0;
            foreach (var material in materials)
            {
                var shaderData = ShaderUtil.GetShaderData(material.shader);
                if (shaders.Contains(material.shader))
                    relevantMaterialCount++;

                int passCount = shaderData.ActiveSubshader.PassCount;
                for (int pass = 0; pass < passCount; pass++)
                {
                    ShaderUtil.CompilePass(material, pass, true);
                }
            }
            yield return null;

            Assert.AreEqual(1u, shaders.Length);
            Assert.AreEqual(1u, relevantMaterialCount);
            foreach (var shader in shaders)
            {
                var allMessages = ShaderUtil.GetShaderMessages(shader);

                Assert.AreEqual(0, allMessages.Length, allMessages.Length > 0 ? allMessages[0].message : string.Empty);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
                Assert.IsFalse(ShaderUtil.ShaderHasWarnings(shader));
            }
        }

        [UnityTest]
        public IEnumerator Check_SG_Properties_Promoted_With_VFX_NonExposed([ValueSource(nameof(kCheck_SG_Properties_SG_PromotedCaseNonExposed))] string shaderGraphName)
        {
            var isGlobal = shaderGraphName.Contains("Global");

            var shaderGraphPath = $"Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/{shaderGraphName}.shadergraph";
            AssetDatabase.ImportAsset(shaderGraphPath);
            var vfxShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(shaderGraphPath);
            var properties = VFXShaderGraphHelpers.GetProperties(vfxShaderGraph).ToArray();
            Assert.AreEqual(0, properties.Length);

            //Non-exposed promoted textures must remain in textureInfos; VFX has no slot for them, so the constant binding is the only way the default texture reaches the generated material (PR 69700 comment 967732).
            var textureInfoNames = vfxShaderGraph.textureInfos.Select(t => t.name).ToArray();
            Assert.IsTrue(textureInfoNames.Contains("_MyNonExposedTexture"), $"Non-exposed promoted texture '_MyNonExposedTexture' must remain in textureInfos so VFX binds the default texture in the generated material. Found: [{string.Join(", ", textureInfoNames)}]");

            var graph = VFXTestCommon.MakeTemporaryGraph();
            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var sgOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
            sgOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
            sgOutput.SetSettingValue("shaderGraph", vfxShaderGraph);
            graph.AddChild(sgOutput);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            spawner.LinkTo(contextInitialize);
            graph.AddChild(spawner);

            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            var blockAttribute = blockAttributeDesc.variant.CreateInstance();
            blockAttribute.SetSettingValue("attribute", "position");
            contextInitialize.AddChild(blockAttribute);

            contextInitialize.LinkTo(sgOutput);
            graph.AddChild(contextInitialize);

            Assert.AreEqual(0, sgOutput.inputSlots.Count);

            var vfxPath = AssetDatabase.GetAssetPath(graph);
            var resource = VisualEffectResource.GetResourceAtPath(vfxPath);
            resource.WriteAsset();
            yield return null;

            var sourceCount = resource.GetShaderSourceCount();
            var generatedCode = "";
            for (var source = 0; source < sourceCount; ++source)
            {
                var sourceName = resource.GetShaderSourceName(source);
                if (sourceName.Contains("Output Particle Shader Graph Quad"))
                {
                    generatedCode = resource.GetShaderSource(source);
                }
            }

            //Inspect generated code
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode));

            var cbufferStart = generatedCode.IndexOf("CBUFFER_START(UnityPerMaterial)", StringComparison.Ordinal);
            var cbufferEnd = cbufferStart >= 0 ? generatedCode.IndexOf("CBUFFER_END", cbufferStart, StringComparison.Ordinal) : -1;
            Assert.Greater(cbufferStart, -1, "Expected CBUFFER_START(UnityPerMaterial) in generated source");
            Assert.Greater(cbufferEnd, cbufferStart, "Expected CBUFFER_END after CBUFFER_START in generated source");
            var cbufferBlock = generatedCode.Substring(cbufferStart, cbufferEnd - cbufferStart);

            if (!isGlobal)
            {
                StringAssert.Contains("_MyNonExposedVector4", cbufferBlock, "Promoted subgraph property '_MyNonExposedVector4' should be declared in the UnityPerMaterial cbuffer.");
            }
            else
            {
                StringAssert.DoesNotContain("_MyNonExposedVector4", cbufferBlock, "Promoted subgraph property '_MyNonExposedVector4' should not be declared in the UnityPerMaterial cbuffer.");
                StringAssert.Contains("float4 _MyNonExposedVector4;", generatedCode, "Promoted subgraph property '_MyNonExposedVector4' should declared outside the cbuffer declaration.");
            }

#if VFX_TESTS_HAS_HDRP
            const string k_GraphPropertiesStructName = "GraphProperties";
            const string k_SurfaceInputsStructName = "FragInputsVFX";
            const string k_VaryingsStructName = "VaryingsMeshToPS";
            const string k_PackedVaryingsStructName = "PackedVaryingsMeshToPS";
#elif VFX_TESTS_HAS_URP
            const string k_GraphPropertiesStructName = "GraphProperties";
            const string k_SurfaceInputsStructName   = "SurfaceDescriptionInputs";
            const string k_VaryingsStructName        = "Varyings";
            const string k_PackedVaryingsStructName  = "PackedVaryings";
#else
#error this suite don't support both pipeline at the same time
#endif

            var graphPropertiesFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(generatedCode, k_GraphPropertiesStructName);
            var surfaceInputFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(generatedCode, k_SurfaceInputsStructName);
            var varyingsFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(generatedCode, k_VaryingsStructName);
            var packedVaryingFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(generatedCode, k_PackedVaryingsStructName);

            Assert.IsNotNull(graphPropertiesFields, $"{k_GraphPropertiesStructName} struct not found");
            Assert.IsNotNull(surfaceInputFields, $"{k_SurfaceInputsStructName} struct not found");
            Assert.IsNotNull(varyingsFields, $"{k_VaryingsStructName} struct not found");
            Assert.IsNotNull(packedVaryingFields, $"{k_PackedVaryingsStructName} struct not found");

            Assert.IsFalse(graphPropertiesFields.Any(f => f.name == "_MyNonExposedVector4"), "Promoted subgraph property '_MyNonExposedVector4' should be declared in GraphProperties because never provided by VFX");
            Assert.IsFalse(surfaceInputFields.Any(f => f.name == "_MyNonExposedVector4"));
            Assert.IsFalse(varyingsFields.Any(f => f.name == "_MyNonExposedVector4"));
            StringAssert.DoesNotContain("PROP._MyNonExposedVector4", generatedCode, "Unexpected fetching of non exposed property from GraphProperties");

            //Insure there isn't any compilation error
            var objets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);
            var shaders = objets.OfType<Shader>().ToArray();
            var materials = objets.OfType<Material>().ToArray();

            var relevantMaterialCount = 0;
            foreach (var material in materials)
            {
                var shaderData = ShaderUtil.GetShaderData(material.shader);
                if (shaders.Contains(material.shader))
                    relevantMaterialCount++;

                int passCount = shaderData.ActiveSubshader.PassCount;
                for (int pass = 0; pass < passCount; pass++)
                {
                    ShaderUtil.CompilePass(material, pass, true);
                }
            }
            yield return null;

            Assert.AreEqual(1u, shaders.Length);
            Assert.AreEqual(1u, relevantMaterialCount);
            foreach (var shader in shaders)
            {
                var allMessages = ShaderUtil.GetShaderMessages(shader);

                Assert.AreEqual(0, allMessages.Length, allMessages.Length > 0 ? allMessages[0].message : string.Empty);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
                Assert.IsFalse(ShaderUtil.ShaderHasWarnings(shader));
            }

        }

        static string[] Check_SG_Properties_Promoted_But_UnusedCases =
        {
            "Repro_SG_SubProperties_Unused",
            "Repro_SG_SubProperties_Nested_Unused",

        };

        [UnityTest]
        public IEnumerator Check_SG_Properties_Promoted_But_Unused([ValueSource(nameof(Check_SG_Properties_Promoted_But_UnusedCases))] string shaderGraphName)
        {
            var shaderGraphPath = $"Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/{shaderGraphName}.shadergraph";
            AssetDatabase.ImportAsset(shaderGraphPath);
            yield return null;

            var material = AssetDatabase.LoadAssetAtPath<Material>(shaderGraphPath);
            Assert.IsFalse(material.HasProperty("_MyExposedVector4"));

            var vfxShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(shaderGraphPath);
            var properties = VFXShaderGraphHelpers.GetProperties(vfxShaderGraph).ToArray();
            Assert.AreEqual(0, properties.Length, "Unexpected property: " + (properties.Length > 0 ? properties[0].property.property.name : "<none>"));
        }

        [UnityTest]
        public IEnumerator Check_SG_Properties_But_Unused()
        {
            var shaderGraphName = "Repro_SG_Properties_Unused";

            var shaderGraphPath = $"Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/{shaderGraphName}.shadergraph";
            AssetDatabase.ImportAsset(shaderGraphPath);
            yield return null;

            var material = AssetDatabase.LoadAssetAtPath<Material>(shaderGraphPath);
            Assert.IsTrue(material.HasProperty("_MyExposedVector4"));

            var vfxShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(shaderGraphPath);
            var properties = VFXShaderGraphHelpers.GetProperties(vfxShaderGraph).ToArray();
            Assert.AreEqual(1, properties.Length);
            Assert.AreEqual("_MyExposedVector4", properties[0].property.property.name);

            var graph = VFXTestCommon.MakeTemporaryGraph();
            var sgOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
            sgOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
            sgOutput.SetSettingValue("shaderGraph", vfxShaderGraph);
            graph.AddChild(sgOutput);

            Assert.AreEqual(1, sgOutput.inputSlots.Count);
            Assert.AreEqual("_MyExposedVector4", sgOutput.inputSlots[0].name);
        }
    }
}
