using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Utility to get the <see cref="IModelUI"/> that have been created for a <see cref="IGraphElementModel"/>.
    /// </summary>
    public static class UIForModel
    {
        static GraphElementMapping s_UIForModel = new GraphElementMapping();

        internal static void AddOrReplaceGraphElement(ModelUI modelUI)
        {
            s_UIForModel.AddOrReplaceUIForModel(modelUI);
        }

        [CanBeNull]
        public static T GetUI<T>(this IGraphElementModel model, IModelView view, IUIContext context = null) where T : class, IModelUI
        {
            return s_UIForModel.FirstUIOrDefault(view, context, model) as T;
        }

        [CanBeNull]
        internal static ModelUI GetUI(this IGraphElementModel model, IModelView view, IUIContext context = null)
        {
            return s_UIForModel.FirstUIOrDefault(view, context, model);
        }

        /// <summary>
        /// Appends all <see cref="GraphElement"/>s that represents <paramref name="model"/> to the list <paramref name="outUIList"/>.
        /// </summary>
        /// <param name="model">The model for which to get the <see cref="GraphElement"/>s.</param>
        /// <param name="view">The view in which the UI elements live.</param>
        /// <param name="filter">A predicate to filter the appended elements.</param>
        /// <param name="outUIList">The list onto which the elements are appended.</param>
        public static void GetAllUIs(this IGraphElementModel model, IModelView view, Predicate<ModelUI> filter, List<ModelUI> outUIList)
        {
            s_UIForModel.AppendAllUIs(model, view, filter, outUIList);
        }

        internal static IEnumerable<ModelUI> GetAllUIsInList(this IEnumerable<IGraphElementModel> models, IModelView view,
            Predicate<ModelUI> filter, List<ModelUI> outUIList)
        {
            outUIList.Clear();
            var modelList = models.ToList();
            foreach (var model in modelList)
            {
                model.GetAllUIs(view, filter, outUIList);
            }

            return outUIList;
        }

        internal static void RemoveGraphElement(ModelUI modelUI)
        {
            s_UIForModel.RemoveGraphElement(modelUI);
        }

        internal static void Reset()
        {
            s_UIForModel = new GraphElementMapping();
        }
    }
}
