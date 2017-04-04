
using UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    public class FilterPopup  : Manipulator
    {
        private readonly VFXFilterWindow.IProvider m_FilterProvider;
        //private readonly Object m_CustomData;

        public FilterPopup(VFXFilterWindow.IProvider filterProvider)
        {
            phaseInterest = EventPhase.Capture;
            m_FilterProvider = filterProvider;
            //m_CustomData = null;
        }

        public FilterPopup(VFXFilterWindow.IProvider filterProvider, Object customData)
        {
            m_FilterProvider = filterProvider;
            //m_CustomData = customData;
        }

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            switch (evt.type)
            {
                case EventType.ContextClick:

                        VFXFilterWindow.Show(Event.current.mousePosition, m_FilterProvider); ;
                    return EventPropagation.Stop;
            }

            return EventPropagation.Continue;
        }
    }
}