using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class MathBookGraphTool : BaseGraphTool
    {
        public static readonly string toolName = "Math Book Editor";

        public MathBookGraphTool()
        {
            Name = toolName;
        }

#if UNITY_2022_2_OR_NEWER
        protected override IOverlayToolbarProvider CreateToolbarProvider(string toolbarId)
        {
            return toolbarId == MainOverlayToolbar.toolbarId ?
                new MathBookMainToolbarProvider() :
                base.CreateToolbarProvider(toolbarId);
        }
#endif
    }
}
