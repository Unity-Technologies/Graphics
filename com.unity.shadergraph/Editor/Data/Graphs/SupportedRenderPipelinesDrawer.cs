using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    // TODO: change this to utility class (we don't need the material drawer)
    public static class SupportedRenderPipelineUtils
    {
        public static readonly string All = "All";
        public static readonly string None = "None";

        public static string GetRenderPipelineName(RenderPipelineAsset rp) => GetRenderPipelineName(rp.GetType());
        public static string GetRenderPipelineName(Type renderPipelineType) => renderPipelineType.Name;
    }
}