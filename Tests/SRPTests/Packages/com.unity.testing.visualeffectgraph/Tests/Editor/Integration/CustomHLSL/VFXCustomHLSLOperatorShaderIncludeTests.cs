using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.HLSL
{
    using CustomHLSL = UnityEditor.VFX.Operator.CustomHLSL;

    [TestFixture]
    public class VFXCustomHLSLOperatorShaderIncludeTests
    {
        // Generated code ends its lines with the platform newline, unlike the HLSL provided by the tests
        static readonly string NL = Environment.NewLine;

        private const string defaultHlslCode =
            "float3 Transform(in float4x4 mat, in float3 vec)" + "\n" +
            "{" + "\n" +
            "    return mat * vec;" + "\n" +
            "}";

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

        [Test]
        public void Check_CustomHLSL_Operator_Use_Shader_File()
        {
            // Arrange
            var operatorName = "AutoTest";
            var shaderInclude = VFXTestCommon.CreateShaderFile(defaultHlslCode, out var shaderIncludePath);
            var hlslOperator = VFXCustomHLSLTestHelper.CreateCustomHLSLOperator();
            hlslOperator.SetSettingValue("m_ShaderFile", shaderInclude);
            hlslOperator.SetSettingValue("m_OperatorName", operatorName);

            // Act
            var expressions = VFXCustomHLSLTestHelper.CallBuildExpression(hlslOperator, new VFXExpression[] { new VFXValue<Matrix4x4>(Matrix4x4.identity), new VFXValue<Vector3>(Vector3.one) });

            // Assert
            Assert.AreEqual(operatorName, hlslOperator.name);
            Assert.AreEqual(1, expressions.Length);

            var hlslExpression = expressions[0] as VFXExpressionHLSL;
            Assert.IsNotNull(hlslExpression);

            var includes = hlslExpression.includes.ToArray();
            Assert.AreEqual(1, includes.Length);
            Assert.AreEqual( shaderIncludePath, includes[0]);

            Assert.AreEqual(VFXExpressionOperation.None, hlslExpression.operation);
            Assert.AreEqual(VFXValueType.Float3, hlslExpression.valueType);
            var expectedGeneratedCode = "float3 Transform_Return_Wrapper(float4x4 mat, float3 vec)" + NL +
                                        "{" + NL +
                                        "\tfloat3 var_2 = \tTransform(mat, vec);" + NL +
                                        "\treturn var_2;" + NL +
                                        "}" + NL;
            Assert.AreEqual(expectedGeneratedCode, hlslExpression.customCode);
        }

        [UnityTest, Description("Regression for UUM-69735")]
        public IEnumerator Check_CustomHLSL_Operator_Use_Shader_File_And_ShaderGraph()
        {
            var hlslCode = @"
float3 GetCustomColor(in float3 vec)
{
    return float3(0.1f, 0.2f, 0.3f);
}";
            var shaderInclude = VFXTestCommon.CreateShaderFile(hlslCode, out var shaderIncludePath);

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);
            hlslOperator.SetSettingValue("m_ShaderFile", shaderInclude);
            hlslOperator.SetSettingValue("m_OperatorName", "GetCustomColor");
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            var vfxAsset = graph.GetResource().asset;
            var outputToReplace = graph.children.OfType<VFXContext>().First(o => o.contextType.HasFlag(VFXContextType.Output));
            var shaderGraphVariant = VFXLibrary.GetContexts().First(o => o.model is VFXComposedParticleOutput);

            var newShaderGraphOutput = shaderGraphVariant.CreateInstance();
            graph.AddChild(newShaderGraphOutput);
            Assert.IsTrue(newShaderGraphOutput.GetSetting("shaderGraph").valid);

            var tracker = "Find_Me_In_Generated_Name";
            newShaderGraphOutput.label = tracker;
            newShaderGraphOutput.LinkFrom(outputToReplace.inputFlowSlot[0].link.First().context);
            outputToReplace.UnlinkAll();
            graph.RemoveChild(outputToReplace);

            var blockSetColor = ScriptableObject.CreateInstance<Block.SetAttribute>();
            blockSetColor.SetSettingValue("attribute", "color");
            newShaderGraphOutput.AddChild(blockSetColor);
            Assert.IsTrue(hlslOperator.outputSlots[0].Link(blockSetColor.inputSlots[0]));

            var blockOrientCameraPlane = ScriptableObject.CreateInstance<Block.Orient>();
            blockOrientCameraPlane.SetSettingValue("faceRay", true);
            blockOrientCameraPlane.SetSettingValue("mode", Block.Orient.Mode.FaceCameraPlane);
            newShaderGraphOutput.AddChild(blockOrientCameraPlane);
            var defineToFind = blockOrientCameraPlane.defines.First();

            VFXTestCommon.ReimportVFXGraph(graph);
            yield return null;

            string source = null;
            for (int shaderIndex = 0; shaderIndex < graph.GetResource().GetShaderSourceCount(); shaderIndex++)
            {
                var name = graph.GetResource().GetShaderSourceName(shaderIndex);
                if (name.Contains(tracker))
                {
                    source = graph.GetResource().GetShaderSource(shaderIndex);
                    break;
                }
            }
            Assert.IsNotNull(source);
            Assert.IsTrue(source.Contains(shaderIncludePath, StringComparison.Ordinal));
            Assert.IsTrue(source.Contains(defineToFind, StringComparison.Ordinal));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_IncludePath_Relative_Path_Success()
        {
            // Arrange
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            VFXTestCommon.CreateShaderFile(defaultHlslCode, out var shaderIncludePath);
            shaderIncludePath = Path.GetFileName(shaderIncludePath);

            var hlslCode = string.Format("#include \"{0}\"", shaderIncludePath);
            hlslCode += @"
float3 Transform(float3 a, float3 b)
{
    return a + b;
}";
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslOperator);
            Assert.IsTrue(!report.Any());
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_IncludePath_Fail()
        {
            // Arrange
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();

            var hlslCode =
                @"#include ""FILE_WHICH_DOESNT_EXIST.hlsl""
float3 Transform(float3 a, float3 b)
{
    return a + b;
}
";
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslOperator).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual("Couldn't open include file 'FILE_WHICH_DOESNT_EXIST.hlsl'.", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_With_Include()
        {
            var hlslCode = @"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceFillingCurves.hlsl""
float3 DecodeMorton(in uint code)
{
    return float3(DecodeMorton2D(code), 0.0f);
}";

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            var vfxTargetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Update);
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            Assert.IsNotNull(blockAttributeDesc);
            var blockAttribute = blockAttributeDesc.variant.CreateInstance() as Block.SetAttribute;
            blockAttribute.SetSettingValue("attribute", "position");
            vfxTargetContext.AddChild(blockAttribute);

            vfxTargetContext.label = "Find_Me_In_Generated_Source";

            Assert.IsTrue(blockAttribute.inputSlots[0].Link(hlslOperator.outputSlots[0]));
            VFXTestCommon.ReimportVFXGraph(graph);
            yield return null;

            graph.errorManager.GenerateErrors();
            var error = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslOperator).Any();
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

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_With_Include_Order_Is_Kept()
        {
            var hlslCode = new StringBuilder();

            var includePathList = new List<string>();
            for (int includeIndex = 0; includeIndex < 8; ++includeIndex)
            {
                VFXTestCommon.CreateShaderFile(string.Empty, out var shaderIncludePath);
                includePathList.Add(shaderIncludePath);
                hlslCode.AppendFormat("#include \"{0}\"", shaderIncludePath);
                hlslCode.AppendLine();
            }

            hlslCode.Append(@"
float3 DummyFunction()
{
    return float3(1,2,3);
}");
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode.ToString());

            var readIncludes = hlslOperator.includes.ToList();
            Assert.IsTrue(includePathList.SequenceEqual(readIncludes), "Unable to parse correctly multiple includes");

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            var vfxTargetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Update);
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            Assert.IsNotNull(blockAttributeDesc);
            var blockAttribute = blockAttributeDesc.variant.CreateInstance() as Block.SetAttribute;
            blockAttribute.SetSettingValue("attribute", "position");
            vfxTargetContext.AddChild(blockAttribute);
            vfxTargetContext.label = "Find_Me_In_Generated_Source";

            Assert.IsTrue(blockAttribute.inputSlots[0].Link(hlslOperator.outputSlots[0]));
            VFXTestCommon.ReimportVFXGraph(graph);
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
                    int previousIndex = -1;
                    foreach (var include in includePathList)
                    {
                        var currentIndex = source.IndexOf(include, StringComparison.InvariantCulture);
                        if (currentIndex > previousIndex)
                        {
                            previousIndex = currentIndex;
                        }
                        else
                        {
                            Assert.IsTrue(false, "Unexpected include ordering at: " + include);
                        }
                    }

                    found = true;
                }
            }
            Assert.IsTrue(found, "Unable to find matching include in generated code.");
        }
    }
}
