using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.HLSL
{
    using CustomHLSL = UnityEditor.VFX.Operator.CustomHLSL;

    [TestFixture]
    public class VFXCustomHLSLOperatorCodeGenerationTests
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
        public void Check_CustomHLSL_Operator_Generated_Code()
        {
            // Arrange
            var operatorName = "AutoTest";
            var hlslOperator = VFXCustomHLSLTestHelper.CreateCustomHLSLOperator();
            hlslOperator.SetSettingValue("m_HLSLCode", defaultHlslCode);
            hlslOperator.SetSettingValue("m_OperatorName", operatorName);

            // Act
            var expressions = VFXCustomHLSLTestHelper.CallBuildExpression(hlslOperator, new VFXExpression[] { new VFXValue<Matrix4x4>(Matrix4x4.identity), new VFXValue<Vector3>(Vector3.one) });

            // Assert
            Assert.AreEqual(operatorName, hlslOperator.name);
            Assert.AreEqual(1, expressions.Length);

            var hlslExpression = expressions[0] as VFXExpressionHLSL;
            Assert.IsNotNull(hlslExpression);
            Assert.AreEqual(VFXExpressionOperation.None, hlslExpression.operation);
            Assert.AreEqual(VFXValueType.Float3, hlslExpression.valueType);

            var function = VFXCustomHLSLTestHelper.GetFunction(hlslOperator);
            var expectedCode = $"float3 {function.GetNameWithHashCode(CustomHLSL.ReturnFunctionSuffix)}(in float4x4 mat, in float3 vec)" + NL +
                               "{\n" +
                               "    return mat * vec;\n" +
                               "}" + NL +
                               $"float3 {function.GetNameWithHashCode(CustomHLSL.ReturnFunctionSuffix)}_Wrapper(float4x4 mat, float3 vec)" + NL +
                               "{" + NL +
                               $"\tfloat3 var_2 = 	Transform_234976C1_{CustomHLSL.ReturnFunctionSuffix}(mat, vec);" + NL +
                               "\treturn var_2;" + NL +
                               "}" + NL;
            Assert.AreEqual(expectedCode, hlslExpression.customCode);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Expression_Equality()
        {
            // Arrange
            VFXTestCommon.CreateShaderFile(defaultHlslCode, out var shaderIncludePath);
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

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator_A, out var view, out var graph);
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
    }
}
