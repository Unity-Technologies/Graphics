using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Factory to create UI from models.
    /// </summary>
    public static class GraphElementFactory
    {
        /// <summary>
        /// Creates an instance of a class implementing <see cref="IModelUI"/> to display <paramref name="model"/>.
        /// </summary>
        /// <param name="view">The view in which to put the UI.</param>
        /// <param name="model">The model.</param>
        /// <typeparam name="T">The type of the returned object.</typeparam>
        /// <returns>An instance of <see cref="IModelUI"/> that display <paramref name="model"/>.</returns>
        [CanBeNull]
        public static T CreateUI<T>(IModelView view, IGraphElementModel model) where T : class, IModelUI
        {
            return CreateUI<T>(view, model, null);
        }

        /// <summary>
        /// Creates an instance of a class implementing <see cref="IModelUI"/> to display <paramref name="model"/>.
        /// </summary>
        /// <param name="view">The view in which to put the UI.</param>
        /// <param name="model">The model.</param>
        /// <param name="context">The context of ui creation. When a model needs different UI in
        /// different contexts, use this parameter to differentiate between contexts.</param>
        /// <typeparam name="T">The type of the returned object.</typeparam>
        /// <returns>An instance of <see cref="IModelUI"/> that display <paramref name="model"/>.</returns>
        public static T CreateUI<T>(IModelView view, IGraphElementModel model, IUIContext context) where T : class, IModelUI
        {
            if (view == null)
            {
                Debug.LogError("GraphElementFactory could not create element because view is null.");
                return null;
            }

            if (model == null)
            {
                Debug.LogError("GraphElementFactory could not create element because model is null.");
                return null;
            }

            var ext = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(
                view.GetType(),
                model.GetType(),
                FilterMethods,
                KeySelector
            );

            T newElem = null;
            if (ext != null)
            {
                var nodeBuilder = new ElementBuilder { View = view, Context = context };
                newElem = ext.Invoke(null, new object[] { nodeBuilder, model }) as T;
            }

            if (newElem == null)
            {
                Debug.LogError($"GraphElementFactory doesn't know how to create a UI of type {typeof(T)} for model of type: {model.GetType()}");
                return null;
            }

            return newElem;
        }

        internal static Type KeySelector(MethodInfo x)
        {
            return x.GetParameters()[1].ParameterType;
        }

        internal static bool FilterMethods(MethodInfo x)
        {
            if (x.ReturnType != typeof(IModelUI))
                return false;

            var parameters = x.GetParameters();
            return parameters.Length == 2 && parameters[0].ParameterType == typeof(ElementBuilder);
        }
    }
}
