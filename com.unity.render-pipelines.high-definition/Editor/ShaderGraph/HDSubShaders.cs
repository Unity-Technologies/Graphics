using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDSubShaders
    {
        public static SubShaderDescriptor StackLit = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.StackLit.ShadowCaster },
                { HDPasses.StackLit.META },
                { HDPasses.StackLit.SceneSelection },
                { HDPasses.StackLit.DepthForwardOnly },
                { HDPasses.StackLit.MotionVectors },
                { HDPasses.StackLit.Distortion, new FieldCondition(HDFields.TransparentDistortion, true) },
                { HDPasses.StackLit.ForwardOnly },
            },
            // customEditorOverride = "Rendering.HighDefinition.StackLitGUI",
        };
    }
}