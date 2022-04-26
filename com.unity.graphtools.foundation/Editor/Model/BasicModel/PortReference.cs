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
    struct PortReference : ISerializationCallbackReceiver
    {
        [SerializeField, FormerlySerializedAs("NodeModelGuid")]
        SerializableGUID m_NodeModelGuid;

        [SerializeField, FormerlySerializedAs("UniqueId")]
        string m_UniqueId;

        [SerializeField]
        string m_Title;

        IGraphModel m_GraphModel;

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
        /// The title of the port referenced by this instance.
        /// </summary>
        public string Title
        {
            get => m_Title;
            // for tests
            internal set => m_Title = value;
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
                    if (m_GraphModel != null && m_GraphModel.TryGetModelFromGuid(m_NodeModelGuid, out var node))
                    {
                        m_NodeModel = node as INodeModel;
                    }
                }

                return m_NodeModel;
            }

            private set
            {
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
            m_GraphModel = portModel.NodeModel.GraphModel;
            NodeModel = portModel.NodeModel;
            m_UniqueId = portModel.UniqueName;
            m_Title = (portModel as IHasTitle)?.Title;
        }

        /// <summary>
        /// Sets the graph model used to resolve the port reference.
        /// </summary>
        /// <remarks>The intended use of this method is to initialize the <see cref="m_GraphModel"/> after deserialization.</remarks>
        /// <param name="graphModel">The graph model in which this port reference lives.</param>
        public void AssignGraphModel(IGraphModel graphModel)
        {
            m_GraphModel = graphModel;
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
            return $"{m_GraphModel.Guid.ToString()}:{m_NodeModelGuid}@{UniqueId}";
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
            n.AddPlaceHolderPort(direction, UniqueId, portName: Title);
            return true;
        }

        /// <summary>
        /// Resets internal cache.
        /// </summary>
        public void ResetCache()
        {
            m_NodeModel = null;
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            m_GraphModel = null;
        }
    }
}
