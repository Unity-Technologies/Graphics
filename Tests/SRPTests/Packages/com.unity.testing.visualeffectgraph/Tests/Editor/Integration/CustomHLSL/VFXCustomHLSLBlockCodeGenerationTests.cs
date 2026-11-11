using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.HLSL
{
    using CustomHLSL = UnityEditor.VFX.Block.CustomHLSL;

    [TestFixture]
    class VFXCustomHLSLBlockCodeGenerationTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            VFXTestCommon.CloseAllVFXWindow();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        // Generated code ends its lines with the platform newline, unlike the HLSL provided by the tests
        static readonly string NL = Environment.NewLine;

        const string defaultHlslCode =
            "void TestFunction(inout VFXAttributes attributes, in float3 offset, in float speed)\n" +
            "{\n" +
            "  attributes.position += offset;\n" +
            "  attributes.velocity = float3(0, 0, speed);\n" +
            "}";

        [Test]
        public void Check_CustomHLSL_Block_Generated_Code()
        {
            // Arrange
            var blockName = "AutoTest";
            var hlslBlock = VFXCustomHLSLTestHelper.CreateCustomHLSLBlock();
            hlslBlock.SetSettingValue("m_HLSLCode", defaultHlslCode);
            hlslBlock.SetSettingValue("m_BlockName", blockName);

            // Act
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            // Assert
            var function = VFXCustomHLSLTestHelper.GetFunction(hlslBlock);

            var expectedSource = "VFXAttributes att = (VFXAttributes)0;" + NL +
                                      "att.position = position;" + NL +
                                      "att.velocity = velocity;" + NL +
                                     $"{function.GetNameWithHashCode(CustomHLSL.FunctionNameSuffix)}(att, {CustomHLSL.parameterPrefix}offset, {CustomHLSL.parameterPrefix}speed);" + NL +
                                      "position = att.position;" + NL +
                                      "velocity = att.velocity;";
            Assert.AreEqual(expectedSource, hlslBlock.source);
            Assert.IsEmpty(hlslBlock.includes);
            Assert.AreEqual(blockName, hlslBlock.name);

            var expectedCustomCode = $"void {function.GetNameWithHashCode(CustomHLSL.FunctionNameSuffix)}(inout VFXAttributes attributes, in float3 offset, in float speed)" + NL +
                                           "{\n" +
                                           "  attributes.position += offset;\n" +
                                           "  attributes.velocity = float3(0, 0, speed);\n" +
                                           "}" + NL;
            Assert.AreEqual(expectedCustomCode, hlslBlock.customCode);

            var expectedAttributes = new[] { "position", "velocity" };
            Assert.AreEqual(expectedAttributes, hlslBlock.attributes.Select(x => x.attrib.name).ToArray());
        }

        [Test]
        public void Check_CustomHLSL_Block_Use_Shader_File()
        {
            // Arrange
            var blockName = "AutoTest";
            var shaderInclude = VFXTestCommon.CreateShaderFile(defaultHlslCode, out var shaderIncludePath);
            var hlslBlock = VFXCustomHLSLTestHelper.CreateCustomHLSLBlock();
            hlslBlock.SetSettingValue("m_ShaderFile", shaderInclude);
            hlslBlock.SetSettingValue("m_BlockName", blockName);

            // Act
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            // Assert
            var expectedSource = "VFXAttributes att = (VFXAttributes)0;" + NL +
                                 "att.position = position;" + NL +
                                 "att.velocity = velocity;" + NL +
                                 $"TestFunction(att, {CustomHLSL.parameterPrefix}offset, {CustomHLSL.parameterPrefix}speed);" + NL +
                                 "position = att.position;" + NL +
                                 "velocity = att.velocity;";
            Assert.AreEqual(expectedSource, hlslBlock.source);
            var includes = hlslBlock.includes.ToArray();
            Assert.AreEqual(1, includes.Length);
            Assert.AreEqual(shaderIncludePath, includes[0]);

            Assert.AreEqual(blockName, hlslBlock.name);
            Assert.AreEqual(string.Empty, hlslBlock.customCode);

            var expectedAttributes = new[] { "position", "velocity" };
            Assert.AreEqual(expectedAttributes, hlslBlock.attributes.Select(x => x.attrib.name).ToArray());
        }

        [Test]
        public void Check_CustomHLSL_Block_Includes()
        {
            // Arrange
            var includeFilePath = "path/to/include/file.hlsl";
            var hlslCode =
                $"#include \"{includeFilePath}\"\n" +
                "void TestFunction(in VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "    attributes.position = float3(param, param, param);\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph, false /* this test expects shader errors */);

            // Act
            var includes = hlslBlock.includes.ToArray();

            // Assert
            Assert.AreEqual(1, includes.Length);
            Assert.AreEqual(includeFilePath, includes[0]);
            // Delete test asset immediately because it won't compile because the included hlsl file do not exists
            view.Close();
            var path = AssetDatabase.GetAssetPath(graph);
            AssetDatabase.DeleteAsset(path);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Keeps_Include_In_Source()
        {
            var hlslCode = @"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceFillingCurves.hlsl""
void CustomHLSL(inout VFXAttributes attributes)
{
    uint2 code = DecodeMorton2D(attributes.particleId);
    attributes.position = float3((float)code.x, (float)code.y, 0.0f);
}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);

            var vfxTargetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Update);
            vfxTargetContext.label = "Find_Me_In_Generated_Source";
            VFXTestCommon.ReimportVFXGraph(graph);
            yield return null;

            graph.errorManager.GenerateErrors();
            var error = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslBlock).Any();
            Assert.IsFalse(error);
            yield return null;

            bool found = false;
            var resource = graph.GetResource();
            Assert.AreEqual(4, resource.GetShaderSourceCount());
            for (int i = 0; i < resource.GetShaderSourceCount(); ++i)
            {
                var shaderName = resource.GetShaderSourceName(i);
                var source = resource.GetShaderSource(i);
                if (shaderName.Contains(vfxTargetContext.label))
                {
                    Assert.IsTrue(source.Contains("SpaceFillingCurves.hlsl"), "Can't find SpaceFillingCurves.hlsl in " + shaderName);
                    found = true;
                }
                else
                {
                    Assert.IsFalse(source.Contains("SpaceFillingCurves.hlsl"), "Unexpected include of SpaceFillingCurves.hlsl in " + shaderName);
                }
            }
            Assert.IsTrue(found, "Unable to find matching include in generated code.");
        }
    }
}
