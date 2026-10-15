using System;
using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    // Serialize Shader Interface mode as a data extension so that the mode doesn't leak into assets that don't use the feature.
    [Serializable]
    [HasDependencies(typeof(ShaderInterfaceGraphData))]
    sealed class ShaderInterfaceGraphData : AbstractShaderGraphDataExtension, IHasDependencies, IHasAssetDependencies
    {
        [SerializeReference]
        IProvider<IShaderInterface> m_ShaderInterface;

        internal IProvider<IShaderInterface> shaderInterface
        {
            get => m_ShaderInterface;
            set => m_ShaderInterface = (IProvider<IShaderInterface>)value?.Clone();
        }

        internal override string displayName => "Shader Interface";

        internal override void OnPropertiesGUI(VisualElement context, Action onChange, Action<string> registerUndo, GraphData owner)
        {
        }

        public bool Reload(HashSet<string> changedAssetGuids)
        {
            if (m_ShaderInterface == null || !changedAssetGuids.Contains(m_ShaderInterface.AssetID.ToString()))
                return false;

            m_ShaderInterface.Reload();
            return true;
        }

        public void GetSourceAssetDependencies(AssetCollection assetCollection)
        {
            if (m_ShaderInterface == null || m_ShaderInterface.AssetID == default)
                return;

            assetCollection.AddAssetDependency(m_ShaderInterface.AssetID,
                AssetCollection.Flags.SourceDependency
                | AssetCollection.Flags.ArtifactDependency
                | AssetCollection.Flags.IncludeInExportPackage);
        }
    }

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

        // Records undo; the window rebuilds its view when the mode changes.
        internal void SwitchShaderInterfaceMode(bool enabled)
        {
            if (isShaderInterfaceMode == enabled)
                return;

            owner?.RegisterCompleteObjectUndo(enabled ? "Enter Shader Interface Mode" : "Exit Shader Interface Mode");
            isShaderInterfaceMode = enabled;
        }

        internal void SetShaderInterface(IProvider<IShaderInterface> provider)
        {
            var data = shaderInterfaceData;
            if (data != null)
                data.shaderInterface = provider;
        }
    }
}
