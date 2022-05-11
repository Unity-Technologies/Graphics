using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public abstract class SGFieldsInspector : FieldsInspector
    {
        protected SGFieldsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <summary>
        /// Meant to be overriden by any fields inspector implementations to determine what constitutes an empty parts list for display
        /// </summary>
        /// <returns> True if this FieldsInspector has gathered up some UI parts that need to be displayed </returns>
        public abstract bool IsEmpty();
    }
}
