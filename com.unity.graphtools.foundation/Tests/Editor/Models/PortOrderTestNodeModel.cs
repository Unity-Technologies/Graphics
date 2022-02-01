using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    [Serializable]
    class PortOrderTestNodeModel : NodeModel
    {
        List<string> m_InputNames = new List<string>();
        List<string> m_InputIds = new List<string>();

        List<int> m_PortOrdering = new List<int>(); // order in which to declare InputNames and InputIds

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            Assert.That(m_InputNames.Count, Is.EqualTo(m_InputIds.Count));
            Assert.That(m_PortOrdering.Count, Is.EqualTo(m_InputIds.Count));

            for (int i = 0; i < m_PortOrdering.Count; i++)
            {
                this.AddDataInputPort<int>(m_InputNames[i], m_InputIds[i]);
            }
        }

        public void RandomizePorts()
        {
            // simple shuffle, swaps item[i] with item[[i+1..n-1]] n times
            for (int i = 0; i < m_PortOrdering.Count - 1; i++)
            {
                int temp = m_PortOrdering[i];

                // get an random index that's not the same
                int randomIndex = Random.Range(i + 1, m_PortOrdering.Count);

                m_PortOrdering[i] = m_PortOrdering[randomIndex];
                m_PortOrdering[randomIndex] = temp;
            }
        }

        public bool IsSorted()
        {
            for (int i = 0; i < m_PortOrdering.Count; i++)
            {
                if (i != m_PortOrdering[i])
                    return false;
            }

            return true;
        }

        public void AddInput(string inputName, string id = null)
        {
            m_InputNames.Add(inputName);
            m_InputIds.Add(id ?? inputName);
            m_PortOrdering.Add(m_PortOrdering.Count); // initially portOrdering is 0, 1, 2, 3...
        }

        public void MakePortsFromNames(IList<string> names, IList<string> ids = null)
        {
            m_InputNames.Clear();
            m_InputIds.Clear();
            m_PortOrdering.Clear();

            if (ids == null)
                ids = names.ToList();
            Assert.That(names.Count == ids.Count);
            for (int i = 0; i < names.Count; i++)
            {
                AddInput(names[i], ids[i]);
            }
        }
    }
}
