using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphGraphTool: BaseGraphTool
    {
        public static readonly string toolName = "Shader Graph";

        public PreviewManager previewManager { get; set; }

        public ShaderGraphGraphTool()
        {
            Name = toolName;
        }
    }

}
