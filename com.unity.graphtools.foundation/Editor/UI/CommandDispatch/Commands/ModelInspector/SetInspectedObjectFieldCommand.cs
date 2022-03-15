using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to set the value of a field on an model.
    /// </summary>
    public abstract class SetInspectedObjectFieldCommand: UndoableCommand
    {
        const string k_UndoStringSingular = "Set Property";

        public object Value;
        public object InspectedObject;
        public FieldInfo Field;

        PropertyInfo m_PropertyInfo;
        MethodInfo m_MethodInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedObjectFieldCommand"/> class.
        /// </summary>
        public SetInspectedObjectFieldCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedObjectFieldCommand"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="inspectedObject">The object that owns the field.</param>
        /// <param name="field">The field to set.</param>
        public SetInspectedObjectFieldCommand(object value, object inspectedObject, FieldInfo field)
        {
            UndoString = k_UndoStringSingular;

            Value = value;
            InspectedObject = inspectedObject;
            Field = field;

            if (InspectedObject != null && Field != null)
            {
                var useMethodAttribute = Field.GetCustomAttribute<InspectorUseSetterMethodAttribute>();
                if (useMethodAttribute != null)
                {
                    m_MethodInfo = InspectedObject.GetType().GetMethod(useMethodAttribute.MethodName);

                    if (m_MethodInfo != null)
                    {
                        var parameters = m_MethodInfo.GetParameters();
                        Debug.Assert(parameters[0].ParameterType.IsInstanceOfType(Value));
                        Debug.Assert(parameters[1].IsOut &&
                            parameters[1].ParameterType.GetElementType() == typeof(IEnumerable<IGraphElementModel>));
                        Debug.Assert(parameters[2].IsOut &&
                            parameters[2].ParameterType.GetElementType() == typeof(IEnumerable<IGraphElementModel>));
                        Debug.Assert(parameters[3].IsOut &&
                            parameters[3].ParameterType.GetElementType() == typeof(IEnumerable<IGraphElementModel>));
                        Debug.Assert(parameters.Length == 4);
                    }
                }

                var usePropertyAttribute = Field.GetCustomAttribute<InspectorUsePropertyAttribute>();
                if (usePropertyAttribute != null)
                {
                    m_PropertyInfo = InspectedObject.GetType().GetProperty(usePropertyAttribute.PropertyName);
                }
            }
        }

        /// <summary>
        /// Sets the field on an object according to the data held in <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command that holds the object, field and new field value.</param>
        /// <param name="newModels">On exit, contains the models that were added as the result of setting the field.</param>
        /// <param name="changedModels">On exit, contains the models that were modified as the result of setting the field (excluding the object on which the field is set).</param>
        /// <param name="deletedModels">On exit, contains the models that were deleted as the result of setting the field.</param>
        protected static void SetField(SetInspectedObjectFieldCommand command,
            out IEnumerable<IGraphElementModel> newModels,
            out IEnumerable<IGraphElementModel> changedModels,
            out IEnumerable<IGraphElementModel> deletedModels)
        {
            newModels = null;
            changedModels = null;
            deletedModels = null;

            if (command.InspectedObject != null && command.Field != null)
            {
                if (command.m_MethodInfo != null)
                {
                    var parameters = new[] { command.Value, null, null, null };
                    command.m_MethodInfo.Invoke(command.InspectedObject, parameters);
                    newModels = (IEnumerable<IGraphElementModel>)parameters[1];
                    changedModels = (IEnumerable<IGraphElementModel>)parameters[2];
                    deletedModels = (IEnumerable<IGraphElementModel>)parameters[3];
                }
                else if (command.m_PropertyInfo != null)
                {
                    command.m_PropertyInfo.SetMethod.Invoke(command.InspectedObject, new[] { command.Value });

                    if (command.InspectedObject is IGraphElementModel graphElementModel)
                    {
                        changedModels = new[]{ graphElementModel };
                    }
                }
                else
                {
                    command.Field.SetValue(command.InspectedObject, command.Value);

                    if (command.InspectedObject is IGraphElementModel graphElementModel)
                    {
                        changedModels = new[]{ graphElementModel };
                    }
                }
            }
        }
    }
}
