using UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    public class FilterPopup : Manipulator
    {
        private readonly VFXFilterWindow.IProvider m_FilterProvider;
        //private readonly Object m_CustomData;

        public FilterPopup(VFXFilterWindow.IProvider filterProvider)
        {
            m_FilterProvider = filterProvider;


            //m_CustomData = null;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(ShowContextualMenu);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(ShowContextualMenu);
        }

        public FilterPopup(VFXFilterWindow.IProvider filterProvider, Object customData)
        {
            m_FilterProvider = filterProvider;
            //m_CustomData = customData;
        }

        protected void ShowContextualMenu(MouseUpEvent e)
        {
            if (e.button == 1)
            {
                VFXFilterWindow.Show(Event.current.mousePosition, m_FilterProvider);
                e.StopPropagation();
            }
        }
    }
}
