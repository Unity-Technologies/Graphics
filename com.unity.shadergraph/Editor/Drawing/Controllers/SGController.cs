using System;
using System.Linq;
using System.Collections.Generic;

using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph
{
    class DummyChangeAction : IGraphDataAction
    {
        void OnDummyChangeAction(GraphData m_GraphData)
        {

        }

        public Action<GraphData> ModifyGraphDataAction => OnDummyChangeAction;
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
    }

    abstract class SGController<T> : SGController
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
                Profiler.BeginSample(element.GetType().Name + ".OnDisable");
                element.OnDisable();
                Profiler.EndSample();
            }
        }

        void RegisterHandler(ISGControlledElement handler)
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

        void UnregisterHandler(ISGControlledElement handler)
        {
            m_EventHandlers.Remove(handler);
        }

        protected void NotifyChange(IGraphDataAction changeAction)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                Profiler.BeginSample("NotifyChange:" + eventHandler.GetType().Name);
                NotifyEventHandler(eventHandler, changeAction);
                Profiler.EndSample();
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

        public virtual IEnumerable<SGController<T>> allChildren
        {
            get { return Enumerable.Empty<SGController<T>>(); }
        }

        List<ISGControlledElement> m_EventHandlers = new List<ISGControlledElement>();
    }

    // Using the Curiously Recurring Template Pattern here
    // Generic subclass that provides itself as argument for base class type
    // Allows access to child class functionality in the parent, prevents the parent from growing into a monolithic base class over time
    abstract class SGViewController<T> : SGController<SGViewController<T>> where T : ISGViewModel
    {
        // NOTE: VFX Graph implements models at a base controller level but they also have a unified data model system that we lack which makes it possible, a note for future improvements
        // Holds application specific data
        // TODO : Have a const reference to a data store instead of a raw GraphData reference, to allow for action dispatches
        GraphDataStore m_GraphDataStore;

        protected GraphDataStore GraphDataStore => m_GraphDataStore;

        // Holds data specific to the views this controller is responsible for
        T m_ViewModel;
        protected SGViewController(T viewModel, GraphDataStore graphDataStore)
        {
            m_ViewModel = viewModel;
            m_GraphDataStore = graphDataStore;
            m_GraphDataStore.Subscribe += ModelChanged;
            ModelChanged(Model, DummyChange);
        }

        protected abstract void RequestModelChange(IGraphDataAction changeAction);

        protected abstract void ModelChanged(GraphData graphData, IGraphDataAction changeAction);

        public T ViewModel => m_ViewModel;
        public GraphData Model => m_GraphDataStore.State;
    }
}
