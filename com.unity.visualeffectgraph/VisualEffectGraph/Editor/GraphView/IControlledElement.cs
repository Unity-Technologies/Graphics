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
        public static T CreateInstance<T>() where T : new()
        {
            return new T();
        }

        public static Controller CreateInstance(Type t)
        {
            return System.Activator.CreateInstance(t) as Controller;
        }

        public virtual void OnEnable()
        {
            //hideFlags = HideFlags.HideAndDontSave;
        }

        public virtual void OnRemoveFromGraph()
        {
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

        public abstract void ApplyChanges();

        List<IEventHandler> m_EventHandlers = new List<IEventHandler>();
    }

    abstract class Controller<T> : Controller where T : UnityEngine.Object
    {
        [SerializeField]
        T m_Model;

        [SerializeField]
        IDataWatchHandle m_Handle;


        public virtual void Init(T model)
        {
            m_Model = model;

            m_Handle = DataWatchService.sharedInstance.AddWatch(m_Model, ModelChanged);
        }

        public virtual void OnDisable()
        {
            DataWatchService.sharedInstance.RemoveWatch(m_Handle);
        }

        protected abstract void ModelChanged(UnityEngine.Object obj);


        public virtual  IEnumerable<Controller> allChildren
        {
            get { return Enumerable.Empty<Controller>(); }
        }

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

    interface IControlledElement
    {
        Controller controller
        {
            get;
        }
    }

    interface IControlledElement<T> : IControlledElement where T : Controller
    {
        new T controller
        {
            get;
            set;
        }
    }
}
