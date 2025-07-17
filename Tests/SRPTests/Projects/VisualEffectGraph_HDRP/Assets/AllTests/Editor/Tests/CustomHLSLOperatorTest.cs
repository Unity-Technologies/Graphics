#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;
using CustomHLSL = UnityEditor.VFX.Operator.CustomHLSL;

namespace UnityEditor.VFX.Test
{
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct CustomHLSLOperatorTestType
    {
        public uint test;
        public Vector3 position;
    }

    [TestFixture]
    public class CustomHLSLOperatorTest
    {
        private const string defaultHlslCode =
            "float3 Transform(in float4x4 mat, in float3 vec)" + "\n" +
            "{" + "\n" +
            "    return mat * vec;" + "\n" +
            "}";

        [OneTimeSetUp]
        public void Setup()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [Test]
        public void Check_CustomHLSL_Operator_Generated_Code()
        {
            // Arrange
            var operatorName = "AutoTest";
            var hlslOperator = CreateCustomHLSLOperator();
            hlslOperator.SetSettingValue("m_HLSLCode", defaultHlslCode);
            hlslOperator.SetSettingValue("m_OperatorName", operatorName);

            // Act
            var expressions = CallBuildExpression(hlslOperator, new VFXExpression[] { new VFXValue<Matrix4x4>(Matrix4x4.identity), new VFXValue<Vector3>(Vector3.one) });

            // Assert
            Assert.AreEqual(operatorName, hlslOperator.name);
            Assert.AreEqual(1, expressions.Length);

            var hlslExpression = expressions[0] as VFXExpressionHLSL;
            Assert.IsNotNull(hlslExpression);
            Assert.AreEqual(VFXExpressionOperation.None, hlslExpression.operation);
            Assert.AreEqual(VFXValueType.Float3, hlslExpression.valueType);

            var function = CustomHLSLBlockTest.GetFunction(hlslOperator);
            var expectedCode = $"float3 {function.GetNameWithHashCode(CustomHLSL.ReturnFunctionSuffix)}(in float4x4 mat, in float3 vec)\r\n" +
                               "{\n" +
                               "    return mat * vec;\n" +
                               "}\r\n" +
                               $"float3 {function.GetNameWithHashCode(CustomHLSL.ReturnFunctionSuffix)}_Wrapper(float4x4 mat, float3 vec)\r\n" +
                               "{\r\n" +
                               $"\tfloat3 var_2 = 	Transform_65810E99_{CustomHLSL.ReturnFunctionSuffix}(mat, vec);\r\n" +
                               "\treturn var_2;\r\n" +
                               "}\r\n";
            Assert.AreEqual(expectedCode, hlslExpression.customCode);
        }

        [Test]
        public void Check_CustomHLSL_Operator_Use_Shader_File()
        {
            // Arrange
            var operatorName = "AutoTest";
            var shaderInclude = CustomHLSLBlockTest.CreateShaderFile(defaultHlslCode, out var shaderIncludePath);
            var hlslOperator = CreateCustomHLSLOperator();
            hlslOperator.SetSettingValue("m_ShaderFile", shaderInclude);
            hlslOperator.SetSettingValue("m_OperatorName", operatorName);

            // Act
            var expressions = CallBuildExpression(hlslOperator, new VFXExpression[] { new VFXValue<Matrix4x4>(Matrix4x4.identity), new VFXValue<Vector3>(Vector3.one) });

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
            var expectedGeneratedCode = "float3 Transform_Return_Wrapper(float4x4 mat, float3 vec)\r\n" +
                                        "{\r\n" +
                                        "\tfloat3 var_2 = \tTransform(mat, vec);\r\n" +
                                        "\treturn var_2;\r\n" +
                                        "}\r\n";
            Assert.AreEqual(expectedGeneratedCode, hlslExpression.customCode);
        }


        [UnityTest, Description("Regression for UUM-74518")]
        public IEnumerator Check_CustomHLSL_Operator_Used_In_Two_Expressions_Path([Values(true, false)] bool strictlySameEndExpression, [Values(true, false)] bool useSimpleFunction)
        {
            string trackedCode;
            string hlslCode;
            if (!useSimpleFunction)
            {
                trackedCode = "ConsumeStructuredBuffer<float2>"; //This type of buffer is exotic enough to be only used by this custom hlsl
                hlslCode = $@"
float3 Check_CustomHLSL_Operator_Used_In_Two_Expression_Path({trackedCode} inputBuffer)
{{
    return float3(inputBuffer.Consume(), 2.0f);
}}";

            }
            else
            {
                trackedCode = "float3(87, 78, 51) / float3(14.5f, 13, 8.5)";
                hlslCode = $@"
float3 Check_CustomHLSL_Operator_Used_In_Two_Expression_Path()
{{
    return {trackedCode};
}}";

            }

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            var init = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
            var update = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault();

            Assert.IsNotNull(init);
            Assert.IsNotNull(update);
            Assert.AreNotEqual(init, update);

            var tracker = "Find_Me_In_Generated_Name";
            init.label = tracker + "_Init";
            update.label = tracker + "_Update";

            var setColorInit = ScriptableObject.CreateInstance<Block.SetAttribute>();
            setColorInit.SetSettingValue("attribute", "color");
            init.AddChild(setColorInit);

            var setColorUpdate = ScriptableObject.CreateInstance<Block.SetAttribute>();
            setColorUpdate.SetSettingValue("attribute", "color");
            update.AddChild(setColorUpdate);

            if (strictlySameEndExpression)
            {
                Assert.IsTrue(hlslOperator.outputSlots[0].Link(setColorInit.inputSlots[0]));
                Assert.IsTrue(hlslOperator.outputSlots[0].Link(setColorUpdate.inputSlots[0]));
            }
            else
            {
                Assert.IsTrue(hlslOperator.outputSlots[0].Link(setColorInit.inputSlots[0]));
                Assert.IsTrue(hlslOperator.outputSlots[0][1].Link(setColorUpdate.inputSlots[0][1])); //This will generate a VFXExpressionCombine in endExpression
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph.GetResource().asset));
            yield return null;

            for (int shaderIndex = 0; shaderIndex < graph.GetResource().GetShaderSourceCount(); shaderIndex++)
            {
                var name = graph.GetResource().GetShaderSourceName(shaderIndex);
                if (name.Contains(tracker))
                {
                    var source = graph.GetResource().GetShaderSource(shaderIndex);
                    Assert.IsTrue(source.Contains(trackedCode), "Can't find target function content in " + name);
                    if (!useSimpleFunction)
                        Assert.IsTrue(Regex.IsMatch(source, trackedCode + ".*;"), "Can't find declaration of buffer in " + name);
                }
            }
        }

        [UnityTest, Description("Regression for UUM-69735")]
        public IEnumerator Check_CustomHLSL_Operator_Use_Shader_File_And_ShaderGraph()
        {
            var hlslCode = @"
float3 GetCustomColor(in float3 vec)
{
    return float3(0.1f, 0.2f, 0.3f);
}";
            var shaderInclude = CustomHLSLBlockTest.CreateShaderFile(hlslCode, out var shaderIncludePath);

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);
            hlslOperator.SetSettingValue("m_ShaderFile", shaderInclude);
            hlslOperator.SetSettingValue("m_OperatorName", "GetCustomColor");
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

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

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(vfxAsset));
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
        public IEnumerator Check_CustomHLSL_Operator_Return_Type_Is_Void()
        {
            // Arrange
            var hlslCode =
                "void Transform(in float4x4 mat, in float3 vec)" + "\n" +
                "{" + "\n" +
                "    return mat * vec;" + "\n" +
                "}";

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            var function = CustomHLSLBlockTest.GetFunction(hlslOperator);

            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslOperator).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual($"HLSL function '{function.name}' must return a value", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Out_Parameter_And_Return_Void()
        {
            // Arrange
            var hlslCode =
                "void Transform(out float3 vec)" + "\n" +
                "{" + "\n" +
                "    return float3(1, 2, 3);" + "\n" +
                "}";

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            var hasRegisteredError = false;
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            var function = CustomHLSLBlockTest.GetFunction(hlslOperator);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            Assert.IsFalse(hasRegisteredError);
            Assert.AreEqual(typeof(void), function.returnType);
            Assert.AreEqual(1, function.inputs.Count);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_No_Function()
        {
            // Arrange
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", "toto");

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslOperator).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual("No valid HLSL function has been provided. You should write at least one function that returns a value", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_IncludePath_Relative_Path_Success()
        {
            // Arrange
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            CustomHLSLBlockTest.CreateShaderFile(defaultHlslCode, out var shaderIncludePath);
            shaderIncludePath = Path.GetFileName(shaderIncludePath);

            var hlslCode = string.Format("#include \"{0}\"", shaderIncludePath);
            hlslCode += @"
float3 Transform(float3 a, float3 b)
{
    return a + b;
}";
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

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
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

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
        public IEnumerator Check_CustomHLSL_Operator_Expression_Equality()
        {
            // Arrange
            CustomHLSLBlockTest.CreateShaderFile(defaultHlslCode, out var shaderIncludePath);
            shaderIncludePath = Path.GetFileName(shaderIncludePath);

            var hlslCode = string.Format("#include \"{0}\"", shaderIncludePath);
            hlslCode += @"
float3 Transform(float3 a, float3 b)
{
    return a + b;
}";
            var hlslOperator_A = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator_A.SetSettingValue("m_HLSLCode", hlslCode);

            var hlslOperator_B = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator_B.SetSettingValue("m_HLSLCode", hlslCode);

            MakeSimpleGraphWithCustomHLSL(hlslOperator_A, out var view, out var graph);
            graph.AddChild(hlslOperator_B);

            //Insure parent expression are the same
            var inlineVector3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineVector3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));
            graph.AddChild(inlineVector3);
            Assert.IsTrue(hlslOperator_A.inputSlots[0].Link(inlineVector3.outputSlots[0]));
            Assert.IsTrue(hlslOperator_A.inputSlots[1].Link(inlineVector3.outputSlots[0]));
            Assert.IsTrue(hlslOperator_B.inputSlots[0].Link(inlineVector3.outputSlots[0]));
            Assert.IsTrue(hlslOperator_B.inputSlots[1].Link(inlineVector3.outputSlots[0]));

            yield return null;

            // Assert, different container, same expression
            var outputSlot_A = hlslOperator_A.outputSlots[0];
            var outputSlot_B = hlslOperator_B.outputSlots[0];
            Assert.AreNotEqual(outputSlot_A, outputSlot_B);

            var expression_A = outputSlot_A.GetExpression();
            var expression_B = outputSlot_B.GetExpression();
            Assert.IsInstanceOf<VFXExpressionHLSL>(expression_A);
            Assert.IsInstanceOf<VFXExpressionHLSL>(expression_B);
            Assert.AreEqual(expression_A.GetHashCode(), expression_B.GetHashCode());
            Assert.IsTrue(expression_A.Equals(expression_B));
            Assert.IsTrue(expression_B.Equals(expression_A));
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Twice_Same_Function_Name()
        {
            // Arrange
            var hlslCode = defaultHlslCode + "\n" + defaultHlslCode;
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslOperator).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Warning, report.type);
            Assert.AreEqual("Multiple functions with same name 'Transform' are declared, only the first one can be selected", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Unknown_Parameter_Type()
        {
            // Arrange
            var parameterType = "xxxx";
            var hlslCode =
                $"float3 Transform(in {parameterType} mat, in float3 vec)" + "\n" +
                "{" + "\n" +
                "    return mat * vec;" + "\n" +
                "}";

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslOperator).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual($"Unknown parameter type '{parameterType}'", report.description);
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

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            var vfxTargetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Update);
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            Assert.IsNotNull(blockAttributeDesc);
            var blockAttribute = blockAttributeDesc.variant.CreateInstance() as Block.SetAttribute;
            blockAttribute.SetSettingValue("attribute", "position");
            vfxTargetContext.AddChild(blockAttribute);

            vfxTargetContext.label = "Find_Me_In_Generated_Source";

            Assert.IsTrue(blockAttribute.inputSlots[0].Link(hlslOperator.outputSlots[0]));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
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
                CustomHLSLBlockTest.CreateShaderFile(string.Empty, out var shaderIncludePath);
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

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            var vfxTargetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Update);
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            Assert.IsNotNull(blockAttributeDesc);
            var blockAttribute = blockAttributeDesc.variant.CreateInstance() as Block.SetAttribute;
            blockAttribute.SetSettingValue("attribute", "position");
            vfxTargetContext.AddChild(blockAttribute);
            vfxTargetContext.label = "Find_Me_In_Generated_Source";

            Assert.IsTrue(blockAttribute.inputSlots[0].Link(hlslOperator.outputSlots[0]));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
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

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Unsupported_Attributes()
        {
            // Arrange
            var attributesName = "position";
            var hlslCode =
                $"float3 Transform(in VFXAttributes attributes)" + "\n" +
                 "{" + "\n" +
                $"    return float3(0, attributes.{attributesName}, 0);" + "\n" +
                 "}";
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslOperator).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual($"No VFXAttributes can be used here:\n\t{string.Join("\n\t", attributesName)}", report.description);
        }

        public enum Check_CustomHLSL_Block_Works_In_Context_Case
        {
            Initialize,
            Update,
            Output,
            OutputSG
        }

        public static Array k_Check_CustomHLSL_Block_Works_In_Context_Case = Enum.GetValues(typeof(Check_CustomHLSL_Block_Works_In_Context_Case));

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Works_In_Context([ValueSource(nameof(k_Check_CustomHLSL_Block_Works_In_Context_Case))] Check_CustomHLSL_Block_Works_In_Context_Case target)
        {
            var hlslCode =
                @"float3 FindMe_In_Generated_Source(float3 position, in float scale)
{
    return position + float3(1,2,3)*scale;
}";

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            if (target == Check_CustomHLSL_Block_Works_In_Context_Case.OutputSG)
            {
                var previousOutput = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Output);
                var sgOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
                sgOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
                sgOutput.SetSettingValue("m_Shading", new ParticleShadingShaderGraph());

                var parentContext = previousOutput.inputFlowSlot.First().link.First().context;
                sgOutput.LinkFrom(parentContext);

                previousOutput.UnlinkAll();
                graph.RemoveChild(previousOutput);
                graph.AddChild(sgOutput);
            }

            VFXContextType contextType;
            switch (target)
            {
                case Check_CustomHLSL_Block_Works_In_Context_Case.Initialize: contextType = VFXContextType.Init; break;
                case Check_CustomHLSL_Block_Works_In_Context_Case.Update: contextType = VFXContextType.Update; break;
                case Check_CustomHLSL_Block_Works_In_Context_Case.Output:
                case Check_CustomHLSL_Block_Works_In_Context_Case.OutputSG: contextType = VFXContextType.Output; break;
                default: throw new NotImplementedException();
            }

            var vfxTargetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == contextType);
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            var blockAttribute = blockAttributeDesc.variant.CreateInstance() as Block.SetAttribute;
            blockAttribute.SetSettingValue("attribute", "position");
            vfxTargetContext.AddChild(blockAttribute);
            Assert.IsTrue(blockAttribute.inputSlots[0].Link(hlslOperator.outputSlots[0]));

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            bool foundCustomFunction = false;
            var vfx = graph.visualEffectResource;
            for (int sourceIndex = 0; sourceIndex < vfx.GetShaderSourceCount(); ++sourceIndex)
            {
                var source = vfx.GetShaderSource(sourceIndex);
                if (source.Contains("FindMe_In_Generated_Source", StringComparison.InvariantCulture))
                {
                    foundCustomFunction = true;
                    break;
                }
            }
            Assert.IsTrue(foundCustomFunction);
            yield return null;
        }

        public struct BufferCase
        {
            public string declaration;
            public string implementation;

            public override string ToString()
            {
                return declaration;
            }
        }

        public static readonly BufferCase[] kSampleBufferCase =
        {
            new() { declaration = "AppendStructuredBuffer<uint>", implementation = "inputBuffer.Append(0u); localValue = 0.1f;" },
            new() { declaration = "ConsumeStructuredBuffer<float3>", implementation = "localValue = inputBuffer.Consume();" },
            new() { declaration = "RWByteAddressBuffer", implementation = "inputBuffer.Store3(0, (float3)1.0f); localValue = asfloat(inputBuffer.Load3(0));" },
            new() { declaration = "ByteAddressBuffer", implementation = "localValue = asfloat(inputBuffer.Load3(0));" },
            new() { declaration = $"StructuredBuffer<{nameof(CustomHLSLOperatorTestType)}>", implementation = "localValue = inputBuffer[0].position;" },
            new() { declaration = "StructuredBuffer<uint3>", implementation = "localValue = asfloat(inputBuffer[0]);" },
            new() { declaration = "StructuredBuffer<float3>", implementation = "localValue = inputBuffer[0];" },
            new() { declaration = "RWStructuredBuffer<float3>", implementation = "inputBuffer[0].x += 0.0f; localValue = inputBuffer[0];" },

            new() { declaration = "RWTexture2D<uint>", implementation = "InterlockedAdd(inputBuffer[(int2)0], 1u); localValue = 0.1f;" },
            new() { declaration = "RWTexture2D<float3>", implementation = "inputBuffer[(int2)0].xyz = (float3)1.0f; localValue = 0.1f;" },
            new() { declaration = "Texture2D<float3>", implementation = "localValue = inputBuffer[(int2)0].rgb;" },
            new() { declaration = "Texture2D", implementation = "localValue = inputBuffer[(int2)0].rgb;" },

            new() { declaration = "RWTexture1D<uint>", implementation = "inputBuffer[0] = 1u; localValue = 0.1f;" },
            new() { declaration = "RWTexture1DArray<float>", implementation = "inputBuffer[(uint2)0] = 0u; localValue = 0.1f;" },
            new() { declaration = "Texture1D", implementation = "localValue = inputBuffer[(uint)0];" }
        };

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Buffer([ValueSource(nameof(kSampleBufferCase))] BufferCase bufferCase)
        {
            var hlslCode = new StringBuilder();
            hlslCode.AppendLine($"float3 Check_Sample_Buffer(in {bufferCase.declaration} inputBuffer)");
            hlslCode.AppendLine("{");
            hlslCode.AppendLine("    float3 localValue = (float3)0.0f;");
            hlslCode.AppendLine($"    {bufferCase.implementation}");
            hlslCode.AppendLine("    return localValue;");
            hlslCode.AppendLine("}");

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode.ToString());
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            var vfxTargetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Update);
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            Assert.IsNotNull(blockAttributeDesc);
            var blockAttribute = blockAttributeDesc.variant.CreateInstance() as Block.SetAttribute;
            blockAttribute.SetSettingValue("attribute", "position");
            vfxTargetContext.AddChild(blockAttribute);
            Assert.IsTrue(blockAttribute.inputSlots[0].Link(hlslOperator.outputSlots[0]));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            for (int i = 0; i < 4; ++i)
                yield return null;
        }

        [UnityTest, Description("Repro case UUM-66018")]
        public IEnumerator Check_Multiple_Usage_Buffer()
        {
            string Check_Multiple_Usage_Buffer_Get_Source(VisualEffectResource vfx)
            {
                var sourceCount = vfx.GetShaderSourceCount();
                Assert.AreNotEqual(0, sourceCount);
                for (int index = 0; index < sourceCount; index++)
                {
                    if (vfx.GetShaderSourceName(index).Contains("Find_Me"))
                        return vfx.GetShaderSource(index);
                }
                return null;
            }

            void Check_Multiple_Usage_Buffer_Sanity_Check(string source)
            {
                Assert.IsTrue(source.Contains("ByteAddressBuffer buffer_a;"));
                Assert.IsTrue(source.Contains("StructuredBuffer<uint> _buffer_b;"));
                Assert.IsTrue(Regex.IsMatch(source, "void mySamplingOfUAV_In_Block_.*\\(inout VFXAttributes attributes, in ByteAddressBuffer buffer\\)"));
                Assert.IsTrue(Regex.IsMatch(source, "void myOtherSamplingOfStructured_In_Block_.*\\(inout VFXAttributes attributes, in StructuredBuffer<uint> buffer\\)"));
                Assert.IsTrue(Regex.IsMatch(source, "float mySamplingOfUAV_In_Operator_.*\\(in ByteAddressBuffer buffer\\)"));
            }

            string vfxPath = "Assets/AllTests/Editor/Tests/Repro_UUM_66018.vfx";
            Assert.IsTrue(AssetDatabase.AssetPathExists(vfxPath));

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            var vfx = asset.GetResource();

            vfx.GetOrCreateGraph().SetCompilationMode(VFXCompilationMode.Edition);
            vfx.GetOrCreateGraph().CompileAndUpdateAsset(asset);
            yield return null;

            var sourceEdition = Check_Multiple_Usage_Buffer_Get_Source(vfx);
            Assert.IsNotNull(sourceEdition);

            vfx.GetOrCreateGraph().SetCompilationMode(VFXCompilationMode.Runtime);
            vfx.GetOrCreateGraph().CompileAndUpdateAsset(asset);
            yield return null;

            var sourceRuntime = Check_Multiple_Usage_Buffer_Get_Source(vfx);
            Assert.IsNotNull(sourceRuntime);

            Assert.AreNotEqual(sourceRuntime, sourceEdition);

            Check_Multiple_Usage_Buffer_Sanity_Check(sourceRuntime);
            Check_Multiple_Usage_Buffer_Sanity_Check(sourceEdition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Check_Diverging_Usage_Buffer([Values(true, false)] bool legalUsage)
        {
            var hlslCodeRW =
@"float3 Read_In_RWBuffer(float3 value, RWBuffer<float3> buffer)
{
    return buffer[0u];
}";
            var hlslOperatorWrite = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperatorWrite.SetSettingValue("m_HLSLCode", hlslCodeRW);

            var hlslCode =
@"float3 Read_In_Buffer(float3 value, Buffer<float3> buffer)
{
    return buffer[0u];
}";
            var hlslOperatorRead = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperatorRead.SetSettingValue("m_HLSLCode", hlslCode);

            var graph = VFXTestCommon.CreateGraph_And_System();

            var init = graph.children.OfType<VFXBasicInitialize>().First();
            var update = graph.children.OfType<VFXBasicUpdate>().First();

            init.label = "Find_Me_Init";
            update.label = "Find_Me_Update";

            VFXContext first, second;
            if (legalUsage)
            {
                first = init;
                second = update;
            }
            else
            {
                first = update;
                second = update;
            }

            graph.AddChild(hlslOperatorWrite);
            graph.AddChild(hlslOperatorRead);

            var parameter = ScriptableObject.CreateInstance<VFXParameter>();
            parameter.Init(typeof(GraphicsBuffer));
            graph.AddChild(parameter);

            Assert.IsTrue(parameter.outputSlots[0].Link(hlslOperatorWrite.inputSlots[1]));
            Assert.IsTrue(parameter.outputSlots[0].Link(hlslOperatorRead.inputSlots[1]));

            var blockSetColor = ScriptableObject.CreateInstance<Block.SetAttribute>();
            blockSetColor.SetSettingValue("attribute", "color");
            first.AddChild(blockSetColor);
            Assert.IsTrue(hlslOperatorWrite.outputSlots[0].Link(blockSetColor.inputSlots[0]));

            blockSetColor = ScriptableObject.CreateInstance<Block.SetAttribute>();
            blockSetColor.SetSettingValue("attribute", "color");
            second.AddChild(blockSetColor);
            Assert.IsTrue(hlslOperatorRead.outputSlots[0].Link(blockSetColor.inputSlots[0]));

            var vfxPath = AssetDatabase.GetAssetPath(graph);
            if (legalUsage)
            {
                AssetDatabase.ImportAsset(vfxPath);
                for (int i = 0; i < 4; ++i)
                    yield return null;

                var resource = graph.GetResource();
                bool correctInit = false;
                bool correctUpdate = false;
                for (int shaderIndex = 0; shaderIndex < resource.GetShaderSourceCount(); ++shaderIndex)
                {
                    var shaderName = resource.GetShaderSourceName(shaderIndex);
                    if (shaderName.Contains(init.label)
                        && resource.GetShaderSource(shaderIndex).Contains("Read_In_RWBuffer")
                        && !resource.GetShaderSource(shaderIndex).Contains("Read_In_Buffer"))
                    {
                        correctInit = true;
                    }

                    if (shaderName.Contains(update.label)
                        && !resource.GetShaderSource(shaderIndex).Contains("Read_In_RWBuffer")
                        && resource.GetShaderSource(shaderIndex).Contains("Read_In_Buffer"))
                    {
                        correctUpdate = true;
                    }
                }
                Assert.IsTrue(correctInit, "Unexpected Init ComputeShader");
                Assert.IsTrue(correctUpdate, "Unexpected Update ComputeShader");
            }
            else
            {
                using var customLogHandler = new CustomLogHandler();
                customLogHandler.ExpectedLog(LogType.Error, "Unity cannot compile the VisualEffectAsset at path");
                AssetDatabase.ImportAsset(vfxPath);
            }

            yield return null;
        }

        [UnityTest, Description("Cover UUM-83676")]
        public IEnumerator Check_CustomHLSL_Operator_Multiple_Out_Parameters()
        {
            // Arrange
            var hlslCode =
                "void Transform(out float first, out float second)" + "\n" +
                "{" + "\n" +
                "    first = 1;" + "\n" +
                "    second = 2;" + "\n" +
                "}";

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            var initializeContext = graph.children.OfType<VFXBasicInitialize>().Single();
            var setPositionBlock = initializeContext.children.Single();
            yield return null;

            setPositionBlock.inputSlots[0][0][0].Link(hlslOperator.outputSlots[0]);
            setPositionBlock.inputSlots[0][0][1].Link(hlslOperator.outputSlots[1]);
            view.graphView.controller.ApplyChanges();

            foreach (var y in VFXTestCommon.CheckCompilation(graph))
            {
                yield return y;
            }
        }

        [UnityTest, Description("Cover UUM-83676")]
        public IEnumerator Check_CustomHLSL_File_Operator_Multiple_Out_Parameters()
        {
            // Arrange
            var hlslCode =
                "void Transform(out float first, out float second)" + "\n" +
                "{" + "\n" +
                "    first = 1;" + "\n" +
                "    second = 2;" + "\n" +
                "}";

            var shaderFile = CustomHLSLBlockTest.CreateShaderFile(hlslCode, out var shaderIncludePath);
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_ShaderFile", shaderFile);

            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            var initializeContext = graph.children.OfType<VFXBasicInitialize>().Single();
            var setPositionBlock = initializeContext.children.Single();
            yield return null;

            setPositionBlock.inputSlots[0][0][0].Link(hlslOperator.outputSlots[0]);
            setPositionBlock.inputSlots[0][0][1].Link(hlslOperator.outputSlots[1]);
            view.graphView.controller.ApplyChanges();

            foreach (var y in VFXTestCommon.CheckCompilation(graph))
            {
                yield return y;
            }
        }

        private VFXExpression[] CallBuildExpression(CustomHLSL hlslOperator, VFXExpression[] parentExpressions)
        {
            var methodInfo = hlslOperator.GetType().GetMethod("BuildExpression", BindingFlags.Instance | BindingFlags.NonPublic);
            return (VFXExpression[])methodInfo.Invoke(hlslOperator, new object[] { parentExpressions });
        }

        private void MakeSimpleGraphWithCustomHLSL(CustomHLSL hlslOperator, out VFXViewWindow view, out VFXGraph graph)
        {
            graph = VFXTestCommon.CreateGraph_And_System();
            view = VFXViewWindow.GetWindow(graph, true, true);
            view.LoadResource(graph.visualEffectResource);

            graph.AddChild(hlslOperator);
            view.graphView.OnSave();
        }

        private CustomHLSL CreateCustomHLSLOperator()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            graph.AddChild(hlslOperator);

            return hlslOperator;
        }
    }
}
#endif
