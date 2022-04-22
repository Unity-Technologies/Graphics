using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Constant model for enums.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class EnumConstant : Constant<EnumValueReference>
    {
        [SerializeField]
        TypeHandle m_EnumType;

        /// <inheritdoc />
        public override object DefaultValue => new EnumValueReference(EnumType);

        /// <inheritdoc />
        public override EnumValueReference Value
        {
            get => base.Value;
            set
            {
                if (value.EnumType != EnumType)
                {
                    throw new ArgumentException(nameof(value));
                }

                base.Value = value;
            }
        }

        /// <summary>
        /// The constant value as an <see cref="Enum"/>.
        /// </summary>
        public Enum EnumValue => Value.ValueAsEnum();

        /// <summary>
        /// The <see cref="TypeHandle"/> for the type of the enum.
        /// </summary>
        public TypeHandle EnumType => m_EnumType;

        /// <inheritdoc />
        public override void Initialize(TypeHandle constantTypeHandle)
        {
            var resolvedType = constantTypeHandle.Resolve();
            if (!resolvedType.IsEnum || resolvedType == typeof(Enum))
            {
                throw new ArgumentException(nameof(constantTypeHandle));
            }

            m_EnumType = constantTypeHandle;

            base.Initialize(constantTypeHandle);
        }

        /// <inheritdoc />
        public override IConstant Clone()
        {
            var copy = (Constant<EnumValueReference>)Activator.CreateInstance(GetType());
            ((EnumConstant)copy).m_EnumType = m_EnumType;
            copy.ObjectValue = ObjectValue;
            return copy;
        }

        /// <inheritdoc />
        public override TypeHandle GetTypeHandle()
        {
            return EnumType;
        }

        /// <inheritdoc />
        protected override EnumValueReference FromObject(object value)
        {
            if (value is Enum e)
                return new EnumValueReference(e);
            return base.FromObject(value);
        }
    }
}
