using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class ModelViewMapping
    {
        List<ContextualizedModelViews> m_ContextualizedModelViews;

        public ModelViewMapping()
        {
            m_ContextualizedModelViews = new List<ContextualizedModelViews>();
        }

        public void AddOrReplaceViewForModel(ModelView modelView)
        {
            if (modelView.Model == null)
                return;

            var view = modelView.RootView;
            var context = modelView.Context;

            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge
                => cge.View == view && cge.Context == context);

            if (contextualizedView == null)
            {
                contextualizedView = new ContextualizedModelViews(view, context);
                m_ContextualizedModelViews.Add(contextualizedView);
            }

            contextualizedView.ModelViews[modelView.Model.Guid] = modelView;
        }

        public void RemoveModelView(ModelView modelView)
        {
            if (modelView.Model == null)
                return;

            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge => cge.View == modelView.RootView && cge.Context == modelView.Context);

            contextualizedView?.ModelViews.Remove(modelView.Model.Guid);
        }

        public ModelView FirstViewOrDefault(IRootView view, IViewContext context, IModel model)
        {
            if (model == null)
                return null;

            ContextualizedModelViews gel = null;
            for (int i = 0; i < m_ContextualizedModelViews.Count; i++)
            {
                var e = m_ContextualizedModelViews[i];
                if (e.View == view && e.Context == context)
                {
                    gel = e;
                    break;
                }
            }

            if (gel == null)
                return null;

            gel.ModelViews.TryGetValue(model.Guid, out var modelView);
            return modelView;
        }

        public void AppendAllViews(IModel model, IRootView view, Predicate<ModelView> filter, List<ModelView> outViewList)
        {
            if (model == null)
                return;

            foreach (var contextualizedView in m_ContextualizedModelViews)
            {
                if (contextualizedView.ModelViews.TryGetValue(model.Guid, out var modelView) &&
                    modelView.RootView == view &&
                    (filter == null || filter(modelView)))
                {
                    outViewList.Add(modelView);
                }
            }
        }
    }
}
