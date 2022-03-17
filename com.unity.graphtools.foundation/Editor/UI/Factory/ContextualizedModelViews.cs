using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class ContextualizedModelViews
    {
        public readonly IRootView View;
        public readonly IViewContext Context;
        public readonly Dictionary<SerializableGUID, ModelView> ModelViews;

        public ContextualizedModelViews(IRootView view, IViewContext context)
        {
            View = view;
            Context = context;
            ModelViews = new Dictionary<SerializableGUID, ModelView>();
        }
    }
}
