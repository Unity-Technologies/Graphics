using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.VFX
{
    public class VFXGraphTool : BaseGraphTool
    {
        public static readonly string toolName = "VFX Graph Editor";

        public VFXGraphTool()
        {
            Name = toolName;
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            base.InitState();
            Preferences.SetInitialSearcherSize(SearcherService.Usage.CreateNode, new Vector2(375, 300), 2.0f);
        }
    }
}
