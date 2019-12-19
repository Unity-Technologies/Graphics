using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Custom Blit")]
    sealed class CustomBlitInputNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture
    {
        public enum InputTextureType
        {
            SceneColor,
            CustomColor,
            SceneDepth,
            CustomDepth,
            Normal,
        }

        public enum SamplingType
        {
            Sample,
            Load
        }

        [SerializeField]
        InputTextureType m_InputTextureType = InputTextureType.SceneColor;

        [SerializeField]
        SamplingType samplingType = SamplingType.Load;

        string GetCurrentInputTextureType()
        {
            return System.Enum.GetName(typeof(InputTextureType), m_InputTextureType);
        }

        public CustomBlitInputNode()
        {
            name = "Custom Blit";
            UpdateNodeAfterDeserialization();
        }

        [EnumControl("Input")]
        public InputTextureType inputTextureType
        {
            get { return m_InputTextureType; }
            set
            {
                if (m_InputTextureType == value)
                    return;

                m_InputTextureType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(0, "Out", "Out", SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            AddSlot(new ScreenPositionMaterialSlot(1, "UV", "UV", ScreenSpaceType.Default, ShaderStageCapability.Fragment));
        }
        
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string result = string.Format("$precision4 {0} = SHADERGRAPH_LOAD_CUSTOM_BLIT_INPUT({1}.xy);", GetVariableNameForSlot(0), GetVariableNameForSlot(1));
            sb.AppendLine(result);
        }
            /*
            protected override MethodInfo GetFunctionToConvert()
            {
                return GetType().GetMethod(string.Format("Unity_CustomBlitInput_{0}", GetCurrentInputTextureType()), BindingFlags.Static | BindingFlags.NonPublic);
            }

            static string Unity_CustomBlitInput_SceneColor(
                [Slot(0, Binding.ScreenPosition)] Vector4 UV,
                [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
            {
                Out = Vector4.one;
                return
                    @"{
                        Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);
                    }
                    ";
            }
            static string Unity_CustomBlitInput_CustomColor(
                [Slot(0, Binding.ScreenPosition)] Vector4 UV,
                [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
            {
                Out = Vector4.one;
                return
                    @"{
                        Out = SHADERGRAPH_LOAD_CUSTOM_BLIT_INPUT(UV.xy);
                    }
                    ";
            }
            static string Unity_CustomBlitInput_SceneDepth(
                [Slot(0, Binding.ScreenPosition)] Vector4 UV,
                [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
            {
                Out = Vector4.one;
                return
                    @"{
                        Out = SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy);
                    }
                    ";
            }
            static string Unity_CustomBlitInput_CustomDepth(
                [Slot(0, Binding.ScreenPosition)] Vector4 UV,
                [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
            {
                Out = Vector4.one;
                return
                    @"{
                        Out = SHADERGRAPH_LOAD_CUSTOM_BLIT_INPUT(UV.xy);
                    }
                    ";
            }
            static string Unity_CustomBlitInput_Normal(
                [Slot(0, Binding.ScreenPosition)] Vector4 UV,
                [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
            {
                Out = Vector4.one;
                return
                    @"{
                        Out = SHADERGRAPH_LOAD_CUSTOM_BLIT_INPUT(UV.xy);
                    }
                    ";
            }
            */
            public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}

