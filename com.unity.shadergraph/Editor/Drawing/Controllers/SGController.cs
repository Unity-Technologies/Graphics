using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;

using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph
{
    class DummyChangeAction : IGraphDataAction
    {
        void OnDummyChangeAction(GraphData m_GraphData)
        {
        }

        public Action<GraphData> modifyGraphDataAction => OnDummyChangeAction;
    }

    struct SGControllerChangedEvent
    {
        public ISGControlledElement target;
        public SGController controller;
        public IGraphDataAction change;

        private bool m_PropagationStopped;
        void StopPropagation()
        {
            m_PropagationStopped = true;
        }

        public bool isPropagationStopped => m_PropagationStopped;
    }

    class SGControllerEvent
    {
        ISGControlledElement target = null;

        SGControllerEvent(ISGControlledElement controlledTarget)
        {
            target = controlledTarget;
        }
    }

    abstract class SGController
    {
        public bool m_DisableCalled = false;

        protected IGraphDataAction DummyChange = new DummyChangeAction();

        public virtual void OnDisable()
        {
            if (m_DisableCalled)
                Debug.LogError(GetType().Name + ".Disable called twice");

            m_DisableCalled = true;
            foreach (var element in allChildren)
            {
                UnityEngine.Profiling.Profiler.BeginSample(element.GetType().Name + ".OnDisable");
                element.OnDisable();
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        internal void RegisterHandler(ISGControlledElement handler)
        {
            //Debug.Log("RegisterHandler  of " + handler.GetType().Name + " on " + GetType().Name );

            if (m_EventHandlers.Contains(handler))
                Debug.LogError("Handler registered twice");
            else
            {
                m_EventHandlers.Add(handler);

                NotifyEventHandler(handler, DummyChange);
            }
        }

        internal void UnregisterHandler(ISGControlledElement handler)
        {
            m_EventHandlers.Remove(handler);
        }

        protected void NotifyChange(IGraphDataAction changeAction)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                UnityEngine.Profiling.Profiler.BeginSample("NotifyChange:" + eventHandler.GetType().Name);
                NotifyEventHandler(eventHandler, changeAction);
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        void NotifyEventHandler(ISGControlledElement eventHandler, IGraphDataAction changeAction)
        {
            SGControllerChangedEvent e = new SGControllerChangedEvent();
            e.controller = this;
            e.target = eventHandler;
            e.change = changeAction;
            eventHandler.OnControllerChanged(ref e);
            if (e.isPropagationStopped)
                return;
            if (eventHandler is VisualElement)
            {
                var element = eventHandler as VisualElement;
                eventHandler = element.GetFirstOfType<ISGControlledElement>();
                while (eventHandler != null)
                {
                    eventHandler.OnControllerChanged(ref e);
                    if (e.isPropagationStopped)
                        break;
                    eventHandler = (eventHandler as VisualElement).GetFirstAncestorOfType<ISGControlledElement>();
                }
            }
        }

        public void SendEvent(SGControllerEvent e)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.OnControllerEvent(e);
            }
        }

        public abstract void ApplyChanges();

        public virtual IEnumerable<SGController> allChildren
        {
            get { return Enumerable.Empty<SGController>(); }
        }

        protected List<ISGControlledElement> m_EventHandlers = new List<ISGControlledElement>();
    }

    abstract class SGController<T> : SGController
    {
        GraphDataStore m_DataStore;
        protected GraphDataStore DataStore => m_DataStore;

        protected SGController(T model, GraphDataStore dataStore)
        {
            m_Model = model;
            m_DataStore = dataStore;
            DataStore.Subscribe += ModelChanged;
        }

        protected abstract void RequestModelChange(IGraphDataAction changeAction);

        protected abstract void ModelChanged(GraphData graphData, IGraphDataAction changeAction);

        T m_Model;
        public T Model => m_Model;

        // Cleanup delegate association before destruction
        public void Cleanup()
        {
            if (m_DataStore == null)
                return;
            DataStore.Subscribe -= ModelChanged;
            m_Model = default;
            m_DataStore = null;
        }
    }

    abstract class SGViewController<ModelType, ViewModelType> : SGController<ModelType>
    {
        protected SGViewController(ModelType model, ViewModelType viewModel, GraphDataStore graphDataStore) : base(model, graphDataStore)
        {
            m_ViewModel = viewModel;
            try
            {
                // Need ViewModel to be initialized before we call ModelChanged() [as view model might need to update]
                ModelChanged(DataStore.State, DummyChange);
            }
            catch (Exception e)
            {
                Debug.Log("Failed to initialize View Controller of type: " + this.GetType() + " due to exception: " + e);
            }
        }

        // Holds data specific to the views this controller is responsible for
        ViewModelType m_ViewModel;
        public ViewModelType ViewModel => m_ViewModel;

        public override void ApplyChanges()
        {
            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        public virtual void Dispose()
        {
            m_EventHandlers.Clear();
            m_ViewModel = default;
        }
    }
}
