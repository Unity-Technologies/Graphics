using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public enum MotionVectorTextureMode
    {
        Default,
        Hue
    };

    [Title("Input/Scene/Motion Vector Texture")]
    public class MotionVectorTextureNode : AbstractMaterialNode, IGenerateProperties, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        const string kUVSlotName = "UV";
        const string kOutputSlotName = "Out";

        public const int UVSlotId = 0;
        public const int OutputSlotId = 1;

        public MotionVectorTextureNode()
        {
            name = "Motion Vector Texture";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        [SerializeField]
        private MotionVectorTextureMode m_MotionVectorTextureMode = MotionVectorTextureMode.Default;

        [EnumControl("Mode")]
        public MotionVectorTextureMode motionVectorTextureMode
        {
            get { return m_MotionVectorTextureMode; }
            set
            {
                if (m_MotionVectorTextureMode == value)
                    return;

                m_MotionVectorTextureMode = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, OutputSlotId });
        }


        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty()
            {
                m_Name = "_CameraMotionVectorTexture",
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
                overrideReferenceName = "_CameraMotionVectorTexture",
                generatePropertyBlock = false
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string uvValue = GetSlotValue(UVSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0}3 _MotionVectorTexture = {0}3(tex2D(_CameraMotionVectorTexture, {1}).rg, 0);", precision, uvValue), true);

            if(motionVectorTextureMode == MotionVectorTextureMode.Hue)
            {
                visitor.AddShaderChunk(string.Format("{0} hue = (atan2(_MotionVectorTexture.x, _MotionVectorTexture.y) / 3.14159265359 + 1.0) * 0.5;", precision), true);
                visitor.AddShaderChunk(string.Format("_MotionVectorTexture = saturate({0}3(abs(hue * 6.0 - 3.0) - 1.0, 2.0 - abs(hue * 6.0 - 2.0), 2.0 - abs(hue * 6.0 - 4.0)));", precision), true);
            }

            visitor.AddShaderChunk(string.Format("{0} {1} = _MotionVectorTexture;", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
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
