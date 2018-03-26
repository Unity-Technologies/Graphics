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
    class VFXGroupNode : Group, IControlledElement<VFXGroupNodeController>, IVFXMovable
    {
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXGroupNodeController controller
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

        VFXGroupNodeController m_Controller;

        public VFXGroupNode()
        {
            RegisterCallback<ControllerChangedEvent>(OnControllerChanged);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        public void OnMoved()
        {
            if (containedElements.Count() == 0)
            {
                controller.position = GetPosition();
            }

            foreach (var node in containedElements.OfType<IVFXMovable>())
            {
                node.OnMoved();
            }
        }

        public void OnControllerChanged(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                // use are custom data changed from the view because we can't listen simply to the VFXUI, because the VFXUI might have been modified because we were removed and the datawatch might call us before the view
                VFXView view = this.GetFirstAncestorOfType<VFXView>();
                if (view == null) return;


                m_ModificationFromPresenter = true;
                title = controller.title;


                var presenterContent = controller.nodes.ToArray();
                var elementContent = containedElements.OfType<ISettableControlledElement<VFXNodeController>>();

                if (elementContent == null)
                {
                    elementContent = new List<ISettableControlledElement<VFXNodeController>>();
                }

                bool elementsChanged = false;
                var elementToDelete = elementContent.Where(t => !presenterContent.Contains(t.controller)).ToArray();
                foreach (var element in elementToDelete)
                {
                    this.RemoveElement(element as GraphElement);
                    elementsChanged = true;
                }

                var viewElements = view.Query().Children<VisualElement>().Children<GraphElement>().ToList().OfType<ISettableControlledElement<VFXNodeController>>();

                var elementToAdd = presenterContent.Where(t => elementContent.FirstOrDefault(u => u.controller == t) == null).Select(t => viewElements.FirstOrDefault(u => u.controller == t)).ToArray();

                foreach (var element in elementToAdd)
                {
                    if (element != null)
                    {
                        this.AddElement(element as GraphElement);
                        elementsChanged = true;
                    }
                }

                // only update position if the groupnode is empty otherwise the size should be computed from the content.
                if (presenterContent.Length == 0)
                {
                    SetPosition(controller.position);
                }
                else
                {
                    UpdateGeometryFromContent();
                }

                m_ModificationFromPresenter = false;
            }
        }

        bool m_ModificationFromPresenter;

        public void ElementAddedToGroupNode(GraphElement element)
        {
            if (!m_ModificationFromPresenter)
            {
                ISettableControlledElement<VFXNodeController> node = element as ISettableControlledElement<VFXNodeController>;

                if (node != null)
                {
                    controller.AddNode(node.controller);

                    OnMoved();
                }
            }
        }

        public void ElementRemovedFromGroupNode(GraphElement element)
        {
            if (!m_ModificationFromPresenter)
            {
                ISettableControlledElement<VFXNodeController> node = element as ISettableControlledElement<VFXNodeController>;
                if (node != null)
                {
                    controller.RemoveNode(node.controller);

                    OnMoved();
                }
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
