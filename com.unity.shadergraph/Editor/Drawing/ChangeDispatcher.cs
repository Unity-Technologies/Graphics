using System;
using System.Collections.Generic;
using Drawing.Views;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ChangeDispatcher
    {
        struct ChangeCallback
        {
            public JsonObject jsonObject;
            public int version;
            public Action action;
        }

        JsonStore m_JsonStore;
        int m_Version;
        List<ChangeCallback> m_Callbacks = new List<ChangeCallback>();
        Queue<int> m_FreeIndices = new Queue<int>();

        public ChangeDispatcher(JsonStore jsonStore)
        {
            m_JsonStore = jsonStore;
            m_Version = jsonStore.version;
        }

        public void Dispatch()
        {
            if (m_Version != m_JsonStore.version)
            {
                m_Version = m_JsonStore.version;
                var callbacksCount = m_Callbacks.Count;
                for (var handle = 0; handle < callbacksCount; handle++)
                {
                    HandleCallback(handle);
                }
            }
        }

        void HandleCallback(int handle)
        {
            var callback = m_Callbacks[handle];
            if (callback.jsonObject == null)
            {
                return;
            }

            var currentObjectVersion = m_JsonStore.GetVersion(callback.jsonObject);
            if (currentObjectVersion == callback.version)
            {
                return;
            }

            callback.version = currentObjectVersion;
            m_Callbacks[handle] = callback;
            callback.action();
        }

        int Register(JsonObject jsonObject, int version, Action action)
        {
            int handle;
            if (m_FreeIndices.Count == 0)
            {
                handle = m_Callbacks.Count;
                m_Callbacks.Add(new ChangeCallback());
            }
            else
            {
                handle = m_FreeIndices.Dequeue();
            }

            m_Callbacks[handle] = new ChangeCallback
            {
                jsonObject = jsonObject,
                version = version,
                action = action
            };

            HandleCallback(handle);

            return handle;
        }

        int Unregister(int handle)
        {
            var version = m_Callbacks[handle].version;
            m_Callbacks[handle] = default;
            m_FreeIndices.Enqueue(handle);
            return version;
        }

        static ChangeDispatcher FindChangeDispatcher(VisualElement visualElement)
        {
            while (visualElement != null)
            {
                if (visualElement is IChangeDispatcherProvider provider)
                {
                    return provider.changeDispatcher;
                }

                visualElement = visualElement.hierarchy.parent;
            }

            return null;
        }

        class ContainerElementChangeState
        {
            VisualElement m_Element;
            JsonObject m_JsonObject;
            int m_Version;
            Action m_Action;
            EventCallback<GeometryChangedEvent> m_GeometryChangedCallback;
            ChangeDispatcher m_Dispatcher;
            int m_Handle;

            public ContainerElementChangeState(VisualElement element, JsonObject jsonObject, Action action)
            {
                m_Element = element;
                m_JsonObject = jsonObject;
                m_Action = action;
                m_GeometryChangedCallback = OnGeometryChanged;
                element.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                element.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            void OnAttachToPanel(AttachToPanelEvent evt)
            {
                m_Element.RegisterCallback(m_GeometryChangedCallback);
            }

            void OnGeometryChanged(GeometryChangedEvent evt)
            {
                m_Element.UnregisterCallback(m_GeometryChangedCallback);
                m_Dispatcher = FindChangeDispatcher(m_Element);
                m_Handle = m_Dispatcher.Register(m_JsonObject, m_Version, m_Action);
            }

            void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                if (m_Dispatcher != null)
                {
                    m_Version = m_Dispatcher.Unregister(m_Handle);
                    m_Dispatcher = null;
                }
            }
        }

        public static void Connect(VisualElement element, JsonObject jsonObject, Action onChange)
        {
            new ContainerElementChangeState(element, jsonObject, onChange);
        }
    }
}
