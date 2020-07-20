using System;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

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

            // TODO: render pipeline visibility attribute
            return $@"
[DiffusionProfile]{hideTagString}{referenceName}(""{displayName}"", Float) = {HDShadowUtils.Asfloat(hash)}
[HideInInspector]{assetReferenceName}(""{displayName}"", Vector) = ({NodeUtils.FloatToShaderValueShaderLabSafe(asset.x)}, {NodeUtils.FloatToShaderValueShaderLabSafe(asset.y)}, {NodeUtils.FloatToShaderValueShaderLabSafe(asset.z)}, {NodeUtils.FloatToShaderValueShaderLabSafe(asset.w)})";
        }

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $@"";
        }

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

        void IShaderPropertyDrawer.HandlePropertyField(PropertySheet propertySheet)
        {
            var diffusionProfileDrawer = new DiffusionProfilePropertyDrawer();
            // TODO:
            // diffusionProfileDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            // diffusionProfileDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            Debug.Log(diffusionProfileDrawer);
            propertySheet.Add(diffusionProfileDrawer.CreateGUI(
                newValue => {
                    value = new SerializableDiffusionProfile{ diffusionProfile = newValue };
                },
                value?.diffusionProfile, // TODO: diffusion profile value
                "Diffusion Profile",
                out var _));
        }
    }
}
