using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class UITestGraphTool : BaseGraphTool
    {
        public static readonly string toolName = "UI Tests";

        public UITestGraphTool()
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

    public class UITestWindow : GraphViewEditorWindow
    {
        public UITestWindow()
        {
            WithSidePanel = false;
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<UITestGraphTool>();
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return true;
        }
    }
}
