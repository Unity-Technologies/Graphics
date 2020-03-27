using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDPasses
    {
        static string GetPassTemplatePath(string materialName)
        {
            return $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/{materialName}/ShaderGraph/{materialName}Pass.template";
        }

        static class HDStructCollections
        {
            public static StructCollection Default = new StructCollection
            {
                { HDStructs.AttributesMesh },
                { HDStructs.VaryingsMeshToPS },
                { Structs.SurfaceDescriptionInputs },
                { Structs.VertexDescriptionInputs },
            };
        }
    }
}
