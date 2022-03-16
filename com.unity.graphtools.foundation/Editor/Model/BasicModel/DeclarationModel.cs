using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a declaration (e.g. a variable) in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class DeclarationModel : GraphElementModel, IDeclarationModel, IRenamable
    {
        [FormerlySerializedAs("name")]
        [SerializeField, HideInInspector]
        string m_Name;

        /// <inheritdoc />
        public string Title
        {
            get => m_Name;
            set => m_Name = value;
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title.Nicify();

        /// <summary>
        /// Initializes a new instance of the DeclarationModel class.
        /// </summary>
        public DeclarationModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Droppable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Renamable
            });
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }
    }
}
