using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    class InputCategory
    {
        [SerializeField]
        string m_Header = "";

        public string header
        {
            get { return m_Header; }
            set { m_Header = value;  }
        }

        [NonSerialized]
        BlackboardCateogrySection m_BlackboardSection;

        public BlackboardCateogrySection blackboardSection
        {
            get
            {
                if (m_BlackboardSection == null)
                    m_BlackboardSection = new BlackboardCateogrySection(this);

                return m_BlackboardSection;
            }
        }

        [NonSerialized]
        List<ShaderInput> m_Inputs = new List<ShaderInput>();

        public IEnumerable<ShaderInput> inputs
        {
            get { return m_Inputs; }
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedInputs = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        bool m_Expanded = true;

        public bool expanded
        {
            get { return m_Expanded; }
        }

        public void ToggleCollapse()
        {
            m_Expanded = !m_Expanded;
            blackboardSection.GetFirstAncestorOfType<MaterialGraphView>().graph.SectionChangesHappened();

            if (m_Expanded)
                m_BlackboardSection.AddToClassList("expanded");
            else
                m_BlackboardSection.AddToClassList("expanded");
        }

        #region ShaderInputs

        internal ShaderInput GetInput(int index)
        {
            return m_Inputs[index];
        }

        internal int GetInputIndex(ShaderInput input)
        {
            return m_Inputs.IndexOf(input);
        }

        internal void AddInput(ShaderInput input, int index)
        {
            if (index < 0 || index > m_Inputs.Count)
                m_Inputs.Add(input);
            else
                m_Inputs.Insert(index, input);
        }

        internal void RemoveInputByGuid(Guid guid)
        {
            m_Inputs.RemoveAll(x => x.guid == guid);
        }

        internal void MoveShaderInput(ShaderInput input, int newIndex)
        {
            if (newIndex > m_Inputs.Count || newIndex < 0)
                throw new ArgumentException("New index is not within keywords list. newIndex=" + newIndex + " m_Inputs.Count=" + m_Inputs.Count);

            var currentIndex = m_Inputs.IndexOf(input);
            if (currentIndex == -1)
                throw new ArgumentException("Input is not in the Input Category.");

            if (newIndex == currentIndex)
                return;

            m_Inputs.RemoveAt(currentIndex);
            if (newIndex > currentIndex)
                newIndex--;

            if (newIndex == m_Inputs.Count)
                m_Inputs.Add(input);
            else
                m_Inputs.Insert(newIndex, input);
        }

        #endregion

        #region Serialization

        internal void OnBeforeSerialize()
        {
            // TODO: does making the ShaderInput cause Property or Keyword specific serialized fields to get lost? probably?
            m_SerializedInputs = SerializationHelper.Serialize<ShaderInput>(m_Inputs);
        }

        internal void OnAfterDeserialize()
        {
            m_Inputs = SerializationHelper.Deserialize<ShaderInput>(m_SerializedInputs, GraphUtil.GetLegacyTypeRemapping());
        }

        #endregion

    }
}
