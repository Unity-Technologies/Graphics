using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    abstract class SGFieldsInspector : FieldsInspector
    {
        protected SGFieldsInspector(string name, IEnumerable<Model> models, RootView rootView, string parentClassName)
            : base(name, models, rootView, parentClassName) { }

        /// <summary>
        /// Meant to be overriden by any fields inspector implementations to determine what constitutes an empty parts list for display
        /// </summary>
        /// <returns> True if this FieldsInspector has gathered up some UI parts that need to be displayed </returns>
        public abstract bool IsEmpty();
    }
}
