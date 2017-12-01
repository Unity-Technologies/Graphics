using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public enum DepthTextureMode
    {
        Default,
        Normalized
    };

    [Title("Input/Scene/Depth Texture")]
    public class DepthTextureNode : AbstractMaterialNode, IGenerateProperties, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        const string kUVSlotName = "UV";
        const string kOutputSlotName = "Out";

        public const int UVSlotId = 0;
        public const int OutputSlotId = 1;

        public DepthTextureNode()
        {
            name = "Depth Texture";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        [SerializeField]
        private DepthTextureMode m_DepthTextureMode = DepthTextureMode.Default;

        [EnumControl("Mode")]
        public DepthTextureMode depthTextureMode
        {
            get { return m_DepthTextureMode; }
            set
            {
                if (m_DepthTextureMode == value)
                    return;

                m_DepthTextureMode = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName));
            AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, OutputSlotId });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty()
            {
                m_Name = "_CameraDepthTexture",
                m_PropType = PropertyType.Float,
                m_Vector4 = new Vector4(1, 1, 1, 1),
                m_Float = 1,
                m_Color = new Vector4(1, 1, 1, 1),
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new SamplerShaderProperty
            {
                overrideReferenceName = "_CameraDepthTexture",
                generatePropertyBlock = false
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string uvValue = GetSlotValue(UVSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            string methodName = "";
            switch (depthTextureMode)
            {
                case DepthTextureMode.Normalized:
                    methodName = "Linear01Depth";
                    break;
                default:
                    methodName = "LinearEyeDepth";
                    break;
            }
            visitor.AddShaderChunk(string.Format("{0} _DepthTexture = {1}(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD({2})).r);", precision, methodName, uvValue), true);
            visitor.AddShaderChunk(string.Format("{0} {1} = _DepthTexture;", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
        }

        public bool RequiresScreenPosition()
        {
            var uvSlot = FindInputSlot<MaterialSlot>(UVSlotId) as ScreenPositionMaterialSlot;
            if (uvSlot == null)
                return false;

            if (uvSlot.isConnected)
                return false;

            return uvSlot.RequiresScreenPosition();
        }
    }
}
