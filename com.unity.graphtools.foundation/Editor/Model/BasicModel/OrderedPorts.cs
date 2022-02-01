using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A container for the <see cref="IPortModel"/>s of a node.
    /// </summary>
    public class OrderedPorts : IReadOnlyDictionary<string, IPortModel>, IReadOnlyList<IPortModel>
    {
        Dictionary<string, IPortModel> m_Dictionary;
        List<int> m_Order;
        List<IPortModel> m_PortModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedPorts"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity of the container.</param>
        public OrderedPorts(int capacity = 0)
        {
            m_Dictionary = new Dictionary<string, IPortModel>(capacity);
            m_Order = new List<int>(capacity);
            m_PortModels = new List<IPortModel>(capacity);
        }

        /// <summary>
        /// Adds a port at the end of the container.
        /// </summary>
        /// <param name="portModel">The port to add.</param>
        public void Add(IPortModel portModel)
        {
            m_Dictionary.Add(portModel.UniqueName, portModel);
            m_PortModels.Add(portModel);
            m_Order.Add(m_Order.Count);

            var graphModel = portModel.NodeModel?.GraphModel as GraphModel;
            graphModel?.PortEdgeIndex.MarkDirty();
        }

        /// <summary>
        /// Removes a port from the container.
        /// </summary>
        /// <param name="portModel">The port model to remove.</param>
        /// <returns>True if the port was removed. False otherwise.</returns>
        public bool Remove(IPortModel portModel)
        {
            bool found = false;
            if (m_Dictionary.ContainsKey(portModel.UniqueName))
            {
                m_Dictionary.Remove(portModel.UniqueName);
                found = true;
                int index = m_PortModels.FindIndex(x => x == portModel);
                m_PortModels.Remove(portModel);
                m_Order.Remove(index);
                for (int i = 0; i < m_Order.Count; ++i)
                {
                    if (m_Order[i] > index)
                        --m_Order[i];
                }
            }

            var graphModel = portModel.NodeModel?.GraphModel as GraphModel;
            graphModel?.PortEdgeIndex.MarkDirty();

            return found;
        }

        /// <summary>
        /// Exchanges port positions in the container.
        /// </summary>
        /// <param name="a">The first port to swap.</param>
        /// <param name="b">The second port to swap.</param>
        public void SwapPortsOrder(IPortModel a, IPortModel b)
        {
            int indexA = m_PortModels.IndexOf(a);
            int indexB = m_PortModels.IndexOf(b);
            int oldAOrder = m_Order[indexA];
            m_Order[indexA] = m_Order[indexB];
            m_Order[indexB] = oldAOrder;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, IPortModel>> GetEnumerator() => m_Dictionary.GetEnumerator();

        /// <summary>
        /// Gets an enumerator for the ports stored in the container.
        /// </summary>
        /// <returns>An enumerator for the ports stored in the container.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The number of ports in the container.
        /// </summary>
        public int Count => m_Dictionary.Count;

        /// <summary>
        /// Checks whether the container contains a port with the name <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The name of the port.</param>
        /// <returns></returns>
        public bool ContainsKey(string key) => m_Dictionary.ContainsKey(key);

        /// <summary>
        /// Tries retrieving a port using its name.
        /// </summary>
        /// <param name="key">The name of the port.</param>
        /// <param name="value">The port found, if any.</param>
        /// <returns>True if a port matching the name was found.</returns>
        public bool TryGetValue(string key, out IPortModel value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets the port using its <see cref="IPortModel.UniqueName"/>.
        /// </summary>
        /// <param name="key">The name of the port.</param>
        public IPortModel this[string key] => m_Dictionary[key];

        /// <summary>
        /// The keys of the ports stored in the container, which are their <see cref="IPortModel.UniqueName"/>.
        /// </summary>
        public IEnumerable<string> Keys => m_Dictionary.Keys;

        /// <summary>
        /// The ports stored in the container.
        /// </summary>
        public IEnumerable<IPortModel> Values => m_Dictionary.Values;

        /// <summary>
        /// Gets an enumerator for the ports stored in the container.
        /// </summary>
        /// <returns>An enumerator for the ports stored in the container.</returns>
        IEnumerator<IPortModel> IEnumerable<IPortModel>.GetEnumerator()
        {
            Assert.AreEqual(m_Order.Count, m_PortModels.Count, "these lists are supposed to always be of the same size");
            return m_Order.Select(i => m_PortModels[i]).GetEnumerator();
        }

        /// <summary>
        /// Gets the port at index.
        /// </summary>
        /// <param name="index">The index.</param>
        public IPortModel this[int index] => m_PortModels[m_Order[index]];
    }
}
