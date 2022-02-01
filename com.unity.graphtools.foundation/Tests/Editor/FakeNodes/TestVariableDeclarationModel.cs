using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    public class TestVariableDeclarationModel : VariableDeclarationModel
    {
        [SerializeField]
        int m_CustomValue;
        public int CustomValue
        {
            get => m_CustomValue;
            set => m_CustomValue = value;
        }
    }
}
