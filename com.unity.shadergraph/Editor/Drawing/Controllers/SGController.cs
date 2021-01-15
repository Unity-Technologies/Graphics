using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph
{
    struct SGControllerChangedEvent
    {
        public ISGControlledElement target;
        public SGController controller;
        public int change;

        bool m_PropagationStopped;
        public void StopPropagation()
        {
            m_PropagationStopped = true;
        }

        public bool isPropagationStopped
        { get { return m_PropagationStopped; } }
    }

    class SGControllerEvent
    {
        public ISGControlledElement target = null;

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

        public void RegisterHandler(ISGControlledElement handler)
        {
            //Debug.Log("RegisterHandler  of " + handler.GetType().Name + " on " + GetType().Name );

            if (m_EventHandlers.Contains(handler))
                Debug.LogError("Handler registered twice");
            else
            {
                m_EventHandlers.Add(handler);

                NotifyEventHandler(handler, AnyThing);
            }
        }

        public void UnregisterHandler(ISGControlledElement handler)
        {
            m_EventHandlers.Remove(handler);
        }

        public const int AnyThing = -1;

        protected void NotifyChange(int eventID)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                Profiler.BeginSample("NotifyChange:" + eventHandler.GetType().Name);
                NotifyEventHandler(eventHandler, eventID);
                Profiler.EndSample();
            }
        }

        void NotifyEventHandler(ISGControlledElement eventHandler, int eventID)
        {
            SGControllerChangedEvent e = new SGControllerChangedEvent();
            e.controller = this;
            e.target = eventHandler;
            e.change = eventID;
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

        protected GraphDataStore graphDataStore => m_GraphDataStore;

        // Holds data specific to the views this controller is responsible for
        T m_ViewModel;
        protected SGViewController(T viewModel, GraphDataStore graphDataStore)
        {
            m_ViewModel = viewModel;
            m_GraphDataStore = graphDataStore;
            ModelChanged(Model);
        }

        // This function is meant to be defined by child classes and lets them provide their own change identifiers, of which it then becomes the child class's responsibility to associate those change IDs with a certain IGraphDataAction
        protected abstract void ChangeModel(int ChangeID);

        public virtual void ModelChanged(GraphData graphData)
        {
            // Lets all event handlers this controller owns/manages know that the model has changed
            // Usually this is to update views and make them reconstruct themself from updated view-model
            NotifyChange(AnyThing);
            // Reconstruct view-model first
            ViewModel.ConstructFromModel(Model);
            // Let child controllers know about changes to this controller so they may update themselves in turn
            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        public T ViewModel => m_ViewModel;
        public GraphData Model => m_GraphDataStore.State;
    }
}
