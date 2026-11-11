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

namespace UnityEditor.VFX.Test
{
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct CustomHLSLOperatorTestType
    {
        public uint test;
        public Vector3 position;
    }
}

namespace UnityEditor.VFX.Test.Integration.HLSL
{
    using CustomHLSL = UnityEditor.VFX.Operator.CustomHLSL;

    [TestFixture]
    public class VFXCustomHLSLOperatorCompilationTests
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
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

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
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

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

        public static readonly VFXCustomHLSLBufferCase[] kSampleBufferCase = VFXCustomHLSLBufferTestCases.kSampleBufferCase;

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Operator_Buffer([ValueSource(nameof(kSampleBufferCase))] VFXCustomHLSLBufferCase bufferCase)
        {
            if (bufferCase.declaration == "RWTexture2D<uint>" && UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal)
                Assert.Ignore("Texture atomics are not supported in Metal");

            var hlslCode = new StringBuilder();
            hlslCode.AppendLine($"float3 Check_Sample_Buffer(in {bufferCase.declaration} inputBuffer)");
            hlslCode.AppendLine("{");
            hlslCode.AppendLine("    float3 localValue = (float3)0.0f;");
            hlslCode.AppendLine($"    {bufferCase.implementation}");
            hlslCode.AppendLine("    return localValue;");
            hlslCode.AppendLine("}");

            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_HLSLCode", hlslCode.ToString());
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);

            var vfxTargetContext = graph.children.OfType<VFXContext>().Single(x => x.contextType == VFXContextType.Update);
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            Assert.IsNotNull(blockAttributeDesc);
            var blockAttribute = blockAttributeDesc.variant.CreateInstance() as Block.SetAttribute;
            blockAttribute.SetSettingValue("attribute", "position");
            vfxTargetContext.AddChild(blockAttribute);
            Assert.IsTrue(blockAttribute.inputSlots[0].Link(hlslOperator.outputSlots[0]));
            VFXTestCommon.ReimportVFXGraph(graph);

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

            string vfxPath = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/CustomHLSL/Repro_UUM_66018.vfx";
            Assert.IsTrue(AssetDatabase.AssetPathExists(vfxPath));

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            var vfx = asset.GetResource();

            vfx.GetGraph().SetCompilationMode(VFXCompilationMode.Edition);
            vfx.GetGraph().CompileAndUpdateAsset(asset);
            yield return null;

            var sourceEdition = Check_Multiple_Usage_Buffer_Get_Source(vfx);
            Assert.IsNotNull(sourceEdition);

            vfx.GetGraph().SetCompilationMode(VFXCompilationMode.Runtime);
            vfx.GetGraph().CompileAndUpdateAsset(asset);
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
                LogAssert.Expect(LogType.Error, new Regex(@".*Unity cannot compile the VisualEffectAsset.*"));
                VFXTestCommon.ReimportVFXGraph(graph);
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

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
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

            var shaderFile = VFXTestCommon.CreateShaderFile(hlslCode, out var shaderIncludePath);
            var hlslOperator = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslOperator.SetSettingValue("m_ShaderFile", shaderFile);

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslOperator, out var view, out var graph);
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
    }
}
