using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataNodeBlock : VFXEdNodeBlockDraggable
    {
        public List<VFXDataParam> Params { get { return m_DataBlock.Parameters; } } 
        public VFXParamValue[] ParamValues { get { return m_ParamValues; } }
        public VFXDataBlock DataBlock { get { return m_DataBlock; } }

        public string m_exposedName;
        protected VFXParamValue[] m_ParamValues;
        protected VFXDataBlock m_DataBlock;


        public VFXEdDataNodeBlock(VFXDataBlock datablock, VFXEdDataSource dataSource, string exposedName) : base(dataSource)
        {
            m_Name = datablock.name;
            m_DataBlock = datablock;
            m_exposedName = exposedName;

            m_ParamValues = new VFXParamValue[m_DataBlock.Parameters.Count];
            m_Fields = new VFXEdNodeBlockParameterField[m_DataBlock.Parameters.Count];
            
            // For selection
            target = ScriptableObject.CreateInstance<VFXEdDataNodeBlockTarget>();
            (target as VFXEdDataNodeBlockTarget).targetNodeBlock = this;

            int i = 0;
            foreach(VFXDataParam p in m_DataBlock.Parameters) {
                m_ParamValues[i] = VFXParamValue.Create(p.m_type);
                m_Fields[i] = new VFXEdNodeBlockParameterField(dataSource as VFXEdDataSource, p.m_Name , m_ParamValues[i], true, Direction.Output, i);
                AddChild(m_Fields[i]);
                i++;
            }

            AddChild(new VFXEdNodeBlockHeader(m_Name, m_DataBlock.icon, datablock.Parameters.Count > 0));
            AddManipulator(new ImguiContainer());
            Layout();
        }

        public VFXEdDataNodeBlock(VFXDataBlock datablock, VFXEdDataSource dataSource, string exposedName, VFXEdEditingWidget widget) : this(datablock, dataSource, exposedName)
        {
            editingWidget = widget;
        }

        public override void SetParamValue(string name, VFXParamValue value)
        {
            int i = Params.IndexOf(Params.Find(parm => parm.m_Name == name));
            ParamValues[i].SetValue(value);
        }

        public override VFXParamValue GetParamValue(string name)
        {
            int i = Params.IndexOf(Params.Find(parm => parm.m_Name == name));
            return ParamValues[i];
        }

        protected override GUIStyle GetNodeBlockStyle()
        {
            return VFXEditor.styles.DataNodeBlock;
        }

        protected override GUIStyle GetNodeBlockSelectedStyle()
        {
            return VFXEditor.styles.DataNodeBlockSelected;
        }

    }
}
