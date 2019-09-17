using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Experimental.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal
{
    class UniversalMeshTarget : ITargetVariant<MeshTarget>
    {
        public string displayName => "Universal";

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return pipelineAsset is UniversalRenderPipelineAsset;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            switch(masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    subShader = new UniversalPBRSubShader();
                    return true;
                case UnlitMasterNode unlitMasterNode:
                    subShader = new UniversalUnlitSubShader();
                    return true;
                case SpriteLitMasterNode spriteLitMasterNode:
                    subShader = new UniversalSpriteLitSubShader();
                    return true;
                case SpriteUnlitMasterNode spriteUnlitMasterNode:
                    subShader = new UniversalSpriteUnlitSubShader();
                    return true;
                default:
                    subShader = null;
                    return false;
            }
        }
    }
}
