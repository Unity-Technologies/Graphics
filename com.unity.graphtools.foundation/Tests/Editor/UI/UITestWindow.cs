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
#if !UNITY_2022_2_OR_NEWER
        public ModelInspectorView ModelInspectorView => m_SidePanel;
#endif

        public UITestWindow()
        {
#if !UNITY_2022_2_OR_NEWER
            WithSidePanel = false;
#endif
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<UITestGraphTool>(WindowID);
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return true;
        }
    }

    public class UITestWindowWithSidePanel : UITestWindow
    {
        public UITestWindowWithSidePanel()
        {
#if !UNITY_2022_2_OR_NEWER
            WithSidePanel = true;
#endif
        }

        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_2022_2_OR_NEWER
            if (TryGetOverlay(ModelInspectorOverlay.idValue, out var inspectorOverlay))
            {
                inspectorOverlay.displayed = true;
            }
#endif
        }
    }
}
