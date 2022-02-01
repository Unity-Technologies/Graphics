using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class ContextualizedGraphElements
    {
        public readonly IModelView View;
        public readonly IUIContext Context;
        public readonly Dictionary<SerializableGUID, ModelUI> GraphElements;

        public ContextualizedGraphElements(IModelView view, IUIContext context)
        {
            View = view;
            Context = context;
            GraphElements = new Dictionary<SerializableGUID, ModelUI>();
        }
    }
}
