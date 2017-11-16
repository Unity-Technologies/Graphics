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
    public class VFXGroupNode : GroupNode
    {
        public VFXGroupNode()
        {
        }

        public void OnViewDataChanged()
        {
            // use are custom data changed from the view because we can't listen simply to the VFXUI, because the VFXUI might have been modified because we were removed and the datawatch might call us before the view
            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            if (view == null) return;
            VFXGroupNodePresenter presenter = GetPresenter<VFXGroupNodePresenter>();


            title = presenter.title;
            var presenterContent = presenter.nodes.ToArray();
            var elementContent = containedElements;

            if (elementContent == null)
            {
                elementContent = new GraphElement[0];
            }
            m_ModificationFromPresenter = true;

            var elementToDelete = elementContent.Where(t => !presenterContent.Contains(t.presenter as VFXNodePresenter)).ToArray();
            foreach (var element in elementToDelete)
            {
                this.RemoveElement(element);
            }

            var viewElements = view.Query().Children<VisualElement>().Children<GraphElement>().ToList();

            var elementToAdd = presenterContent.Where(t => elementContent.FirstOrDefault(u => u.presenter == t) == null).Select(t => viewElements.FirstOrDefault(u => u.presenter == t)).ToArray();

            foreach (var element in elementToAdd)
            {
                if (element != null)
                    this.AddElement(element);
            }

            m_ModificationFromPresenter = false;
        }

        bool m_ModificationFromPresenter;

        public void ElementAddedToGroupNode(GraphElement element)
        {
            if (!m_ModificationFromPresenter)
            {
                VFXGroupNodePresenter presenter = GetPresenter<VFXGroupNodePresenter>();

                presenter.AddNode(element.presenter as VFXNodePresenter);
            }
        }

        public void ElementRemovedFromGroupNode(GraphElement element)
        {
            if (!m_ModificationFromPresenter)
            {
                VFXGroupNodePresenter presenter = GetPresenter<VFXGroupNodePresenter>();

                presenter.RemoveNode(element.presenter as VFXNodePresenter);
            }
        }

        public void GroupNodeTitleChanged(string title)
        {
            if (!m_ModificationFromPresenter)
            {
                VFXGroupNodePresenter presenter = GetPresenter<VFXGroupNodePresenter>();

                presenter.title = title;
            }
        }
    }
}
