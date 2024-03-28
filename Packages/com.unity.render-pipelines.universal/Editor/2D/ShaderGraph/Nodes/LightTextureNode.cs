using System;
using UnityEditor.Graphing;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    enum BlendStyle
    {
        LightTex0,
        LightTex1,
        LightTex2,
        LightTex3,
    }

    [Title("Input", "2D", "Light Texture")]
    [SubTargetFilterAttribute(new[] { typeof(UniversalSpriteCustomLitSubTarget), typeof(UniversalSpriteUnlitSubTarget)})]
    class LightTextureNode : AbstractMaterialNode, IGeneratesFunction
    {
        private const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        [SerializeField] private BlendStyle m_BlendStyle = BlendStyle.LightTex0;

        [EnumControl("")]
        public BlendStyle blendStyle
        {
            get { return m_BlendStyle; }
            set
            {
                if (m_BlendStyle == value)
                    return;

                m_BlendStyle = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public LightTextureNode()
        {
            name = "2D Light Texture";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        string GetVariableName()
        {
            return $"_ShapeLightTexture{(int)m_BlendStyle}";
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return $"Unity_GetLightTexture{(int)m_BlendStyle}()";
        }


        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl", true);
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightVariables.hlsl");

            registry.ProvideFunction($"Unity_GetLightTexture{(int)m_BlendStyle}", s =>
            {
                s.AppendLine($"UnityTexture2D Unity_GetLightTexture{(int)m_BlendStyle}()");
                using (s.BlockScope())
                {
                    s.AppendLine("#if USE_SHAPE_LIGHT_TYPE_0 || USE_SHAPE_LIGHT_TYPE_1 || USE_SHAPE_LIGHT_TYPE_2 || USE_SHAPE_LIGHT_TYPE_3");
                    s.AppendLine("    return " + $"UnityBuildTexture2DStructNoScale(" + GetVariableName() + ");");
                    s.AppendLine("#else");
                    s.AppendLine("    return " + $"UnityBuildTexture2DStructNoScale(_DefaultWhiteTex);");
                    s.AppendLine("#endif");
                }
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Texture2DShaderProperty()
            {
                overrideReferenceName = GetVariableName(),
                generatePropertyBlock = false,
                defaultType = Texture2DShaderProperty.DefaultType.White,
                // value = m_Texture,
                modifiable = false
            });
        }
    }
}
