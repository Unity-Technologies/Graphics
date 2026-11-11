using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.HLSL
{
    using CustomHLSL = UnityEditor.VFX.Block.CustomHLSL;

    [TestFixture]
    class VFXCustomHLSLBlockCompilationTests
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

        public static readonly VFXCustomHLSLBufferCase[] kSampleBufferCase = VFXCustomHLSLBufferTestCases.kSampleBufferCase;

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Buffer([ValueSource(nameof(kSampleBufferCase))] VFXCustomHLSLBufferCase bufferCase)
        {
            if (bufferCase.declaration == "RWTexture2D<uint>" && UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal)
                Assert.Ignore("Texture atomics are not supported in Metal");

            var hlslCode = new StringBuilder();
            hlslCode.AppendLine($"void Check_Sample_Buffer(inout VFXAttributes attributes, in {bufferCase.declaration} inputBuffer)");
            hlslCode.AppendLine("{");
            hlslCode.AppendLine("    float3 localValue = (float3)0.0f;");
            hlslCode.AppendLine($"    {bufferCase.implementation}");
            hlslCode.AppendLine("    attributes.position = localValue;");
            hlslCode.AppendLine("}");

            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode.ToString());
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);

            VFXTestCommon.ReimportVFXGraph(graph);
            for (int i = 0; i < 4; ++i)
                yield return null;
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
        public IEnumerator Check_CustomHLSL_Block_Works_In_Context([ValueSource(nameof(k_Check_CustomHLSL_Block_Works_In_Context_Case))] Check_CustomHLSL_Block_Works_In_Context_Case target)
        {
            // Arrange
            var hlslCode =
@"void FindMe_In_Generated_Source(inout VFXAttributes attributes)
{
    attributes.color *= length(attributes.position);
}";

            VFXContextType contextType;
            switch (target)
            {
                case Check_CustomHLSL_Block_Works_In_Context_Case.Initialize: contextType = VFXContextType.Init; break;
                case Check_CustomHLSL_Block_Works_In_Context_Case.Update: contextType = VFXContextType.Update; break;
                case Check_CustomHLSL_Block_Works_In_Context_Case.Output:
                case Check_CustomHLSL_Block_Works_In_Context_Case.OutputSG: contextType = VFXContextType.Output; break;
                default: throw new NotImplementedException();
            }

            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph, true, contextType);

            if (target == Check_CustomHLSL_Block_Works_In_Context_Case.OutputSG)
            {
                var previousOutput = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Output);
                previousOutput.RemoveChild(hlslBlock);
                var sgOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
                sgOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
                sgOutput.SetSettingValue("m_Shading", new ParticleShadingShaderGraph());
                sgOutput.AddChild(hlslBlock);

                var parentContext = previousOutput.inputFlowSlot.First().link.First().context;
                previousOutput.UnlinkAll();
                sgOutput.LinkFrom(parentContext);
                graph.AddChild(sgOutput);
            }

            VFXTestCommon.ReimportVFXGraph(graph);

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

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Compiles()
        {
            // Arrange
            var hlslCode =
                "void TestFunction(in VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "    attributes.position = float3(param, param, param);\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            // Act
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);

            // Assert
            foreach (var y in VFXTestCommon.CheckCompilation(graph))
            {
                yield return y;
            }
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Twice_Same_Block_Compiles()
        {
            // Arrange
            var hlslCode =
                "void TestFunction(inout VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "    attributes.position = float3(param, param, param);\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);

            // Act
            var copyHlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            copyHlslBlock.SetSettingValue("m_HLSLCode", hlslCode);
            var targetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Update);
            targetContext.AddChild(copyHlslBlock);
            view.graphView.OnSave();

            // Assert
            foreach (var y in VFXTestCommon.CheckCompilation(graph))
            {
                yield return y;
            }
        }

        [UnityTest]
        public IEnumerator Check_Diverging_Usage_Buffer([Values(true, false)] bool legalUsage)
        {
            var hlslCodeRW =
@"void Write_In_RWBuffer(VFXAttributes attributes, RWBuffer<float3> buffer)
{
    buffer[0u] = attributes.color;
}";

            var hlslCode =
@"void Read_In_Buffer(VFXAttributes attributes, Buffer<float3> buffer)
{
    attributes.color = buffer[0u];
}";
            var hlslOperatorWrite = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperatorWrite.SetSettingValue("m_HLSLCode", hlslCodeRW);

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

            first.AddChild(hlslOperatorWrite);
            second.AddChild(hlslOperatorRead);

            var parameter = ScriptableObject.CreateInstance<VFXParameter>();
            parameter.Init(typeof(GraphicsBuffer));
            graph.AddChild(parameter);

            Assert.IsTrue(parameter.outputSlots[0].Link(hlslOperatorWrite.inputSlots[0]));
            Assert.IsTrue(parameter.outputSlots[0].Link(hlslOperatorRead.inputSlots[0]));

            var vfxPath = AssetDatabase.GetAssetPath(graph);
            if (legalUsage)
            {
                VFXTestCommon.ReimportVFXGraph(graph);
                for (int i = 0; i < 4; ++i)
                    yield return null;

                var resource = graph.GetResource();
                bool correctInit = false;
                bool correctUpdate = false;
                for (int shaderIndex = 0; shaderIndex < resource.GetShaderSourceCount(); ++shaderIndex)
                {
                    var shaderName = resource.GetShaderSourceName(shaderIndex);
                    if (shaderName.Contains(init.label)
                        && resource.GetShaderSource(shaderIndex).Contains("Write_In_RWBuffer")
                        && !resource.GetShaderSource(shaderIndex).Contains("Read_In_Buffer"))
                    {
                        correctInit = true;
                    }

                    if (shaderName.Contains(update.label)
                        && !resource.GetShaderSource(shaderIndex).Contains("Write_In_RWBuffer")
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
                LogAssert.Expect(LogType.Error, new Regex(@".*Unity cannot compile the VisualEffectAsset.*"));
                VFXTestCommon.ReimportVFXGraph(graph);
            }

            yield return null;
        }
    }
}
