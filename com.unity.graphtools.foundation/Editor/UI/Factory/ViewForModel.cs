using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Utility to get the <see cref="IModelView"/> that have been created for a <see cref="IGraphElementModel"/>.
    /// </summary>
    public static class ViewForModel
    {
        static ModelViewMapping s_ViewForModel = new ModelViewMapping();

        internal static void AddOrReplaceModelView(ModelView modelView)
        {
            s_ViewForModel.AddOrReplaceViewForModel(modelView);
        }

        [CanBeNull]
        public static T GetView<T>(this IModel model, IRootView view, IViewContext context = null) where T : class, IModelView
        {
            return s_ViewForModel.FirstViewOrDefault(view, context, model) as T;
        }

        [CanBeNull]
        internal static ModelView GetView(this IModel model, IRootView view, IViewContext context = null)
        {
            return s_ViewForModel.FirstViewOrDefault(view, context, model);
        }

        /// <summary>
        /// Appends all <see cref="GraphElement"/>s that represents <paramref name="model"/> to the list <paramref name="outUIList"/>.
        /// </summary>
        /// <param name="model">The model for which to get the <see cref="GraphElement"/>s.</param>
        /// <param name="view">The view in which the UI elements live.</param>
        /// <param name="filter">A predicate to filter the appended elements.</param>
        /// <param name="outUIList">The list onto which the elements are appended.</param>
        public static void GetAllViews(this IModel model, IRootView view, Predicate<ModelView> filter, List<ModelView> outUIList)
        {
            s_ViewForModel.AppendAllViews(model, view, filter, outUIList);
        }

        internal static IEnumerable<ModelView> GetAllViewsInList(this IEnumerable<IModel> models, IRootView view,
            Predicate<ModelView> filter, List<ModelView> outViewList)
        {
            outViewList.Clear();
            var modelList = models.ToList();
            foreach (var model in modelList)
            {
                model.GetAllViews(view, filter, outViewList);
            }

            return outViewList;
        }

        internal static IEnumerable<ModelView> GetAllViewsRecursivelyInList(this IEnumerable<IModel> models, IRootView view,
            Predicate<ModelView> filter, List<ModelView> outViewList)
        {
            outViewList.Clear();
            return RecurseGetAllViewsInList(models, view, filter, outViewList);
        }

        static IEnumerable<ModelView> RecurseGetAllViewsInList(this IEnumerable<IModel> models, IRootView view,
            Predicate<ModelView> filter, List<ModelView> outViewList)
        {
            var modelList = models.ToList();
            foreach (var model in modelList)
            {
                if (model is IGraphElementContainer container)
                {
                    RecurseGetAllViewsInList(container.GraphElementModels, view, filter, outViewList);
                }
                model.GetAllViews(view, filter, outViewList);
            }

            return outViewList;
        }

        internal static void RemoveModelView(ModelView modelView)
        {
            s_ViewForModel.RemoveModelView(modelView);
        }

        internal static void Reset()
        {
            s_ViewForModel = new ModelViewMapping();
        }
    }
}
