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

        [SerializeField]
        bool m_Expanded = true;

        public bool expanded
        {
            get { return m_Expanded; }
        }

        [NonSerialized]
        GraphData m_Graph;

        [NonSerialized]
        List<ShaderInput> m_Inputs = new List<ShaderInput>();

        public List<ShaderInput> inputs
        {
            get { return m_Inputs; }
        }

        [NonSerialized]
        BlackboardCateogrySection_V m_BlackboardSection;

        public BlackboardCateogrySection_V blackboardSection
        {
            get
            {
                return m_BlackboardSection;
            }
        }

        public void CreateBlackboardSection(GraphData graph)
        {
            m_BlackboardSection = new BlackboardCateogrySection_V(this, graph);
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedInputs = new List<SerializationHelper.JSONSerializedElement>();

        #region ShaderInputs

        public void AddShaderInput(ShaderInput input, int index = -1)
        {
            if (index < 0)
                m_Inputs.Add(input);
            else
                m_Inputs.Insert(index, input);

            BlackboardProvider.needsUpdate = true;
        }

        public void RemoveShaderInput(ShaderInput input)
        {
            m_Inputs.Remove(input);
        }

        public void RemoveShaderInputByGuid(Guid guid)
        {
            m_Inputs.RemoveAll(x => x.guid == guid);

            BlackboardProvider.needsUpdate = true;
        }

        // True if the input was moved TODO: y probably remove that though lol...
        public bool MoveShaderInput(ShaderInput input, int newIndex)
        {
            if (newIndex > m_Inputs.Count || newIndex < 0)
                throw new ArgumentException("New index is not within keywords list.");

            var currentIndex = m_Inputs.IndexOf(input);
            if (currentIndex == -1)
                throw new ArgumentException("Input is not in Input Category.");

            if (newIndex == currentIndex)
                return false;

            m_Inputs.RemoveAt(currentIndex);
            if (newIndex > currentIndex)
                newIndex--;

            if (newIndex == m_Inputs.Count)
                m_Inputs.Add(input);
            else
                m_Inputs.Insert(newIndex, input);

            BlackboardProvider.needsUpdate = true;

            return true;
        }

        #endregion

        #region Serialization

        public void OnBeforeSerialize()
        {
            // TODO: does making the ShaderInput cause Property or Keyword specific serialized fields to get lost? probably?
            m_SerializedInputs = SerializationHelper.Serialize<ShaderInput>(m_Inputs);
        }

        public void OnAfterDeserialize()
        {
            m_Inputs = SerializationHelper.Deserialize<ShaderInput>(m_SerializedInputs, GraphUtil.GetLegacyTypeRemapping());
        }

        #endregion

    }
}
