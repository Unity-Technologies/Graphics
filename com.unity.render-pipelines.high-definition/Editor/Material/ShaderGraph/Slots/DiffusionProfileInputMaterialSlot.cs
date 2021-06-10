using System;
using System.Collections.Generic;
using UnityEditor.Rendering.HighDefinition.Drawing.Slots;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.DiffusionProfileInputMaterialSlot")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.DiffusionProfileInputMaterialSlot")]
    [HasDependencies(typeof(DiffusionProfileInputMaterialSlot))]
    class DiffusionProfileInputMaterialSlot : Vector1MaterialSlot, IHasDependencies
    {
        [SerializeField, Obsolete("Use m_DiffusionProfileAsset instead.")]
        PopupList m_DiffusionProfile;

        // Helper class to serialize an asset inside a shader graph
        [Serializable]
        private class DiffusionProfileSerializer
        {
            [SerializeField]
            public DiffusionProfileSettings    diffusionProfileAsset = null;
        }

        [SerializeField]
        string m_SerializedDiffusionProfile;

        [NonSerialized]
        DiffusionProfileSettings m_DiffusionProfileAsset;

        [SerializeField]
        int m_Version;

        DiffusionProfileSlotControlView view;

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
                owner.ValidateNode();
            }
        }

        public override bool isDefaultValue => diffusionProfile == null;

        public DiffusionProfileInputMaterialSlot()
        {
        }

        public DiffusionProfileInputMaterialSlot(int slotId, string displayName, string shaderOutputName,
                                                 ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, 0.0f, stageCapability, hidden: hidden)
        {
        }

        public override VisualElement InstantiateControl()
        {
            view = new DiffusionProfileSlotControlView(this);
            return view;
        }

        AbstractMaterialNode matOwner;
        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            // Note: Unity ShaderLab can't parse float values with exponential notation so we just can't
            // store the hash nor the asset GUID here :(
            var diffusionProfileHash = new Vector1ShaderProperty
            {
                overrideReferenceName = "_DiffusionProfileHash",
                hidden = true,
                value = 0
            };
            var diffusionProfileAsset = new Vector4ShaderProperty
            {
                overrideReferenceName = "_DiffusionProfileAsset",
                hidden = true,
                value = Vector4.zero
            };

            properties.AddShaderProperty(diffusionProfileHash);
            properties.AddShaderProperty(diffusionProfileAsset);

            // We can't upgrade here because we need to access the current render pipeline asset which is not
            // possible outside of unity context so we wait the next editor frame to do it
            EditorApplication.update += UpgradeIfNeeded;
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            if (diffusionProfile == null)
                return "_DiffusionProfileHash";
            else
                return "((asuint(_DiffusionProfileHash) != 0) ? _DiffusionProfileHash : asfloat(uint(" + diffusionProfile.profile.hash + ")))";
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as DiffusionProfileInputMaterialSlot;

            if (slot != null)
            {
                m_SerializedDiffusionProfile = slot.m_SerializedDiffusionProfile;
                m_DiffusionProfileAsset = null;
            }
        }

        void UpgradeIfNeeded()
        {
#pragma warning disable 618
            // Once the profile is upgraded, we set the selected entry to 0 (which was previously none
            // in the diffusion profile index so it's fine if we don't upgrade it
            if (m_Version == 0)
            {
                // We need to warn the user that we can't upgrade the diffusion profile but this upgrade code
                // does not work currently :(
                // Debug.LogError("Failed to upgrade the diffusion profile slot value, reseting to default value: " + HDRenderPipeline.defaultAsset.diffusionProfileSettingsList[m_DiffusionProfile.selectedEntry] +
                //     "\nTo remove this message save the shader graph with the new diffusion profile reference");
                // m_DiffusionProfileAsset = HDRenderPipeline.defaultAsset.diffusionProfileSettingsList[m_DiffusionProfile.selectedEntry];
                m_Version = 1;
                // Sometimes the view is created after we upgrade the slot so we need to update it's value
                view?.UpdateSlotValue();
            }
#pragma warning restore 618
            EditorApplication.update -= UpgradeIfNeeded;
        }

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
