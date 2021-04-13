using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

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
    // Allows access to child class functionality in the parent
    abstract class SGViewController<T> : SGController<SGViewController<T>> where T : SGViewModel
    {
        // Holds application specific data
        GraphData m_Model;

        // Holds data specific to the views this controller is responsible for
        T m_ViewModel;
        protected SGViewController(T viewModel, GraphData graphData)
        {
            m_ViewModel = viewModel;
            m_Model = graphData;
            m_ViewModel.ConstructFromModel(m_Model);
        }

        protected abstract void ModelChanged(GraphData graphData);

        public override void ApplyChanges()
        {
            ModelChanged(Model);
            ViewModel.ConstructFromModel(Model);
            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        public T ViewModel => m_ViewModel;
        public GraphData Model => m_Model;
    }
}
