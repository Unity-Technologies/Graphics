using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class GraphElementMapping
    {
        List<ContextualizedGraphElements> m_ContextualizedGraphElements;

        public GraphElementMapping()
        {
            m_ContextualizedGraphElements = new List<ContextualizedGraphElements>();
        }

        public void AddOrReplaceUIForModel(ModelUI modelUI)
        {
            if (modelUI.Model == null)
                return;

            var view = modelUI.View;
            var context = modelUI.Context;

            var contextualizedGraphElement = m_ContextualizedGraphElements.FirstOrDefault(cge
                => cge.View == view && cge.Context == context);

            if (contextualizedGraphElement == null)
            {
                contextualizedGraphElement = new ContextualizedGraphElements(view, context);
                m_ContextualizedGraphElements.Add(contextualizedGraphElement);
            }

            contextualizedGraphElement.GraphElements[modelUI.Model.Guid] = modelUI;
        }

        public void RemoveGraphElement(ModelUI modelUI)
        {
            if (modelUI.Model == null)
                return;

            var contextualizedGraphElements = m_ContextualizedGraphElements.FirstOrDefault(cge => cge.View == modelUI.View && cge.Context == modelUI.Context);

            contextualizedGraphElements?.GraphElements.Remove(modelUI.Model.Guid);
        }

        public ModelUI FirstUIOrDefault(IModelView view, IUIContext context, IGraphElementModel model)
        {
            if (model == null)
                return null;

            ContextualizedGraphElements gel = null;
            for (int i = 0; i < m_ContextualizedGraphElements.Count; i++)
            {
                var e = m_ContextualizedGraphElements[i];
                if (e.View == view && e.Context == context)
                {
                    gel = e;
                    break;
                }
            }

            if (gel == null)
                return null;

            ModelUI modelUI = null;
            gel.GraphElements.TryGetValue(model.Guid, out modelUI);
            return modelUI;
        }

        public void AppendAllUIs(IGraphElementModel model, IModelView view, Predicate<ModelUI> filter, List<ModelUI> outUIList)
        {
            if (model == null)
                return;

            foreach (var contextualizedGraphElement in m_ContextualizedGraphElements)
            {
                if (contextualizedGraphElement.GraphElements.TryGetValue(model.Guid, out var modelUI) &&
                    modelUI.View == view &&
                    (filter == null || filter(modelUI)))
                {
                    outUIList.Add(modelUI);
                }
            }
        }
    }
}
