using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    // Base class for all controllers of a sub-window within the shader graph
    abstract class GraphSubWindowController<ToolState, SubWindowView> : StateObserver<ToolState>
        where ToolState : IState
        where SubWindowView : VisualElement
    {
        // Actual view content of the SubWindow
        // Must be initialized by child classes as needed
        protected SubWindowView m_SubWindowContent;

        // Reference to the GTF command dispatcher
        protected CommandDispatcher m_CommandDispatcher;

        protected GraphSubWindowController(CommandDispatcher dispatcher)
        {
            m_CommandDispatcher = dispatcher;
        }
    }
}
