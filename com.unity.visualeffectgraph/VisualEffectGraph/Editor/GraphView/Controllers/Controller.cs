using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
    public abstract class Controller
    {
        public virtual void OnDisable()
        {
            foreach (var element in allChildren)
            {
                element.OnDisable();
            }
        }

        public void RegisterHandler(IEventHandler handler)
        {
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
                NotifyEventHandler(eventHandler, eventID);
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

        IDataWatchHandle m_Handle;


        public Controller(T model)
        {
            m_Model = model;
            if( m_Model != null)
            m_Handle = DataWatchService.sharedInstance.AddWatch(m_Model, OnModelChanged);
        }

        public override void OnDisable()
        {
            if (m_Handle != null)
            {
            try
            {
                if( m_Handle != null)
                {
                DataWatchService.sharedInstance.RemoveWatch(m_Handle);
                m_Handle = null;
            }
            }
            catch (ArgumentException e)
            {
                Debug.LogError("handle on Controller" + GetType().Name + " was probably removed twice");
            }
            }
            base.OnDisable();
        }

        void OnModelChanged(UnityEngine.Object obj)
        {
            if (m_Handle != null)
                ModelChanged(obj);
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
        public VFXController(T model) : base(model)
        {
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
