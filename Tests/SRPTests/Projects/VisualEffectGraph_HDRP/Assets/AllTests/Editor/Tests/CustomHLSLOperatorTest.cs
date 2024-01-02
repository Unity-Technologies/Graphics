#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;
using CustomHLSL = UnityEditor.VFX.Operator.CustomHLSL;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class CustomHLSLOperatorTest
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
            var expectedCode = $"float3 {function.GetNameWithHashCode()}(in float4x4 mat, in float3 vec)\r\n" +
                                     "{\n" +
                                     "    return mat * vec;\n" +
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
            Assert.AreEqual(VFXExpressionOperation.None, hlslExpression.operation);
            Assert.AreEqual(VFXValueType.Float3, hlslExpression.valueType);
            Assert.AreEqual($"#include \"{shaderIncludePath}\"\n", hlslExpression.customCode);
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

            var hasRegisteredError = false;
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            var function = CustomHLSLBlockTest.GetFunction(hlslOperator);
            view.graphView.errorManager.onRegisterError += (model, origin, error, errorType, description) => CustomHLSLBlockTest.CheckErrorFeedback(
                model,
                errorType,
                description,
                hlslOperator,
                $"HLSL function '{function.name}' must return a value",
                VFXErrorType.Error,
                ref hasRegisteredError);

            yield return null;

            // Act
            VFXViewWindow.RefreshErrors(hlslOperator);

            // Assert
            Assert.IsTrue(hasRegisteredError);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_No_Function()
        {
            // Arrange
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", "toto");

            var hasRegisteredError = false;
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            view.graphView.errorManager.onRegisterError += (model, origin, error, errorType, description) => CustomHLSLBlockTest.CheckErrorFeedback(
                model,
                errorType,
                description,
                hlslOperator,
                "No valid HLSL function has been provided. You should write at least one function that returns a value",
                VFXErrorType.Error,
                ref hasRegisteredError);

            yield return null;

            // Act
            VFXViewWindow.RefreshErrors(hlslOperator);

            // Assert
            Assert.IsTrue(hasRegisteredError);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Twice_Same_Function_Name()
        {
            // Arrange
            var functionName = "Transform";
            var hlslCode = defaultHlslCode + "\n" + defaultHlslCode;
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            var hasRegisteredError = false;
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            view.graphView.errorManager.onRegisterError += (model, origin, error, errorType, description) => CustomHLSLBlockTest.CheckErrorFeedback(
                model,
                errorType,
                description,
                hlslOperator,
                $"Multiple functions with same name '{functionName}' are declared, only the first one can be selected",
                VFXErrorType.Warning,
                ref hasRegisteredError);

            yield return null;

            // Act
            VFXViewWindow.RefreshErrors(hlslOperator);

            // Assert
            Assert.IsTrue(hasRegisteredError);
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

            var hasRegisteredError = false;
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            view.graphView.errorManager.onRegisterError += (model, origin, error, errorType, description) => CustomHLSLBlockTest.CheckErrorFeedback(
                model,
                errorType,
                description,
                hlslOperator,
                $"Unknown parameter type '{parameterType}'",
                VFXErrorType.Error,
                ref hasRegisteredError);

            yield return null;

            // Act
            VFXViewWindow.RefreshErrors(hlslOperator);

            // Assert
            Assert.IsTrue(hasRegisteredError);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Texture2D_Type_Used()
        {
            // Arrange
            var paramName = "texture";
            var hlslCode =
                $"float3 Transform(in Texture2D {paramName})" + "\n" +
                "{" + "\n" +
                "    return float3(0, 0, 0);" + "\n" +
                "}";
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode);

            var hasRegisteredError = false;
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            view.graphView.errorManager.onRegisterError += (model, origin, error, errorType, description) => CustomHLSLBlockTest.CheckErrorFeedback(
                model,
                errorType,
                description,
                hlslOperator,
                $"The function parameter '{paramName}' is of type Texture2D.\nPlease use VFXSampler2D type instead (see documentation)",
                VFXErrorType.Error,
                ref hasRegisteredError);

            yield return null;

            // Act
            VFXViewWindow.RefreshErrors(hlslOperator);

            // Assert
            Assert.IsTrue(hasRegisteredError);
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

            var hasRegisteredError = false;
            MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
            view.graphView.errorManager.onRegisterError += (model, origin, error, errorType, description) => CustomHLSLBlockTest.CheckErrorFeedback(
                model,
                errorType,
                description,
                hlslOperator,
                $"No VFXAttributes can be used here:\n\t{string.Join("\n\t", attributesName)}",
                VFXErrorType.Error,
                ref hasRegisteredError);

            yield return null;

            // Act
            VFXViewWindow.RefreshErrors(hlslOperator);

            // Assert
            Assert.IsTrue(hasRegisteredError);
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
