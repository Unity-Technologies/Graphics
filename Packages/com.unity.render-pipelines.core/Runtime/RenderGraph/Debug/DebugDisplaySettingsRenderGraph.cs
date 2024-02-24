using System;
using UnityEngine.Rendering.RenderGraphModule;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Render Graph-related Rendering Debugger settings.
    /// </summary>
    class DebugDisplaySettingsRenderGraph : IDebugDisplaySettingsData
    {
        public DebugDisplaySettingsRenderGraph()
        {
            foreach (var graph in RenderGraph.GetRegisteredRenderGraphs())
            {
                graph.debugParams.Reset();
            }
        }

        [DisplayInfo(name = "Render Graph", order = 10)]
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Render Graph";
            public SettingsPanel(DebugDisplaySettingsRenderGraph _)
            {
                bool usingRenderGraph = false;
                foreach (var graph in RenderGraph.GetRegisteredRenderGraphs())
                {
                    usingRenderGraph = true;
                    var list = graph.GetWidgetList();
                    foreach (var item in list)
                        AddWidget(item);
                }

                if (!usingRenderGraph)
                {
                    AddWidget(new DebugUI.MessageBox()
                    {
                        displayName =
                            "Warning: The current render pipeline does not have Render Graphs Registered",
                        style = DebugUI.MessageBox.Style.Warning
                    });
                }
            }
        }

        #region IDebugDisplaySettingsQuery

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        public bool AreAnySettingsActive
        {
            get
            {
                foreach (var graph in RenderGraph.GetRegisteredRenderGraphs())
                {
                    if (graph.areAnySettingsActive)
                        return true;
                }

                return false;
            }
        }

        #endregion
    }
}
