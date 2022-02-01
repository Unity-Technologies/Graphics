using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public sealed class ConstantNodeModel : NodeModel, IConstantNodeModel
    {
        const string k_OutputPortId = "Output_0";

        [SerializeField]
        bool m_IsLocked;

        [SerializeReference]
        IConstant m_Value;

        /// <inheritdoc />
        public override string Title => string.Empty;

        /// <inheritdoc />
        public IPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        /// <inheritdoc />
        public IConstant Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <inheritdoc />
        public object ObjectValue
        {
            get => m_Value.ObjectValue;
            set => m_Value.ObjectValue = value;
        }

        /// <inheritdoc />
        public Type Type => m_Value.Type;

        /// <inheritdoc />
        public bool IsLocked
        {
            get => m_IsLocked;
            set => m_IsLocked = value;
        }

        /// <inheritdoc />
        public void PredefineSetup() =>
            m_Value.ObjectValue = m_Value.DefaultValue;

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        public ConstantNodeModel Clone()
        {
            if (GetType() == typeof(ConstantNodeModel))
            {
                return new ConstantNodeModel { Value = Value.CloneConstant() };
            }
            var clone = Activator.CreateInstance(GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(this, clone);
            return (ConstantNodeModel)clone;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{GetType().Name}: {ObjectValue}";
        }

        /// <inheritdoc />
        public void SetValue<T>(T value)
        {
            if (!(value is Enum) && Type != value.GetType() && !value.GetType().IsSubclassOf(Type))
                throw new ArgumentException($"can't set value of type {value.GetType().Name} in {Type.Name}");
            m_Value.ObjectValue = value;
        }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            this.AddDataOutputPort(null, Value.Type.GenerateTypeHandle(), k_OutputPortId);
        }
    }
}
