using System;
using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // A node backed by a provider.
    internal interface IProviderNode
    {
        IProvider Provider { get; }

        bool isOrphaned { get; }

        bool isLocal { get; }

        void InitializeFromNode(IProviderNode source);
    }

    [Serializable]
    internal abstract class AbstractProviderNode<T> : AbstractMaterialNode, IProviderNode, IHasAssetDependencies where T : IDefinition
    {
        [SerializeReference]
        IProvider<T> m_provider;

        // Set instead of a copy when the graph's shader interface owns the provider.
        [SerializeField]
        string m_providerKey;

        internal IProvider<T> Provider => isLocal ? FindLocalProvider() : m_provider;

        IProvider<T> FindLocalProvider()
            => owner?.shaderInterface is ShaderInterfaceProvider shaderInterface && TryFind(shaderInterface, m_providerKey, out var provider) ? provider : null;

        IProvider IProviderNode.Provider => Provider;

        // Saved graphs store a missing key as an empty string.
        internal bool isLocal => !string.IsNullOrEmpty(m_providerKey);

        bool IProviderNode.isLocal => isLocal;

        internal string providerKey => m_providerKey;

        // Only node types that override this can refer to an interface's provider.
        private protected virtual bool TryFind(ShaderInterfaceProvider shaderInterface, string key, out IProvider<T> provider)
        {
            provider = null;
            return false;
        }

        // Subgraphs have no shader interface to refer to.
        public override bool allowedInSubGraph => !isLocal;

        internal bool isOrphaned => Provider is not { IsValid: true };

        bool IProviderNode.isOrphaned => isOrphaned;

        // An orphan generates nothing, but its errors still fail the import when it feeds what was generated.
        internal override bool wasUsedByGenerator => base.wasUsedByGenerator || (isOrphaned && FeedsUsedNode());

        // Orphans further down are skipped: what they feed is already in the list.
        bool FeedsUsedNode()
        {
            List<AbstractMaterialNode> downstream = new();
            NodeUtils.CollectNodesNodeFeedsInto(downstream, this, NodeUtils.IncludeSelf.Exclude);
            foreach (var node in downstream)
            {
                if (node is not IProviderNode { isOrphaned: true } && node.wasUsedByGenerator)
                    return true;
            }
            return false;
        }

        internal IReadOnlyList<NodeMessage> messages { get; private set; } = Array.Empty<NodeMessage>();

        internal override bool ExposeToSearcher => false;

        // Owns a copy of the provider.
        internal void InitializeFromProvider(IProvider<T> provider)
        {
            m_providerKey = null;
            m_provider = (IProvider<T>)provider?.Clone();
            Refresh();
        }

        // Refers to the shader interface's provider by its key.
        internal void InitializeFromLocalProvider(IProvider<T> provider)
        {
            m_provider = null;
            m_providerKey = provider?.Info.ProviderKey;
            Refresh(provider);
        }

        // Takes another node's source: its key when it's local, else a copy of its provider.
        internal void InitializeFromNode(AbstractProviderNode<T> source)
        {
            if (!source.isLocal)
            {
                InitializeFromProvider(source.Provider);
                return;
            }
            m_provider = null;
            m_providerKey = source.m_providerKey;
            Refresh(source.Provider ?? Provider);
        }

        void IProviderNode.InitializeFromNode(IProviderNode source)
        {
            if (source is AbstractProviderNode<T> node)
                InitializeFromNode(node);
        }

        private protected abstract void BuildModel(T definition);

        private protected virtual string missingProviderMessage => "Node is in an invalid state and cannot recover.";

        // A local node follows the interface's asset, so it still refreshes when the interface drops its key.
        UnityEngine.GUID sourceAssetID => isLocal ? owner?.shaderInterface?.AssetID ?? default : m_provider?.AssetID ?? default;

        // Names what the interface should declare, such as "virtual function".
        private protected string NotDeclaredMessage(string declaration) => owner?.shaderInterface == null
            ? "No shader interface is selected."
            : $"No {declaration} '{m_providerKey}' is declared by shader interface '{owner.shaderInterface.Info.ProviderKey}'.";

        private protected virtual void AddNodeMessages(IProvider<T> provider, List<NodeMessage> messages)
        {
        }

        internal void UpdateModel() => UpdateModel(Provider);

        void UpdateModel(IProvider<T> provider)
        {
            List<NodeMessage> collected = new();
            if (provider == null)
                collected.Add(new NodeMessage(null, missingProviderMessage, MessageType.Error));
            else
            {
                ProviderMessageUtils.Collect(provider.Messages, provider.Definition as IFieldCollection, collected);
                if (provider.IsValid)
                    BuildModel(provider.Definition);
                else if (collected.Count == 0)
                    collected.Add(new NodeMessage(null, NotLoadedMessage(provider), MessageType.Error));
            }
            AddNodeMessages(provider, collected);
            messages = collected;
        }

        static string NotLoadedMessage(IProvider provider) => provider.AssetID == default
            ? $"'{provider.Info.ProviderKey}' could not be loaded."
            : $"'{provider.Info.ProviderKey}' could not be loaded from '{AssetDatabase.GUIDToAssetPath(provider.AssetID)}'.";

        internal void Refresh() => Refresh(Provider);

        // For a node that isn't in a graph yet, so it can't look its provider up.
        private protected void Refresh(IProvider<T> provider)
        {
            UpdateModel(provider);
            ValidateNode();
            Dirty(ModificationScope.Graph);
        }

        public bool Reload(HashSet<string> changedAssetGuids)
        {
            var assetID = sourceAssetID;
            if (assetID == default || !changedAssetGuids.Contains(assetID.ToString()))
                return false;

            Provider?.Reload();
            Refresh();
            return true;
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            Refresh();
        }

        public override void Concretize()
        {
            SetOverrideActiveState(isOrphaned ? ActiveState.ExplicitInactive : ActiveState.Implicit);
            UpdateModel();
            base.Concretize();
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            foreach (var message in messages)
            {
                var compilerSeverity = message.severity == MessageType.Error ? Rendering.ShaderCompilerMessageSeverity.Error : Rendering.ShaderCompilerMessageSeverity.Warning;
                if (isOrphaned)
                    owner?.AddSetupError(objectId, message.ToString(), compilerSeverity);
                else
                    owner?.AddValidationError(objectId, message.ToString(), compilerSeverity);
            }
        }
    }
}
