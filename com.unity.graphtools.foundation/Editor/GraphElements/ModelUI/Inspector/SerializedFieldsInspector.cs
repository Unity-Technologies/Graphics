using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Inspector for the serializable fields of a <see cref="IGraphElementModel"/> or its surrogate, if it implements <see cref="IHasInspectorSurrogate"/>.
    /// </summary>
    public class SerializedFieldsInspector : FieldsInspector
    {
        const BindingFlags k_FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        /// <summary>
        /// Creates a new instance of the <see cref="SerializedFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardHeaderPart"/>.</returns>
        public static SerializedFieldsInspector Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            return new SerializedFieldsInspector(name, model, ownerElement, parentClassName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedFieldsInspector"/> class.
        /// </summary>
        protected SerializedFieldsInspector(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        static bool IsInspectable(FieldInfo f)
        {
            if (f != null)
            {
                var isSerializable = (f.Attributes & FieldAttributes.Public) == FieldAttributes.Public ||
                    (f.Attributes & FieldAttributes.Private) == FieldAttributes.Private &&
                    f.CustomAttributes.Any(a => a.AttributeType == typeof(SerializeField));

                if (!f.IsNotSerialized && isSerializable &&
                    f.CustomAttributes.All(a => a.AttributeType != typeof(HideInInspector)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the <see cref="FieldInfo"/> of a field, if it is displayable in an inspector.
        /// </summary>
        /// <param name="target">The object for which to get the field.</param>
        /// <param name="fieldName">The name of the field to get.</param>
        /// <returns>A <see cref="FieldInfo"/> for the field, or null if the field was not found or if it not inspectable.</returns>
        public static FieldInfo GetInspectableField(object target, string fieldName)
        {
            var f = target?.GetType().GetField(fieldName, k_FieldFlags);
            return IsInspectable(f) ? f : null;
        }

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            var target = m_Model is IHasInspectorSurrogate hasInspectorSurrogate ? hasInspectorSurrogate.Surrogate : m_Model;

            if (target == null)
                yield break;

            var fields = target.GetType().GetFields(k_FieldFlags);
            foreach (var fieldInfo in fields.Where(IsInspectable))
            {
                var modelFieldFieldType = typeof(ModelSerializedFieldField<>).MakeGenericType(fieldInfo.FieldType);
                yield return Activator.CreateInstance(
                    modelFieldFieldType, m_OwnerElement.View, m_Model, fieldInfo.Name)
                    as BaseModelPropertyField;
            }
        }
    }
}
