using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset)), HideInInspector]
class ForceDepthPrepassFeatureResources : IRenderPipelineResources
{
    [SerializeField]
    [ResourcePath("Hidden/Universal Render Pipeline/CopyDepth", SearchType.ShaderName)]
    Shader m_CopyDepthPS;
        
    public Shader CopyDepthPS
    {
        get => m_CopyDepthPS;
        set => this.SetValueAndNotify(ref m_CopyDepthPS, value);
    } 

    public bool isAvailableInPlayerBuild => true;
    public int version => 0;
}