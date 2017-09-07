using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class StandardNodeEditorView : AbstractNodeEditorView
    {
        VisualElement m_EditorTitle;
        VisualElement m_SlotsContainer;
        VisualElement m_DefaultSlotValuesSection;

        new StandardNodeEditorPresenter presenter
        {
            get { return (StandardNodeEditorPresenter) base.presenter; }
        }

        public StandardNodeEditorView()
        {
            AddToClassList("nodeEditor");

            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("header");
            {
                m_EditorTitle = new VisualElement();
                m_EditorTitle.AddToClassList("title");
                headerContainer.Add(m_EditorTitle);

                var headerType = new VisualElement { text = "(node)" };
                headerType.AddToClassList("type");
                headerContainer.Add(headerType);
            }
            Add(headerContainer);

            m_DefaultSlotValuesSection = new VisualElement();
            m_DefaultSlotValuesSection.AddToClassList("section");
            {
                var sectionTitle = new VisualElement { text = "Default Slot Values" };
                sectionTitle.AddToClassList("title");
                m_DefaultSlotValuesSection.Add(sectionTitle);

                m_SlotsContainer = new VisualElement { name = "slots" };
                m_DefaultSlotValuesSection.Add(m_SlotsContainer);
            }
            Add(m_DefaultSlotValuesSection);
        }

        public override void OnDataChanged()
        {
            if (presenter == null)
                return;

            m_EditorTitle.text = presenter.node.name;

            m_SlotsContainer.Clear();
            foreach (var slotEditorPresenter in presenter.slotEditorPresenters)
            {
                m_SlotsContainer.Add(new IMGUISlotEditorView { presenter = slotEditorPresenter });
            }

            if (m_SlotsContainer.Any())
                m_DefaultSlotValuesSection.RemoveFromClassList("hidden");
            else
                m_DefaultSlotValuesSection.AddToClassList("hidden");
        }
    }
}
