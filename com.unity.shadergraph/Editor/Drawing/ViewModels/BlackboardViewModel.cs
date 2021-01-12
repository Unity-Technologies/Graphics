using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.ShaderGraph.Drawing.Views.Blackboard
{
    class BlackboardViewModel : SGViewModel
    {
        List<Type> m_ShaderInputTypes;

        public override void ConstructFromModel(GraphData graphData)
        {
            m_ShaderInputTypes = TypeCache.GetTypesWithAttribute<BlackboardInputInfo>().ToList();
        }

        public override void WriteToModel()
        {
            throw new System.NotImplementedException();
        }
    }
}
