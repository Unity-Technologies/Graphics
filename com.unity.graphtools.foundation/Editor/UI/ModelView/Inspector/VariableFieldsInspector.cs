using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{ /// <summary>
    /// Inspector for <see cref="INodeModel"/>.
    /// </summary>
    public class VariableFieldsInspector : SerializedFieldsInspector
    {
        /// <summary>
        /// Creates a new instance of the <see cref="VariableFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        /// <returns>A new instance of <see cref="VariableFieldsInspector"/>.</returns>
        public static VariableFieldsInspector Create(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            if (model is IVariableDeclarationModel)
            {
                return new VariableFieldsInspector(name, model, ownerElement, parentClassName,filter);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableFieldsInspector"/> class.
        /// </summary>
        protected VariableFieldsInspector(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName, Func<FieldInfo,bool> filter)
            : base(name, model, ownerElement, parentClassName,filter) {}

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            foreach (var field in base.GetFields())
            {
                yield return field;
            }

            BaseModelPropertyField valueEditor = null;
            if (m_Model is IVariableDeclarationModel variableDeclarationModel)
            {
                if (variableDeclarationModel.InitializationModel != null)
                {
                    valueEditor = InlineValueEditor.CreateEditorForConstant(m_OwnerElement.RootView, variableDeclarationModel,
                        variableDeclarationModel.InitializationModel, false,"Value");
                }
            }

            yield return valueEditor;
        }
    }
}
