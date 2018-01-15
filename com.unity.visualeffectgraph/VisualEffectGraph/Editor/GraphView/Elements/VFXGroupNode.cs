using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXGroupNode : GroupNode, IControlledElement<VFXGroupNodePresenter>
    {
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXGroupNodePresenter controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        VFXGroupNodePresenter m_Controller;

        public VFXGroupNode()
        {
        }

        public void OnControllerChanged(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                // use are custom data changed from the view because we can't listen simply to the VFXUI, because the VFXUI might have been modified because we were removed and the datawatch might call us before the view
                VFXView view = this.GetFirstAncestorOfType<VFXView>();
                if (view == null) return;


                title = controller.title;
                var presenterContent = controller.nodes.ToArray();
                var elementContent = containedElements.Cast<IControlledElement<VFXNodeController>>();

                if (elementContent == null)
                {
                    elementContent = new List<IControlledElement<VFXNodeController>>();
                }
                m_ModificationFromPresenter = true;

                var elementToDelete = elementContent.Where(t => !presenterContent.Contains(t.controller)).ToArray();
                foreach (var element in elementToDelete)
                {
                    this.RemoveElement(element as GraphElement);
                }

                var viewElements = view.Query().Children<VisualElement>().Children<GraphElement>().ToList().OfType<IControlledElement<VFXNodeController>>();

                var elementToAdd = presenterContent.Where(t => elementContent.FirstOrDefault(u => u.controller == t) == null).Select(t => viewElements.FirstOrDefault(u => u.controller == t)).ToArray();

                foreach (var element in elementToAdd)
                {
                    if (element != null)
                        this.AddElement(element as GraphElement);
                }

                m_ModificationFromPresenter = false;
            }
        }

        bool m_ModificationFromPresenter;

        public void ElementAddedToGroupNode(GraphElement element)
        {
            if (!m_ModificationFromPresenter)
            {
                controller.AddNode((element as IControlledElement<VFXNodeController>).controller);

                UpdatePresenterPosition();
            }
        }

        public void ElementRemovedFromGroupNode(GraphElement element)
        {
            if (!m_ModificationFromPresenter)
            {
                controller.RemoveNode((element as IControlledElement<VFXNodeController>).controller);

                UpdatePresenterPosition();
            }
        }

        public void GroupNodeTitleChanged(string title)
        {
            if (!m_ModificationFromPresenter)
            {
                controller.title = title;
            }
        }
    }
}
