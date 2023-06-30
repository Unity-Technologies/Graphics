using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // External debuggers implement this interface to get notified every time a graph is compiled
    // It's up to the debuggers to filter this callback to executions they are interested in
    // E.g You could filter on the graphs name using graph.graph.debugName
    internal interface IRenderGraphDebugger
    {
        public void OutputGraph(NativePassCompiler graph);
    }

    internal partial class NativePassCompiler
    {
        static List<IRenderGraphDebugger> debuggerList = new List<IRenderGraphDebugger>();
        internal static bool hasNativePassData = false;

        internal static void AddRenderGraphDebugger(IRenderGraphDebugger debugger)
        {
            debuggerList.Add(debugger);
        }

        internal void OutputDebugGraph()
        {
            hasNativePassData = true;
            foreach (IRenderGraphDebugger debugger in debuggerList)
            {
                debugger.OutputGraph(this);
            }
        }
    }
}
