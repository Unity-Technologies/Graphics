using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPMeshTarget : ITargetVariant<MeshTarget>
    {
        public string displayName => "HDRP";

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return pipelineAsset is HDRenderPipelineAsset;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            switch(masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    subShader = new HDPBRSubShader();
                    return true;
                case UnlitMasterNode unlitMasterNode:
                    subShader = new UnlitSubShader();
                    return true;
                case DecalMasterNode decalMasterNode:
                    subShader = new DecalSubShader();
                    return true;
                case EyeMasterNode eyeMasterNode:
                    subShader = new EyeSubShader();
                    return true;
                case FabricMasterNode fabricMasterNode:
                    subShader = new FabricSubShader();
                    return true;
                case HairMasterNode hairMasterNode:
                    subShader = new HairSubShader();
                    return true;
                case HDLitMasterNode hdLitMasterNode:
                    subShader = new HDLitSubShader();
                    return true;
                case HDUnlitMasterNode hdUnlitMasterNode:
                    subShader = new HDUnlitSubShader();
                    return true;
                case StackLitMasterNode stackLitMasterNode:
                    subShader = new StackLitSubShader();
                    return true;
                default:
                    subShader = null;
                    return false;
            }
        }
    }
}
