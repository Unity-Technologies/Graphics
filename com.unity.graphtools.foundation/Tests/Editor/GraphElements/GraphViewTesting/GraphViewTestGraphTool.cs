using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphViewTestGraphTool : BaseGraphTool
    {
        public static readonly string toolName = "GTF Tests";

        public GraphViewTestGraphTool()
        {
            Name = toolName;
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            WantsTransientPrefs = true;
            base.InitState();
            Preferences.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);
        }
    }
}
