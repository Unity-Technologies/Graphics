using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    // An interface with a function in each of two ordered contexts and a pure virtual one without a context.
    static class VirtualFunctionTestInterface
    {
        internal const string kPath = "Assets/VirtualFunctionNodeTests.shader";
        internal const string kName = "Hidden/Tests/VirtualFunctionNodeTests";
        internal const string kOtherPath = "Assets/VirtualFunctionNodeTests_Other.shader";
        internal const string kOtherName = "Hidden/Tests/VirtualFunctionNodeTests_Other";

        internal const string kSurface = "Tests.Surface";
        internal const string kVertex = "Tests.Vertex";
        internal const string kTint = "Tests.Tint";
        internal const string kLight = "Tests.Light";

        internal static string Hints(string key, string context)
            => "/// <funchints>\n/// <sg:ProviderKey>" + key + "</sg:ProviderKey>\n"
            + (context != null ? "/// <sg:Context>" + context + "</sg:Context>\n" : string.Empty)
            + "/// </funchints>\n";

        internal static readonly string kSurfaceFunction = Hints(kSurface, "Fragment, 2")
            + "UNITY_VIRTUAL void OnSurface(in float3 positionWS, inout half3 albedo, out half alpha) { alpha = 1; }\n";
        internal static readonly string kVertexFunction = Hints(kVertex, "Vertex, 1")
            + "UNITY_VIRTUAL float3 OnVertex(float3 positionOS) { return positionOS; }\n";
        internal static readonly string kTintFunction = Hints(kTint, null)
            + "UNITY_PURE_VIRTUAL float4 GetTint();\n";
        internal static readonly string kLightFunction = Hints(kLight, "Fragment, 2")
            + "UNITY_VIRTUAL half3 OnLight(half3 color) { return color; }\n";

        internal static readonly string kDefault = kSurfaceFunction + kVertexFunction + kTintFunction;

        internal static string Source(string name, string functions) => @"
ShaderInterface """ + name + @"""
{
    SubShader
    {
        Pass
        {
            Name ""Forward""
            Public
            {
" + functions + @"
            }
            Private
            {
                #pragma vertex Vert
                #pragma fragment Frag
                float4 Vert(float4 positionOS : POSITION0) : SV_POSITION0 { return positionOS; }
                float4 Frag() : SV_TARGET0 { return 1; }
            }
        }
    }
}";

        internal static void Write(string functions, string path = kPath, string name = kName)
            => WriteSource(path, Source(name, functions));

        // A shader without a Public output isn't an interface, so the interface can't load from it.
        internal static void WritePlainShader()
            => WriteSource(kPath, @"
Shader """ + kName + @"""
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Vert(float4 positionOS : POSITION0) : SV_POSITION0 { return positionOS; }
            float4 Frag() : SV_TARGET0 { return 1; }
            ENDHLSL
        }
    }
}");

        static void WriteSource(string path, string source)
        {
            File.WriteAllText(path, source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        internal static void Delete()
        {
            AssetDatabase.DeleteAsset(kPath);
            AssetDatabase.DeleteAsset(kOtherPath);
        }

        internal static ShaderInterfaceProvider Interface(string path = kPath, string name = kName)
            => new(AssetDatabase.GUIDFromAssetPath(path), name);

        internal static GraphData InterfaceGraph(GraphData graph = null)
        {
            graph ??= new GraphData();
            graph.isShaderInterfaceMode = true;
            graph.SetShaderInterface(Interface());
            return graph;
        }

        // Validation consults the target outside the mode.
        internal static GraphData InterfaceGraphWithTarget()
        {
            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { new BuiltInTarget() }, null);
            return InterfaceGraph(graph);
        }

        internal static GraphObject InterfaceGraphObject()
        {
            var data = new GraphData();
            data.AddContexts();
            data.InitializeOutputs(null, null);
            data.AddCategory(CategoryData.DefaultCategory());
            var graphObject = ScriptableObject.CreateInstance<GraphObject>();
            graphObject.hideFlags = HideFlags.HideAndDontSave;
            graphObject.graph = data;
            InterfaceGraph(data);
            return graphObject;
        }

        // Reloads the interface the way the window does when its asset changes.
        internal static void Reload(GraphData graph)
        {
            graph.ReloadAssetDependencies(new HashSet<string> { graph.shaderInterface.AssetID.ToString() });
            graph.ValidateGraph();
        }

        internal static IProvider<IVirtualFunction> Function(GraphData graph, string key)
        {
            Assert.IsTrue(graph.TryGetVirtualFunctionProvider(key, out var provider), key);
            return provider;
        }

        internal static VirtualFunctionOutputNode Output(GraphData graph, string key)
            => graph.GetNodes<VirtualFunctionOutputNode>().Single(o => o.providerKey == key);

        internal static string StackOf(GraphData graph, string key) => graph.virtualFunctionStacks.Single(s => s.Holds(key)).context;

        internal static T AddNode<T>(GraphData graph, string key) where T : AbstractVirtualFunctionNode, new()
        {
            var node = new T();
            graph.AddLocalProviderNode(node, Function(graph, key), Vector2.zero);
            return node;
        }

        internal static string[] StackContexts(GraphData graph) => graph.virtualFunctionStacks.Select(s => s.context).ToArray();

        internal static string[] Errors(GraphData graph, AbstractMaterialNode node)
            => graph.messageManager.ErrorStrings(id => id == node.objectId).ToArray();
    }

    abstract class VirtualFunctionFixture
    {
        System.Func<bool> m_PreviousInternalMode;

        [SetUp]
        public void SetUpInterface()
        {
            m_PreviousInternalMode = GraphData.isInternalMode;
            GraphData.isInternalMode = () => true;
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kDefault);
        }

        [TearDown]
        public void TearDownInterface()
        {
            GraphData.isInternalMode = m_PreviousInternalMode;
            VirtualFunctionTestInterface.Delete();
        }
    }
}
