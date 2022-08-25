using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.UIElements;

using PreviewRenderMode = UnityEditor.ShaderGraph.GraphDelta.PreviewService.PreviewRenderMode;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Is responsible for taking in change lists and sending requests for updates to the preview service
    /// </summary>
    public class PreviewUpdateDispatcher
    {
        PreviewService m_PreviewHandlerInstance;
        MainPreviewData m_MainPreviewData;
        // TODO: Should this communicate with the graph model through an interface layer?
        // Would allow for anyone (in theory) to hook up their own graph model and use our preview dispatcher
        ShaderGraphModel m_GraphModel;

        // TODO: Remove and replace with reference to preview service
        IVisualElementScheduler m_Scheduler;

        HashSet<string> m_TimeDependentNodes;
        double m_LastTimedUpdateTime;
        EditorWindow m_OwningWindowReference;

        int PreviewWidth => Mathf.FloorToInt(m_MainPreviewData.mainPreviewSize.x);
        int PreviewHeight => Mathf.FloorToInt(m_MainPreviewData.mainPreviewSize.y);

        /// <summary>
        /// Provides this preview update dispatcher with necessary resources for initialization
        /// </summary>
        /// <param name="owningWindow"> Reference to the EditorWindow that is requesting for preview updates </param>
        /// <param name="shaderGraphModel"> Source of the Graph Data needed to generate previews </param>
        /// <param name="previewUpdateReceiver"></param>
        public void Initialize(
            EditorWindow owningWindow,
            ShaderGraphModel shaderGraphModel,
            IPreviewUpdateReceiver previewUpdateReceiver,
            IVisualElementScheduler scheduler)
        {
            m_GraphModel = shaderGraphModel;
            m_MainPreviewData = shaderGraphModel.MainPreviewData;

            m_Scheduler = scheduler;

            m_TimeDependentNodes = new();
            m_OwningWindowReference = owningWindow;

            m_PreviewHandlerInstance = new PreviewService();
            m_PreviewHandlerInstance.Initialize(shaderGraphModel.DefaultContextName, m_MainPreviewData.mainPreviewSize);
            m_PreviewHandlerInstance.SetActiveGraph(shaderGraphModel.GraphHandler);
            m_PreviewHandlerInstance.SetActiveRegistry(shaderGraphModel.RegistryInstance.Registry);
            m_PreviewHandlerInstance.SetActiveTarget(shaderGraphModel.ActiveTarget);
            m_PreviewHandlerInstance.SetPreviewUpdateReceiver(previewUpdateReceiver);

            // Request preview data for any nodes that exist on graph load
            // TODO: Think about how to auto-persist this so we don't need to recalculate every time
            foreach (var nodeModel in m_GraphModel.NodeModels)
            {
                switch (nodeModel)
                {
                    case GraphDataContextNodeModel contextNode when contextNode.IsMainContextNode():
                        OnListenerAdded(contextNode.graphDataName, contextNode.NodePreviewMode, m_GraphModel.DoesNodeRequireTime(contextNode));
                        break;
                    case GraphDataNodeModel graphDataNodeModel when graphDataNodeModel.HasPreview:
                        OnListenerAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.NodePreviewMode, m_GraphModel.DoesNodeRequireTime(graphDataNodeModel));
                        break;
                }
            }
        }

        void RequestPreviewUpdate(string nodeName, PreviewRenderMode previewRenderMode = PreviewRenderMode.Preview2D, bool forceRender = false)
        {
            if (nodeName == m_GraphModel.DefaultContextName)
                m_PreviewHandlerInstance.RequestMainPreviewUpdate(m_Scheduler,
                    PreviewWidth,
                    PreviewHeight,
                    m_MainPreviewData.mesh,
                    m_MainPreviewData.scale,
                    m_MainPreviewData.lockMainPreviewRotation,
                    m_MainPreviewData.rotation,
                    forceRender);
            else
                m_PreviewHandlerInstance.RequestNodePreviewUpdate(nodeName, m_Scheduler, previewRenderMode, forceRerender: forceRender);
        }

        public void OnListenerAdded(string listenerID, PreviewRenderMode previewRenderMode, bool isListenerTimeDependent)
        {
            if (isListenerTimeDependent)
                m_TimeDependentNodes.Add(listenerID);

            RequestPreviewUpdate(listenerID, previewRenderMode);
        }

        public void OnListenerConnectionChanged(string listenerID, bool wasNodeDeleted = false)
        {
            var impactedNodes = m_PreviewHandlerInstance.NotifyNodeFlowChanged(listenerID, wasNodeDeleted);

            var doesNodeRequireTime = m_GraphModel.DoesNodeRequireTime(listenerID);
            if (doesNodeRequireTime)
                m_TimeDependentNodes.Add(listenerID);

            // If a node was deleted we don't want to issue update calls for it thereafter
            if (wasNodeDeleted)
            {
                impactedNodes.Remove(listenerID);
                m_TimeDependentNodes.Remove(listenerID);
            }

            foreach (var downstreamNode in impactedNodes)
            {
                // Also make sure that downstream nodes are added/removed to time-dependent nodes when connections change
                if (wasNodeDeleted)
                    m_TimeDependentNodes.Remove(downstreamNode);
                else if(doesNodeRequireTime)
                    m_TimeDependentNodes.Add(downstreamNode);

                RequestPreviewUpdate(downstreamNode);
            }
        }

        public void OnGlobalPropertyChanged(string propertyName, object newValue)
        {
            var linkedVariableNodes =  m_GraphModel.GetLinkedVariableNodes(propertyName);

            var variableNodeNames = new List<string>();
            foreach(var node in linkedVariableNodes)
            {
                var nodeModel = node as GraphDataVariableNodeModel;
                variableNodeNames.Add(nodeModel.graphDataName);
            }

            var impactedNodes = m_PreviewHandlerInstance.SetGlobalProperty(propertyName, newValue, variableNodeNames);
            foreach (var downstreamNode in impactedNodes)
                RequestPreviewUpdate(downstreamNode);
        }

        public void OnLocalPropertyChanged(string nodeName, string propertyName, object newValue)
        {
            var impactedNodes = m_PreviewHandlerInstance.SetLocalProperty(nodeName, propertyName, newValue);
            foreach (var downstreamNode in impactedNodes)
                RequestPreviewUpdate(downstreamNode);
        }

        public void OnMainPreviewDataChanged()
        {
            m_PreviewHandlerInstance.RequestMainPreviewUpdate(
                m_Scheduler,
                PreviewWidth,
                PreviewHeight,
                m_MainPreviewData.mesh,
                m_MainPreviewData.scale,
                m_MainPreviewData.lockMainPreviewRotation,
                m_MainPreviewData.rotation);
        }

        bool TimedNodesShouldUpdate(EditorWindow editorWindow)
        {
            // get current screen FPS, clamp to what we consider a valid range
            // this is probably not accurate for multi-monitor.. but should be relevant to at least one of the monitors
            double monitorFPS = Screen.currentResolution.refreshRate + 1.0;  // +1 to round up, since it is an integer and rounded down
            if (Double.IsInfinity(monitorFPS) || Double.IsNaN(monitorFPS))
                monitorFPS = 60.0f;
            monitorFPS = Math.Min(monitorFPS, 144.0);
            monitorFPS = Math.Max(monitorFPS, 30.0);

            var curTime = EditorApplication.timeSinceStartup;
            var deltaTime = curTime - m_LastTimedUpdateTime;
            bool isFocusedWindow = (EditorWindow.focusedWindow == editorWindow);

            // we throttle the update rate, based on whether the window is focused and if unity is active
            const double k_AnimatedFPS_WhenNotFocused = 10.0;
            const double k_AnimatedFPS_WhenInactive = 2.0;
            double maxAnimatedFPS =
                (UnityEditorInternal.InternalEditorUtility.isApplicationActive ?
                    (isFocusedWindow ? monitorFPS : k_AnimatedFPS_WhenNotFocused) :
                    k_AnimatedFPS_WhenInactive);

            bool update = (deltaTime > (1.0 / maxAnimatedFPS));
            if (update)
                m_LastTimedUpdateTime = curTime;
            return update;
        }

        public void Update()
        {
            if (m_OwningWindowReference == null)
                return;

            if (TimedNodesShouldUpdate(m_OwningWindowReference))
                foreach (var timeDependentNode in m_TimeDependentNodes)
                    RequestPreviewUpdate(timeDependentNode, forceRender: true);
        }

        public void Cleanup()
        {
            m_PreviewHandlerInstance.Cleanup();
        }
    }
}
