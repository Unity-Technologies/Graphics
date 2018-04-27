using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    public abstract class Controller
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

        public void RegisterHandler(IEventHandler handler)
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

        public void UnregisterHandler(IEventHandler handler)
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

        void NotifyEventHandler(IEventHandler eventHandler, int eventID)
        {
            using (var e = ControllerChangedEvent.GetPooled())
            {
                e.controller = this;
                e.change = eventID;
                e.target = eventHandler;
                UIElementsUtility.eventDispatcher.DispatchEvent(e, (eventHandler as VisualElement).panel);
            }
        }

        public void SendEvent(EventBase e)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                e.target = eventHandler;
                UIElementsUtility.eventDispatcher.DispatchEvent(e, (eventHandler as VisualElement).panel);
            }
        }

        public abstract void ApplyChanges();


        public virtual  IEnumerable<Controller> allChildren
        {
            get { return Enumerable.Empty<Controller>(); }
        }

        List<IEventHandler> m_EventHandlers = new List<IEventHandler>();
    }

    abstract class Controller<T> : Controller where T : UnityEngine.Object
    {
        T m_Model;


        public Controller(T model)
        {
            m_Model = model;
        }

        protected abstract void ModelChanged(UnityEngine.Object obj);

        public override void ApplyChanges()
        {
            ModelChanged(model);

            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        public T model { get { return m_Model; } }
    }

    abstract class VFXController<T> : Controller<T> where T : VFXModel
    {
        VFXViewController m_ViewController;

        public VFXController(VFXViewController viewController, T model) : base(model)
        {
            m_ViewController = viewController;
            m_ViewController.RegisterNotification(model, OnModelChanged);
        }

        public VFXViewController viewController {get {return m_ViewController; }}

        public override void OnDisable()
        {
            m_ViewController.UnRegisterNotification(model, OnModelChanged);
            base.OnDisable();
        }

        void OnModelChanged()
        {
            ModelChanged(model);
        }

        public virtual string name
        {
            get
            {
                return model.name;
            }
        }
    }

    public class ControllerChangedEvent : EventBase<ControllerChangedEvent>, IPropagatableEvent
    {
        public Controller controller;

        public int change;
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.Capturable;
            controller = null;
            change = 0;
        }
    }
}
