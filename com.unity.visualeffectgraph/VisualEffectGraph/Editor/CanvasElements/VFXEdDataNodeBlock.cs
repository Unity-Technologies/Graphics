using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataNodeBlock : VFXEdNodeBlockDraggable
    {
        public List<VFXDataParam> Params { get { return m_DataBlock.Parameters; } }
        public VFXDataBlock DataBlock { get { return m_DataBlock; } }
        public VFXOutputSlot[] Slots { get { return m_Slots; } }

        public string m_exposedName;
        protected VFXDataBlock m_DataBlock;

        private VFXOutputSlot[] m_Slots; // TODO Refactor Only one slot per data block!

        public VFXEdDataNodeBlock(VFXDataBlock datablock, VFXEdDataSource dataSource, string exposedName) : base(dataSource)
        {
            m_LibraryName = datablock.name;
            m_DataBlock = datablock;
            m_exposedName = exposedName;

            m_Slots = new VFXOutputSlot[m_DataBlock.Parameters.Count];
            m_Fields = new VFXEdNodeBlockParameterField[m_DataBlock.Parameters.Count];
            
            // For selection
            target = ScriptableObject.CreateInstance<VFXEdDataNodeBlockTarget>();
            (target as VFXEdDataNodeBlockTarget).targetNodeBlock = this;

            int i = 0;
            foreach(VFXDataParam p in m_DataBlock.Parameters) {
                m_Slots[i] = new VFXOutputSlot(new VFXProperty(VFXPropertyConverter.CreateSemantics(VFXPropertyConverter.ConvertType(p.m_type)), p.m_Name));
                m_Fields[i] = new VFXEdNodeBlockParameterField(dataSource as VFXEdDataSource, p.m_Name , m_Slots[i], true, Direction.Output, i);
                AddChild(m_Fields[i]);
                i++;
            }

            AddChild(new VFXEdNodeBlockHeader(m_LibraryName, m_DataBlock.icon, datablock.Parameters.Count > 0));
            AddManipulator(new ImguiContainer());
            Layout();
        }

        public VFXEdDataNodeBlock(VFXDataBlock datablock, VFXEdDataSource dataSource, string exposedName, VFXEdEditingWidget widget) : this(datablock, dataSource, exposedName)
        {
            editingWidget = widget;
        }

        public override VFXPropertySlot GetSlot(string name)
        {
            foreach (var slot in Slots)
                if (slot.Name.Equals(name))
                    return slot;
            return null;
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
