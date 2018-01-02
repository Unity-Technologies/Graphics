using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System.Collections.Generic;

/*namespace UnityEditor.ShaderGraph
{
    public enum SceneDepthMode
    {
        Default,
        Normalized
    };

    [Title("Input", "Scene", "Scene Depth")]
    public sealed class SceneDepthNode : AbstractMaterialNode, IGenerateProperties, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        const string kUVSlotName = "UV";
        const string kOutputSlotName = "Out";

        public const int UVSlotId = 0;
        public const int OutputSlotId = 1;

        public SceneDepthNode()
        {
            name = "Scene Depth";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        [SerializeField]
        private SceneDepthMode m_SceneDepthMode = SceneDepthMode.Default;

        [EnumControl("Mode")]
        public SceneDepthMode sceneDepthMode
        {
            get { return m_SceneDepthMode; }
            set
            {
                if (m_SceneDepthMode == value)
                    return;

                m_SceneDepthMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName));
            AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, OutputSlotId });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty()
            {
                name = "_CameraDepthTexture",
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
                overrideReferenceName = "_CameraDepthTexture",
                generatePropertyBlock = false
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string uvValue = GetSlotValue(UVSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            string methodName = "";
            switch (sceneDepthMode)
            {
                case SceneDepthMode.Normalized:
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
}*/
