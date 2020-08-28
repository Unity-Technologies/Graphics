using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using System.Globalization;
using static UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers.ShaderInputPropertyDrawer;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    class SerializableDiffusionProfile : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_SerializedDiffusionProfile;

        [SerializeField]
        string m_Guid;

        [NonSerialized]
        DiffusionProfileSettings m_DiffusionProfile;

        [Serializable]
        class DiffusionProfileHelper
        {
#pragma warning disable 649
            public DiffusionProfileSettings profile;
#pragma warning restore 649
        }

        public DiffusionProfileSettings diffusionProfile
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedDiffusionProfile))
                {
                    var diffusionProfileHelper = new DiffusionProfileHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedDiffusionProfile, diffusionProfileHelper);
                    m_SerializedDiffusionProfile = null;
                    m_Guid = null;
                    m_DiffusionProfile = diffusionProfileHelper.profile;
                }
                else if (!string.IsNullOrEmpty(m_Guid) && m_DiffusionProfile == null)
                {
                    m_DiffusionProfile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetDatabase.GUIDToAssetPath(m_Guid));
                    m_Guid = null;
                }

                return m_DiffusionProfile;
            }
            set
            {
                m_DiffusionProfile = value;
                m_Guid = null;
                m_SerializedDiffusionProfile = null;
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedDiffusionProfile = EditorJsonUtility.ToJson(new DiffusionProfileHelper { profile = diffusionProfile }, false);
        }

        public void OnAfterDeserialize()
        {
        }
    }

    [Serializable]
    class DiffusionProfileShaderProperty : AbstractShaderProperty<SerializableDiffusionProfile>, IShaderPropertyDrawer
    {
        internal DiffusionProfileShaderProperty()
        {
            displayName = "Diffusion Profile";
        }

        internal override bool isBatchable => true;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;
        internal override bool isGpuInstanceable => true;

        internal override List<string> supportedRenderPipelines => new List<string>{SupportedRenderPipelineUtils.GetRenderPipelineName(typeof(HDRenderPipelineAsset))};

        internal override bool showPrecisionField => false;
        internal override bool showSupportedRenderPipelinesField => false;
        
        public override PropertyType propertyType => PropertyType.Vector1;

        string assetReferenceName => $"{referenceName}_Asset";

        internal override string GetPropertyBlockString()
        {
            uint hash = 0;
            Vector4 asset = Vector4.zero;

            if (value?.diffusionProfile != null)
            {
                hash = value.diffusionProfile.profile.hash;
                asset = HDUtils.ConvertGUIDToVector4(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value.diffusionProfile)));
            }

            /// <summary>Float to string convertion function without any loss of precision</summary>
            string f2s(float f) => System.Convert.ToDouble(f).ToString("0." + new string('#', 339));

            return
$@"{supportedRenderPipelinesTagString}[DiffusionProfile]{referenceName}(""{displayName}"", Float) = {f2s(HDShadowUtils.Asfloat(hash))}
[HideInInspector]{assetReferenceName}(""{displayName}"", Vector) = ({f2s(asset.x)}, {f2s(asset.y)}, {f2s(asset.z)}, {f2s(asset.w)})";
        }

        public override string GetDefaultReferenceName() => $"DiffusionProfile_{objectId}";

        internal override string GetPropertyDeclarationString(string delimiter = ";") => $@"float {referenceName}{delimiter}";

        internal override AbstractMaterialNode ToConcreteNode()
        {
            var node = new DiffusionProfileNode();
            node.diffusionProfile = value.diffusionProfile;
            return node;
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                floatValue = value?.diffusionProfile != null ? HDShadowUtils.Asfloat(value.diffusionProfile.profile.hash) : 0
            };
        }

        internal override ShaderInput Copy()
        {
            return new DiffusionProfileShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value,
                precision = precision,
                gpuInstanced = gpuInstanced,
            };
        }

        void IShaderPropertyDrawer.HandlePropertyField(PropertySheet propertySheet, PreChangeValueCallback preChangeValueCallback, PostChangeValueCallback postChangeValueCallback)
        {
            var diffusionProfileDrawer = new DiffusionProfilePropertyDrawer();

            propertySheet.Add(diffusionProfileDrawer.CreateGUI(
                newValue => {
                    preChangeValueCallback("Changed Diffusion Profile");
                    value = new SerializableDiffusionProfile{ diffusionProfile = newValue };
                    postChangeValueCallback(true);
                },
                value?.diffusionProfile,
                "Diffusion Profile",
                out var _));
        }
    }
}
