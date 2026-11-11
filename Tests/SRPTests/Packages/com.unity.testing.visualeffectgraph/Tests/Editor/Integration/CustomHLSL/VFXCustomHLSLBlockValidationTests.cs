using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.HLSL
{
    using CustomHLSL = UnityEditor.VFX.Block.CustomHLSL;

    [TestFixture]
    class VFXCustomHLSLBlockValidationTests
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

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Return_Type_Not_Void()
        {
            // Arrange
            var hlslCode =
                "float TestFunction(inout VFXAttributes attributes, in float3 offset, in float speedFactor)\n" +
                "{\n" +
                "}";

            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph, false /* this test expects shader errors */);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslBlock).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual("HLSL function 'TestFunction' must return a 'void' type", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_No_Function()
        {
            // Arrange
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", "toto");

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph, false /* this test expects shader errors */);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslBlock).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual("No valid HLSL function has been provided. You should write at least one function that returns a value", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Twice_Same_Function_Name()
        {
            // Arrange
            var functionName = "TestFunction";
            var hlslCode =
                $"void {functionName}(inout VFXAttributes attributes, in float3 offset, in float speedFactor)\n" +
                "{\n" +
                "attributes.size = 1;\n" +
                "}\n" +
                $"void {functionName}(inout VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "attributes.alive = false;\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslBlock).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Warning, report.type);
            Assert.AreEqual($"Multiple functions with same name '{functionName}' are declared, only the first one can be selected", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Unknown_Parameter_Type()
        {
            // Arrange
            var parameterType = "xxxx";
            var hlslCode =
                $"void TestFunction(inout VFXAttributes attributes, in {parameterType} offset)\n" +
                "{\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph, false /* this test expects shader errors */);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslBlock).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual($"Unknown parameter type '{parameterType}'", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Missing_VFXAttributes()
        {
            // Arrange
            var hlslCode =
                "void TestFunction(in float param)\n" +
                "{\n" +
                "    float3 p = float3(param, param, param);\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph, false /* this test expects shader errors */);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslBlock).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual("Missing `VFXAttributes attributes` as function's parameter", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_VFXAttributes_Wrong_Access_Modifier()
        {
            // Arrange
            var hlslCode =
                "void TestFunction(in VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "    attributes.position = float3(param, param, param);\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslBlock).Single();
            Assert.IsNotNull(report);
            Assert.AreEqual(VFXErrorType.Error, report.type);
            Assert.AreEqual("Missing `inout` access modifier before the VFXAttributes type.\nNeeded because your code writes to at least one attribute.", report.description);
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_VFXAttributes_Without_Access_Modifier()
        {
            // Arrange
            var hlslCode =
                "void TestFunction(inout VFXAttributes attributes, float param)\n" +
                "{\n" +
                "    attributes.position = float3(param, param, param);\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);
            yield return null;

            // Act
            graph.errorManager.GenerateErrors();

            // Assert
            var report = graph.errorManager.errorReporter.GetDirtyModelErrors(hlslBlock);
            Assert.IsFalse(report.Any());

            //Check input slot contents
            Assert.AreEqual(1, hlslBlock.inputSlots.Count);
            Assert.AreEqual($"{CustomHLSL.parameterPrefix}param", hlslBlock.inputSlots[0].name);
            Assert.AreEqual(VFXValueType.Float, hlslBlock.inputSlots[0].valueType);
        }
    }
}
