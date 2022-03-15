using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Base class for graph element models.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class GraphElementModel : IGraphElementModel, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        [SerializeField, HideInInspector, FormerlySerializedAs("m_GraphAssetModel")]
        protected GraphAssetModel m_AssetModel;

        [SerializeField, HideInInspector]
        Color m_Color;

        [SerializeField, HideInInspector]
        bool m_HasUserColor;

        [SerializeField, HideInInspector]
        SerializationVersion m_Version;

        protected List<Capabilities> m_Capabilities = new List<Capabilities>();

        /// <summary>
        /// Serialized version, used for backward compatibility
        /// </summary>
        public SerializationVersion Version => m_Version;

        /// <inheritdoc />
        public virtual IGraphModel GraphModel => AssetModel?.GraphModel;

        /// <inheritdoc />
        public virtual SerializableGUID Guid
        {
            get => m_Guid;
            set => m_Guid = value;
        }

        /// <inheritdoc />
        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        /// <inheritdoc />
        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        /// <summary>
        /// Used for backward compatibility
        /// </summary>
        protected internal Color InternalSerializedColor => m_Color;

        /// <summary>
        /// Default Color to use when no user color is provided
        /// </summary>
        public virtual Color DefaultColor => Color.clear;

        /// <inheritdoc />
        public Color Color
        {
            get => HasUserColor ? m_Color : DefaultColor;
            set
            {
                if (this.IsColorable())
                {
                    m_HasUserColor = true;
                    m_Color = value;
                }
            }
        }

        /// <inheritdoc />
        public bool HasUserColor => m_HasUserColor;

        /// <summary>
        /// The container this node is stored into.
        /// </summary>
        public virtual IGraphElementContainer Container
        {
            get => GraphModel;
        }

        /// <summary>
        /// Version number for serialization.
        /// </summary>
        /// <remarks>
        /// Useful for models backward compatibility
        /// </remarks>
        public enum SerializationVersion
        {
            // Use package release number as the name of the version.

            // ReSharper disable once InconsistentNaming
            GTF_V_0_8_2 = 0,

            GTF_V_0_13_0 = 1,

            /// <summary>
            /// Keep Latest as the highest value in this enum
            /// </summary>
            Latest
        }

        protected GraphElementModel()
        {
            AssignNewGuid();
        }

        /// <inheritdoc />
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        /// <inheritdoc />
        public void ResetColor()
        {
            m_HasUserColor = false;
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
            m_Version = SerializationVersion.Latest;
        }

        public virtual void OnAfterDeserialize()
        {
        }
    }
}
