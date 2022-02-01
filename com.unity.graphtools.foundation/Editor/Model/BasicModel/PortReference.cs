using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Reference to a port by its unique id.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    struct PortReference
    {
        [SerializeField, FormerlySerializedAs("NodeModelGuid")]
        SerializableGUID m_NodeModelGuid;

        [SerializeField, FormerlySerializedAs("GraphAssetModel")]
        GraphAssetModel m_GraphAssetModel;

        [SerializeField, FormerlySerializedAs("UniqueId")]
        string m_UniqueId;

        INodeModel m_NodeModel;

        /// <summary>
        /// The GUID of the node model that owns the port referenced by this instance.
        /// </summary>
        public SerializableGUID NodeModelGuid => m_NodeModelGuid;

        /// <summary>
        /// The unique id of the port referenced by this instance.
        /// </summary>
        public string UniqueId
        {
            get => m_UniqueId;
            // for tests
            internal set => m_UniqueId = value;
        }

        /// <summary>
        /// The node model that owns the port referenced by this instance.
        /// </summary>
        public INodeModel NodeModel
        {
            get
            {
                if (m_NodeModel == null)
                {
                    if (m_GraphAssetModel != null && m_GraphAssetModel.GraphModel.TryGetModelFromGuid(m_NodeModelGuid, out var node))
                    {
                        m_NodeModel = node as INodeModel;
                    }
                }

                return m_NodeModel;
            }

            private set
            {
                m_GraphAssetModel = (GraphAssetModel)value.AssetModel;
                m_NodeModelGuid = value.Guid;
                m_NodeModel = null;
            }
        }

        /// <summary>
        /// Sets the port that this instance references.
        /// </summary>
        /// <param name="portModel"></param>
        public void Assign(IPortModel portModel)
        {
            Assert.IsNotNull(portModel);
            NodeModel = portModel.NodeModel;
            m_UniqueId = portModel.UniqueName;
        }

        public IPortModel GetPortModel(PortDirection direction, ref IPortModel previousValue)
        {
            var nodeModel = NodeModel;
            if (nodeModel == null)
            {
                return previousValue = null;
            }

            // when removing a set property member, we patch the edges portIndex
            // the cached value needs to be invalidated
            if (previousValue != null && (previousValue.NodeModel.Guid != nodeModel.Guid || previousValue.Direction != direction))
            {
                previousValue = null;
            }

            if (previousValue != null)
                return previousValue;

            previousValue = null;

            INodeModel nodeModel2 = null;
            nodeModel.GraphModel?.TryGetModelFromGuid(nodeModel.Guid, out nodeModel2);
            if (nodeModel2 != nodeModel)
            {
                NodeModel = nodeModel2;
            }

            var portHolder = nodeModel as IInputOutputPortsNodeModel;
            var portModelsByGuid = direction == PortDirection.Input ? portHolder?.InputsById : portHolder?.OutputsById;
            if (portModelsByGuid != null && UniqueId != null)
            {
                if (portModelsByGuid.TryGetValue(UniqueId, out var v))
                    previousValue = v;
            }
            return previousValue;
        }

        /// <summary>
        /// Gets a string representation of this instance.
        /// </summary>
        /// <returns>A string representation of this instance.</returns>
        public override string ToString()
        {
            if (m_GraphAssetModel != null)
            {
                return $"{m_GraphAssetModel.GetInstanceID()}:{m_NodeModelGuid}@{UniqueId}";
            }
            return String.Empty;
        }

        /// <summary>
        /// Adds a placeholder port on the node to represent a missing port that is represented by this instance.
        /// </summary>
        /// <param name="direction">Direction of the placeholder port.</param>
        /// <returns>True if the placeholder port was added.</returns>
        public bool AddPlaceHolderPort(PortDirection direction)
        {
            if (!(NodeModel is NodeModel n))
                return false;
            n.AddPlaceHolderPort(direction, UniqueId);
            return true;
        }
    }
}
