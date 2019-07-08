using UnityEngine.UIElements;
using System;

namespace UnityEditor.Experimental.Rendering.Universal
{
    class HeaderModifier : IMGUIContainer
    {
        Action m_OldHeaderGUIHandler;
        Action m_ModifierGUIHandler;

        public HeaderModifier(Action onGUIHandler, Action modifierGUIHandler) : base(onGUIHandler)
        {
            m_ModifierGUIHandler = modifierGUIHandler;
        }

        public override void HandleEvent(EventBase evt)
        {
            if (evt is AttachToPanelEvent)
            {
                var root = parent.parent;
                if (root.childCount > 0)
                {
                    foreach (var child in root.Children())
                    {
                        var header = child as IMGUIContainer;
                        if (header != null && header.name.Contains("Header"))
                        {
                            if (m_OldHeaderGUIHandler == null)
                            {
                                m_OldHeaderGUIHandler = header.onGUIHandler;
                                header.onGUIHandler = () =>
                                {
                                    m_OldHeaderGUIHandler();
                                    m_ModifierGUIHandler();
                                };
                            }
                        }

                    }
                }
            }

            base.HandleEvent(evt);
        }
    }
}
