using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    enum DepthSamplingMode
    {
        Linear01,
        Raw,
        Eye
    };

    [Title("Input", "Scene", "Scene Depth")]
    sealed class SceneDepthNode : CodeFunctionNode, IMayRequireDepthTexture
    {
        const string kScreenPositionSlotName = "UV";
        const string kOutputSlotName = "Out";

        public const int ScreenPositionSlotId = 0;
        public const int OutputSlotId = 1;

        [SerializeField]
        private DepthSamplingMode m_DepthSamplingMode = DepthSamplingMode.Linear01;

        [EnumControl("Sampling Mode")]
        public DepthSamplingMode depthSamplingMode
        {
            get { return m_DepthSamplingMode; }
            set
            {
                if (m_DepthSamplingMode == value)
                    return ;

                m_DepthSamplingMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public SceneDepthNode()
        {
            name = "Scene Depth";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }


        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_DepthSamplingMode)
            {
                case DepthSamplingMode.Raw:
                    return GetType().GetMethod("Unity_SceneDepth_Raw", BindingFlags.Static | BindingFlags.NonPublic);
                case DepthSamplingMode.Eye:
                    return GetType().GetMethod("Unity_SceneDepth_Eye", BindingFlags.Static | BindingFlags.NonPublic);
                case DepthSamplingMode.Linear01:
                default:
                    return GetType().GetMethod("Unity_SceneDepth_Linear01", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_SceneDepth_Linear01(
            [Slot(0, Binding.ScreenPosition)] Vector4 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
                @"
{
    Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
}
";
        }

        static string Unity_SceneDepth_Raw(
            [Slot(0, Binding.ScreenPosition)] Vector4 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
                @"
{
    Out = SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy);
}
";
        }

        static string Unity_SceneDepth_Eye(
            [Slot(0, Binding.ScreenPosition)] Vector4 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
                @"
{
    Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
}
";
        }

        public bool RequiresDepthTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
