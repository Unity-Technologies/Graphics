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

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            
        }

        public void OnMoved()
        {
            controller.position = GetPosition();

            foreach (var node in containedElements.OfType<IVFXMovable>())
            {
                node.OnMoved();
            }
        }

        void OnControllerChanged(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }
        public void SelfChange()
        {
            // use are custom data changed from the view because we can't listen simply to the VFXUI, because the VFXUI might have been modified because we were removed and the datawatch might call us before the view
            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            if (view == null) return;


            m_ModificationFromController = true;
            inRemoveElement = true;
            title = controller.title;


            var presenterContent = controller.nodes.ToArray();
            var elementContent = containedElements.OfType<IControlledElement>().Where(t => t.controller is VFXNodeController || t.controller is VFXStickyNoteController).ToArray();

            bool elementsChanged = false;
            var elementToDelete = elementContent.Where(t => !presenterContent.Contains(t.controller)).ToArray();
            foreach (var element in elementToDelete)
            {
                this.RemoveElement(element as GraphElement);
                elementsChanged = true;
            }

            var viewElements = view.Query().Children<VisualElement>().Children<GraphElement>().ToList().OfType<IControlledElement>();

            var elementToAdd = presenterContent.Select(t=>view.GetGroupNodeElement(t)).Except(elementContent.Cast<GraphElement>()).ToArray();

            bool someNodeNotFound = false;
            foreach (var element in elementToAdd)
            {
                if (element != null)
                {
                    this.AddElement(element as GraphElement);
                    elementsChanged = true;
                }
                else
                {
                    someNodeNotFound = true;
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

            m_ModificationFromController = false;
                inRemoveElement = false;
        }

        bool m_ModificationFromController;

        public static bool inRemoveElement {get; set; }

        public void ElementAddedToGroupNode(GraphElement element)
        {
            if (!m_ModificationFromController)
            {
                ISettableControlledElement<VFXNodeController> node = element as ISettableControlledElement<VFXNodeController>;

                if (node != null)
                {
                    controller.AddNode(node.controller);

                    OnMoved();
                }
                else if (element is VFXStickyNote)
                {
                    controller.AddStickyNote((element as VFXStickyNote).controller);
                }
            }
        }

        public void ElementRemovedFromGroupNode(GraphElement element)
        {
            if (!m_ModificationFromController && !inRemoveElement)
            {
                ISettableControlledElement<VFXNodeController> node = element as ISettableControlledElement<VFXNodeController>;
                if (node != null)
                {
                    controller.RemoveNode(node.controller);

                    OnMoved();
                }
                else if (element is VFXStickyNote)
                {
                    controller.RemoveStickyNote((element as VFXStickyNote).controller);
                }
            }
        }

        public void GroupNodeTitleChanged(string title)
        {
            if (!m_ModificationFromController)
            {
                controller.title = title;
            }
        }
    }
}
