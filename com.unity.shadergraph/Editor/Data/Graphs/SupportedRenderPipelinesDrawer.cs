using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    public class SupportedRenderPipelinesDecorator : MaterialPropertyDrawer
    {
        public static readonly string All = "All";
        public static readonly string None = "None";
        string[] supportedRPs;
        bool noneSupported = false;

        public SupportedRenderPipelinesDecorator(params string[] renderpipelines)
        {
            supportedRPs = renderpipelines;
            if (supportedRPs.Contains(None))
                noneSupported = true;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) => 0;

        public bool IsCurrentlySupported()
        {
            if (noneSupported)
                return false;

            string name = GetRenderPipelineName(RenderPipelineManager.currentPipeline);
            return supportedRPs.Contains(name);
        }

        public override void OnGUI (Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            EditorGUILayout.LabelField("HRGFIYWGFYWGIFU");
            if (!IsCurrentlySupported())
                return ;

            base.OnGUI(position, prop, label, editor);
        }

        public static string GetRenderPipelineName(RenderPipeline rp) => GetRenderPipelineName(rp.GetType());
        public static string GetRenderPipelineName(Type renderPipelineType) => renderPipelineType.Name;
    }
}