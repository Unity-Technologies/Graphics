//using UnityEditor.GraphToolsFoundation.Overdrive;
//using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
//using UnityEngine.GraphToolsFoundation.CommandStateObserver;

//namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch
//{
//    public class GraphWindowTickCommand : ICommand
//    {
//        public static void DefaultCommandHandler(
//            GraphViewStateComponent graphViewState,
//            GraphWindowTickCommand command
//        )
//        {
//            if (graphToolState is ShaderGraphState shaderGraphState)
//            {
//                using var previewUpdater = shaderGraphState.GraphPreviewState.UpdateScope;
//                {
//                    previewUpdater.GraphWindowTick();
//                }
//            }
//        }
//    }
//}
