using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Variable flags.
    /// </summary>
    [Flags]
    public enum VariableFlags
    {
        /// <summary>
        /// Empty flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// The variable was automatically generated.
        /// </summary>
        Generated = 1,

        /// <summary>
        /// The variable is hidden.
        /// </summary>
        Hidden = 2,
    }

    /// <summary>
    /// A model that represents a variable declaration in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class VariableDeclarationModel : DeclarationModel, IVariableDeclarationModel
    {
        [SerializeField]
        TypeHandle m_DataType;
        [SerializeField]
        bool m_IsExposed;
        [SerializeField]
        string m_Tooltip;

        [SerializeReference]
        IConstant m_InitializationValue;

        [SerializeField]
        int m_Modifiers;

        /// <summary>
        /// The variable flags.
        /// </summary>
        public VariableFlags variableFlags;

        /// <inheritdoc />
        public ModifierFlags Modifiers
        {
            get => (ModifierFlags)m_Modifiers;
            set => m_Modifiers = (int)value;
        }

        /// <inheritdoc />
        public override void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = GraphModel.GenerateGraphVariableDeclarationUniqueName(newName, Guid);
        }

        /// <inheritdoc />
        public virtual string GetVariableName() => Title.CodifyStringInternal();

        /// <summary>
        /// How we should call this variable.
        /// </summary>
        public string VariableString => IsExposed ? "Exposed variable" : "Variable";

        /// <inheritdoc />
        public virtual TypeHandle DataType
        {
            get => m_DataType;
            set
            {
                if (m_DataType == value)
                    return;
                m_DataType = value;
                m_InitializationValue = null;
                if (GraphModel.Stencil.RequiresInspectorInitialization(this))
                    CreateInitializationValue();
            }
        }

        /// <inheritdoc />
        public bool IsExposed
        {
            get => m_IsExposed;
            set => m_IsExposed = value;
        }

        /// <inheritdoc />
        public string Tooltip
        {
            get => m_Tooltip;
            set => m_Tooltip = value;
        }

        /// <inheritdoc />
        public IConstant InitializationModel
        {
            get => m_InitializationValue;
            set => m_InitializationValue = value;
        }

        /// <inheritdoc />
        public IGroupModel Group { get; set; }

        /// <inheritdoc />
        public virtual void CreateInitializationValue()
        {
            if (GraphModel.Stencil.GetConstantNodeValueType(DataType) != null)
            {
                InitializationModel = GraphModel.Stencil.CreateConstantValue(DataType);

                EditorUtility.SetDirty((Object)AssetModel);
            }
        }

        bool Equals(VariableDeclarationModel other)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(other) && m_DataType.Equals(other.m_DataType) && m_IsExposed == other.m_IsExposed;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((VariableDeclarationModel)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
                int hashCode = base.GetHashCode();
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ m_DataType.GetHashCode();
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ m_IsExposed.GetHashCode();
                return hashCode;
            }
        }
    }
}
