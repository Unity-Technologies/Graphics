using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEngine;
using Types = UnityEditor.ShaderGraph.Registry.Types;
using com.unity.shadergraph.defs;

namespace UnityEditor.ShaderGraph.HeadlessPreview.StandardDefTests
{
    [TestFixture]
    class StandardFunctionPreviewTestFixture
    {
        static Texture2D DrawShaderToTexture(Shader shader)
        {
            var rt = RenderTexture.GetTemporary(4,4,0,RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(null, rt, new Material(shader));
            Texture2D output = new Texture2D(4, 4, TextureFormat.ARGB32, false);
            output.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            RenderTexture.active = prevActive;
            rt.Release();
            return output;
        }

        static Color SampleMaterialColor(Material material)
        {
            var outputTexture = DrawShaderToTexture(material.shader);
            try
            {
                return outputTexture.GetPixel(0, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        static void InitPortValue(IPortWriter port, Vector4 value)
        {
            port.SetField("c0", value.x);
            port.SetField("c1", value.y);
            port.SetField("c2", value.z);
            port.SetField("c3", value.w);
        }

        static void TestStandardFunction<T>(Color expected, params Vector4[] init) where T : IStandardNode
        {
            var std = (IStandardNode)Activator.CreateInstance<T>();
            TestFunctionDescription(expected, std.FunctionDescriptor, init);
        }

        static void TestFunctionDescription(Color expected, FunctionDescriptor func, params Vector4[] init)
        {
            var graphHandler = GraphUtil.CreateGraph();
            var registry = new Registry.Registry();
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<Types.GraphType>();
            registry.Register<Types.GraphTypeAssignment>();
            var funcKey = registry.Register(func);

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);

            var nodeWriter = graphHandler.AddNode(funcKey, "Node", registry);

            int i = 0;
            foreach (var param in func.Parameters)
            {
                if (param.Usage != Types.GraphType.Usage.In) continue;
                var port = nodeWriter.GetPort(param.Name);
                InitPortValue(port, init[i]);
                previewMgr.SetLocalProperty("Node", param.Name, init[i]);
                ++i;
            }

            var nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("Node");
            Assert.AreEqual(expected, SampleMaterialColor(nodePreviewMaterial));
        }

        [Test]
        public void FunctionDefinition_Test()
        {
            FunctionDescriptor func = new FunctionDescriptor(1, "Func",
                "Out = In.x;",
                new ParameterDescriptor("Out", TYPE.Float, Types.GraphType.Usage.Out),
                new ParameterDescriptor("In", TYPE.Float, Types.GraphType.Usage.In));
            TestFunctionDescription(new Color(1,1,1,1), func, new Vector4(1,0,0,0)); // because floats propogate.
        }

        [Test]
        public void StandardDefinition_Add_Test() =>
            TestStandardFunction<AddNode>(new Color(1, 0, 1, 1), new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0));

        [Test]
        public void StandardDefinition_Lerp_Test_Start() =>
            TestStandardFunction<LerpNode>(new Color(0, 0, 0, 1), Vector4.zero, Vector4.one, Vector4.zero);

        public void StandardDefinition_Lerp_Test_End() =>
            TestStandardFunction<LerpNode>(new Color(1, 1, 1, 1), Vector4.zero, Vector4.one, Vector4.one);

        [Test]
        public void StandardDefinition_Normalize_Shorten_Test() =>
            TestStandardFunction<NormalizeNode>(new Color(1, 0, 0, 1), new Vector4(10,0,0,0));

        [Test]
        public void StandardDefinition_Normalize_Lengthen_Test() =>
            TestStandardFunction<NormalizeNode>(new Color(1, 0, 0, 1), new Vector4(.5f, 0, 0, 0));

        [Test]
        public void StandardDefinition_Power_Zero_Test() =>
            TestStandardFunction<PowerNode>(new Color(0, 0, 0, 1), Vector4.zero, Vector4.one);
        [Test]
        public void StandardDefinition_Power_One_Test() =>
            TestStandardFunction<PowerNode>(new Color(1, 1, 1, 1), Vector4.one, Vector4.one);
    }
}
