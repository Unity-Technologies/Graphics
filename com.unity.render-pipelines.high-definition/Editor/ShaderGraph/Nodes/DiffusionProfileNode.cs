using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [Title("Input", "High Definition Render Pipeline", "Diffusion Profile")]
    [FormerName("UnityEditor.ShaderGraph.DiffusionProfileNode")]
    class DiffusionProfileNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public DiffusionProfileNode()
        {
            name = "Diffusion Profile";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            // This still needs to be added.
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Diffusion-Profile-Node"; }
        }

        [SerializeField, Obsolete("Use m_DiffusionProfileAsset instead.")]
        PopupList m_DiffusionProfile = new PopupList();

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

        [ObjectControl]
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
                    return ;
                
                var serializedProfile = new DiffusionProfileSerializer();
                serializedProfile.diffusionProfileAsset = value;
                m_SerializedDiffusionProfile = EditorJsonUtility.ToJson(serializedProfile, true);
                m_DiffusionProfileAsset = value;
                Dirty(ModificationScope.Node);
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
                Debug.LogError("Failed to upgrade the diffusion profile node value, reseting to default value."+ 
                    "\nTo remove this message save the shader graph with the new diffusion profile reference.");
                m_DiffusionProfile.selectedEntry = 0;
            }
#pragma warning restore 618
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            uint hash = 0;
            
            if (diffusionProfile != null)
                hash = (diffusionProfile.profile.hash);
            
            visitor.AddShaderChunk(precision + " " + GetVariableNameForSlot(0) + " = asfloat(uint(" + hash + "));", true);
        }
    }
}
