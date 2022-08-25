using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

using PreviewRenderMode = UnityEditor.ShaderGraph.GraphDelta.HeadlessPreviewManager.PreviewRenderMode;

namespace UnityEditor.ShaderGraph.GraphUI
{

    public static class PreviewCommands
    {
        public static void RegisterCommandHandlers(
            BaseGraphTool graphTool,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            ShaderGraphModel shaderGraphModel,
            Dispatcher commandDispatcher,
            GraphViewModel graphViewModel)
        {
            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, ChangePreviewExpandedCommand>(
                ChangePreviewExpandedCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                previewUpdateDispatcher
            );

            commandDispatcher.RegisterCommandHandler<ShaderGraphModel, PreviewUpdateDispatcher, ChangePreviewMeshCommand>(
                ChangePreviewMeshCommand.DefaultCommandHandler,
                shaderGraphModel,
                previewUpdateDispatcher
            );

            commandDispatcher.RegisterCommandHandler<ShaderGraphModel, PreviewUpdateDispatcher, ChangePreviewZoomCommand>(
                ChangePreviewZoomCommand.DefaultCommandHandler,
                shaderGraphModel,
                previewUpdateDispatcher
            );

            commandDispatcher.RegisterCommandHandler<ShaderGraphModel, PreviewUpdateDispatcher, ChangePreviewRotationCommand>(
                ChangePreviewRotationCommand.DefaultCommandHandler,
                shaderGraphModel,
                previewUpdateDispatcher
            );

            commandDispatcher.RegisterCommandHandler<ShaderGraphModel, PreviewUpdateDispatcher, ChangePreviewSizeCommand>(
                ChangePreviewSizeCommand.DefaultCommandHandler,
                shaderGraphModel,
                previewUpdateDispatcher
            );

            commandDispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, PreviewUpdateDispatcher, ChangePreviewModeCommand>(
                ChangePreviewModeCommand.DefaultCommandHandler,
                graphTool.UndoStateComponent,
                graphViewModel.GraphModelState,
                previewUpdateDispatcher
            );
        }
    }

    public class ChangePreviewExpandedCommand : ModelCommand<IGraphElementModel>
    {
        bool m_IsPreviewExpanded;
        public ChangePreviewExpandedCommand(bool isPreviewExpanded, IReadOnlyList<IGraphElementModel> models)
            : base("Change Preview Expansion", "Change Previews Expansion", models)
        {
            m_IsPreviewExpanded = isPreviewExpanded;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphViewState,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            ChangePreviewExpandedCommand command
        )
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                foreach (var graphElementModel in command.Models)
                {
                    if(graphElementModel is GraphDataNodeModel graphDataNodeModel)
                        graphDataNodeModel.IsPreviewExpanded = command.m_IsPreviewExpanded;
                    graphUpdater.MarkChanged(command.Models, ChangeHint.Layout);
                }
            }
        }
    }

    public class ChangePreviewMeshCommand : ICommand
    {
        public const string UserPrefsKey = "PreviewMesh";

        Mesh m_NewPreviewMesh;
        bool m_LockPreviewRotation;

        public ChangePreviewMeshCommand(Mesh newPreviewMesh, bool lockPreviewRotation)
        {
            m_NewPreviewMesh = newPreviewMesh;
            m_LockPreviewRotation = lockPreviewRotation;
        }

        public static void DefaultCommandHandler(
            ShaderGraphModel graphModel,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            ChangePreviewMeshCommand command
        )
        {
            // TODO: The preview data should be stored in user prefs
            // Otherwise this might be undoable.
            graphModel.MainPreviewData.mesh = command.m_NewPreviewMesh;
            graphModel.MainPreviewData.lockMainPreviewRotation = command.m_LockPreviewRotation;

            // Lets the preview manager know to re-render the main preview output
            previewUpdateDispatcher.OnMainPreviewDataChanged();
        }
    }


    public class ChangePreviewModeCommand : ModelCommand<GraphDataNodeModel>
    {
        PreviewRenderMode m_PreviewMode;

        // Needed for the ModelPropertyField used by the SGNodeFieldsInspector
        public ChangePreviewModeCommand(object previewMode, IModel model)
            : base("Change Preview Mode", "Change Preview Modes", new []{ model as GraphDataNodeModel })
        {
            m_PreviewMode = (PreviewRenderMode)previewMode;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            ChangePreviewModeCommand command
        )
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using(var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var graphDataNodeModel in command.Models)
                {
                    graphDataNodeModel.NodePreviewMode = command.m_PreviewMode;
                    graphUpdater.MarkChanged(command.Models);
                }
            }

            // Because every nodes preview mode can affect the modes of those downstream of it
            // we first want to set the preview mode of all the nodes that are being modified
            foreach (var graphDataNodeModel in command.Models)
            {
                graphDataNodeModel.NodePreviewMode = command.m_PreviewMode;
            }

            // After all the nodes preview modes are set, go through the nodes again
            // and concretize the preview modes that are set to inherit for preview data
            //foreach (var graphDataNodeModel in command.Models)
            //{
            //    previewUpdateDispatcher.OnPreviewModeChanged(graphDataNodeModel.graphDataName, command.m_PreviewMode);
            //}
        }
    }

    public class ChangePreviewZoomCommand : ICommand
    {
        public const string UserPrefsKey = "PreviewZoom";

        float m_NewPreviewZoom;
        public ChangePreviewZoomCommand(float newPreviewZoom)
        {
            m_NewPreviewZoom = newPreviewZoom;
        }

        public static void DefaultCommandHandler(
            ShaderGraphModel graphModel,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            ChangePreviewZoomCommand command
        )
        {
            graphModel.MainPreviewData.scale += command.m_NewPreviewZoom;

            // Lets the preview manager know to re-render the main preview output
            previewUpdateDispatcher.OnMainPreviewDataChanged();
        }
    }

    public class ChangePreviewRotationCommand : ICommand
    {
        public const string UserPrefsKey = "PreviewRotation";

        Quaternion m_NewPreviewRotation;
        public ChangePreviewRotationCommand(Quaternion newPreviewRotation)
        {
            m_NewPreviewRotation = newPreviewRotation;
        }

        public static void DefaultCommandHandler(
            ShaderGraphModel graphModel,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            ChangePreviewRotationCommand command
        )
        {
            graphModel.MainPreviewData.rotation = command.m_NewPreviewRotation;

            // Lets the preview manager know to re-render the main preview output
            previewUpdateDispatcher.OnMainPreviewDataChanged();
        }
    }

    public class ChangePreviewSizeCommand : ICommand
    {
        public const string UserPrefsKey = "PreviewSize";

        Vector2 m_NewPreviewSize;
        public ChangePreviewSizeCommand(Vector2 newPreviewSize)
        {
            m_NewPreviewSize = newPreviewSize;
        }

        public static void DefaultCommandHandler(
            ShaderGraphModel graphModel,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            ChangePreviewSizeCommand command
        )
        {
            graphModel.MainPreviewData.mainPreviewSize = command.m_NewPreviewSize;

            // Lets the preview manager know to re-render the main preview outputs
            previewUpdateDispatcher.OnMainPreviewDataChanged();
        }
    }
}
