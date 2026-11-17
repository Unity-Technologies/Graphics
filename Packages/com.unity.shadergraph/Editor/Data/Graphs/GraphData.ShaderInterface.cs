using System;
using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.ShaderGraph.ProviderSystem;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    sealed partial class GraphData
    {
        // The mode is still in development, so it only takes effect in Internal Mode.
        internal static Func<bool> isInternalMode = Unsupported.IsDeveloperMode;

        ShaderInterfaceGraphData shaderInterfaceData
        {
            get
            {
                foreach (var subData in SubDatas)
                    if (subData is ShaderInterfaceGraphData data)
                        return data;
                return null;
            }
        }

        internal bool hasShaderInterfaceData => shaderInterfaceData != null;

        internal bool isShaderInterfaceMode
        {
            get => hasShaderInterfaceData && isInternalMode();
            set
            {
                var data = shaderInterfaceData;
                if (value && data == null)
                    m_SubDatas.Add(new ShaderInterfaceGraphData());
                else if (!value && data != null)
                    m_SubDatas.RemoveAll(d => d.value == data);
            }
        }

        internal IProvider<IShaderInterface> shaderInterface => shaderInterfaceData?.shaderInterface;

        internal IReadOnlyList<VirtualFunctionStackData> virtualFunctionStacks
            => shaderInterfaceData?.stacks ?? (IReadOnlyList<VirtualFunctionStackData>)Array.Empty<VirtualFunctionStackData>();

        internal List<IProvider<IVirtualFunction>> loadedVirtualFunctions
        {
            get
            {
                List<IProvider<IVirtualFunction>> functions = new();
                if (shaderInterface is ShaderInterfaceProvider { IsValid: true } shaderInterfaceProvider)
                    foreach (var function in shaderInterfaceProvider.VirtualFunctionProviders)
                        if (function.IsValid)
                            functions.Add(function);
                return functions;
            }
        }

        bool m_ShaderInterfaceChanged;

        // Not set by undo and redo, which replace the interface and its stacks wholesale.
        internal bool shaderInterfaceChanged => m_ShaderInterfaceChanged;

        // Records undo; the window rebuilds its view when the mode changes.
        // Output nodes exist only in stacks, so leaving removes them; other local provider nodes become orphans.
        internal void SwitchShaderInterfaceMode(bool enabled)
        {
            if (isShaderInterfaceMode == enabled)
                return;

            owner?.RegisterCompleteObjectUndo(enabled ? "Enter Shader Interface Mode" : "Exit Shader Interface Mode");
            isShaderInterfaceMode = enabled;
            if (!enabled)
            {
                foreach (var output in new List<VirtualFunctionOutputNode>(GetNodes<VirtualFunctionOutputNode>()))
                    RemoveNodeAndEdgesNoValidate(output);
            }
            m_ShaderInterfaceChanged = true;
            ValidateGraph();
        }

        // Callers record undo.
        internal void SetShaderInterface(IProvider<IShaderInterface> provider)
        {
            var data = shaderInterfaceData;
            if (data == null)
                return;

            var previous = data.shaderInterface;
            data.shaderInterface = provider;
            if (previous != null)
                data.shaderInterface?.CopyStateFrom(previous);

            // Nodes the new interface doesn't declare stay as orphans, so picking one that declares them again recovers them.
            SyncVirtualFunctionStacks();
            m_ShaderInterfaceChanged = true;
            ValidateGraph();
        }

        internal bool TryGetVirtualFunctionProvider(string key, out IProvider<IVirtualFunction> provider)
        {
            provider = null;
            return shaderInterface is ShaderInterfaceProvider shaderInterfaceProvider && shaderInterfaceProvider.TryGetVirtualFunctionProvider(key, out provider);
        }

        // Callers record undo.
        internal void SetEnsureBaseCall(string key, bool ensureBaseCall)
        {
            if (!TryGetVirtualFunctionProvider(key, out var provider) || provider is not VirtualFunctionProvider function)
                return;

            function.EnsureBaseCall = ensureBaseCall;
            foreach (var node in GetNodes<AbstractVirtualFunctionNode>())
                if (node.providerKey == key)
                    node.Refresh();
        }

        // Callers record undo.
        internal void AddLocalProviderNode<T>(AbstractProviderNode<T> node, IProvider<T> provider, Vector2 position) where T : IDefinition
        {
            node.InitializeFromLocalProvider(provider);
            var drawState = node.drawState;
            drawState.position = new Rect(position, Vector2.zero);
            node.drawState = drawState;
            AddNode(node);
        }

        // Runs only when the interface changes, never from validation. Orphaned output nodes keep their stacks.
        internal void SyncVirtualFunctionStacks()
        {
            if (!isShaderInterfaceMode)
                return;

            var functions = loadedVirtualFunctions;
            var outputs = VirtualFunctionOutputNodesByKey();
            AddMissingOutputNodes(functions, outputs);
            var data = shaderInterfaceData;
            m_ShaderInterfaceChanged |= data.SetStacks(VirtualFunctionStackData.StacksFor(functions, outputs.Keys, data.stacks));
        }

        internal Dictionary<string, VirtualFunctionOutputNode> VirtualFunctionOutputNodesByKey()
        {
            Dictionary<string, VirtualFunctionOutputNode> outputs = new();
            foreach (var output in GetNodes<VirtualFunctionOutputNode>())
                outputs.TryAdd(output.providerKey ?? string.Empty, output);
            return outputs;
        }

        void AddMissingOutputNodes(List<IProvider<IVirtualFunction>> functions, Dictionary<string, VirtualFunctionOutputNode> outputs)
        {
            foreach (var function in functions)
            {
                var key = function.Info.ProviderKey;
                if (outputs.ContainsKey(key))
                    continue;

                var output = new VirtualFunctionOutputNode();
                output.InitializeFromLocalProvider(function);
                AddNodeNoValidate(output);
                outputs.Add(key, output);
            }
        }

        // Skipped during a replace, which brings its own stacks.
        void RemoveFromVirtualFunctionStack(VirtualFunctionOutputNode output)
        {
            if (!replaceInProgress && shaderInterfaceData?.RemoveFromStack(output.providerKey ?? string.Empty) == true)
                m_ShaderInterfaceChanged = true;
        }

        void RemoveNodeAndEdgesNoValidate(AbstractMaterialNode node)
        {
            if (m_NodeEdges.TryGetValue(node.objectId, out var edges))
                foreach (var edge in edges.ToArray())
                    RemoveEdgeNoValidate(edge);
            RemoveNodeNoValidate(node);
        }
    }
}
