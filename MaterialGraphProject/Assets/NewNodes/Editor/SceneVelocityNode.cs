using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System.Collections.Generic;

/*namespace UnityEditor.ShaderGraph
{
    public enum SceneVelocityMode
    {
        Default,
        Hue
    };

    [Title("Input", "Scene", "Scene Velocity")]
    public class SceneVelocityNode : AbstractMaterialNode, IGenerateProperties, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        const string kUVSlotName = "UV";
        const string kOutputSlotName = "Out";

        public const int UVSlotId = 0;
        public const int OutputSlotId = 1;

        public SceneVelocityNode()
        {
            name = "Scene Velocity";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        [SerializeField]
        private SceneVelocityMode m_SceneVelocityMode = SceneVelocityMode.Default;

        [EnumControl("Mode")]
        public SceneVelocityMode sceneVelocityMode
        {
            get { return m_SceneVelocityMode; }
            set
            {
                if (m_SceneVelocityMode == value)
                    return;

                m_SceneVelocityMode = value;
                Dirty(ModificationScope.Graph);
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
                name = "_CameraMotionVectorTexture",
                propType = PropertyType.Float,
                vector4Value = new Vector4(1, 1, 1, 1),
                floatValue = 1,
                colorValue = new Vector4(1, 1, 1, 1),
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Sampler2DShaderProperty
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

            if (sceneVelocityMode == SceneVelocityMode.Hue)
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
}*/
