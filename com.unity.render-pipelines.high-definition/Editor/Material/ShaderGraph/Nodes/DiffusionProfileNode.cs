using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Input", "High Definition Render Pipeline", "Diffusion Profile")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.DiffusionProfileNode")]
    [FormerName("UnityEditor.ShaderGraph.DiffusionProfileNode")]
    [HasDependencies(typeof(DiffusionProfileNode))]
    class DiffusionProfileNode : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode, IHasDependencies
    {
        public DiffusionProfileNode()
        {
            name = "Diffusion Profile";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-Diffusion-Profile");

        [SerializeField, Obsolete("Use m_DiffusionProfileAsset instead.")]
        UnityEditor.ShaderGraph.Drawing.Controls.PopupList m_DiffusionProfile = new UnityEditor.ShaderGraph.Drawing.Controls.PopupList();

        // Helper class to serialize an asset inside a shader graph
        [Serializable]
        private class DiffusionProfileSerializer
        {
            [SerializeField]
            public DiffusionProfileSettings    diffusionProfileAsset;
        }

        [SerializeField]
        string m_SerializedDiffusionProfile;

        [NonSerialized]
        DiffusionProfileSettings    m_DiffusionProfileAsset;

        //Hide name to be consistent with Texture2DAsset node
        [ObjectControl("")]
        public DiffusionProfileSettings diffusionProfile
        {
            get
            {
                if (String.IsNullOrEmpty(m_SerializedDiffusionProfile))
                    return null;

                if (m_DiffusionProfileAsset == null)
                {
                    var serializedProfile = new DiffusionProfileSerializer();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedDiffusionProfile, serializedProfile);
                    m_DiffusionProfileAsset = serializedProfile.diffusionProfileAsset;
                }

                return m_DiffusionProfileAsset;
            }
            set
            {
                if (m_DiffusionProfileAsset == value)
                    return;

                var serializedProfile = new DiffusionProfileSerializer();
                serializedProfile.diffusionProfileAsset = value;
                m_SerializedDiffusionProfile = EditorJsonUtility.ToJson(serializedProfile, true);
                m_DiffusionProfileAsset = value;
                Dirty(ModificationScope.Node);
                ValidateNode();
            }
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0.0f));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });

            UpgradeIfNeeded();
        }

        void UpgradeIfNeeded()
        {
#pragma warning disable 618
            // When the node is upgraded we set the selected entry to 0
            if (m_DiffusionProfile.selectedEntry != 0)
            {
                // Can't reliably retrieve the slot value from here so we warn the user that we probably loose his diffusion profile reference
                Debug.LogError("Failed to upgrade the diffusion profile node value, reseting to default value." +
                    "\nTo remove this message save the shader graph with the new diffusion profile reference.");
                m_DiffusionProfile.selectedEntry = 0;
            }
#pragma warning restore 618
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            uint hash = 0;

            if (diffusionProfile != null)
                hash = (diffusionProfile.profile.hash);

            // Note: we don't use the auto precision here because we need a 32 bit to store this value
            sb.AppendLine(string.Format("float {0} = asfloat(uint({1}));", GetVariableNameForSlot(0), hash));
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var prop = new DiffusionProfileShaderProperty { value = diffusionProfile };
            if (diffusionProfile != null)
                prop.displayName = diffusionProfile.name;
            return prop;
        }

        public int outputSlotId => kOutputSlotId;

        public void GetSourceAssetDependencies(AssetCollection assetCollection)
        {
            if ((diffusionProfile != null) && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(diffusionProfile, out string guid, out long localId))
            {
                // diffusion profile is a ScriptableObject, so this is an artifact dependency
                assetCollection.AddAssetDependency(new GUID(guid), AssetCollection.Flags.ArtifactDependency | AssetCollection.Flags.IncludeInExportPackage);
            }
        }
    }
}
