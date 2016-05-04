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
        public VFXPropertySlot Slot     { get { return m_Slot; } }
        public VFXDataBlock DataBlock   { get { return m_DataBlock; } }

        public string m_exposedName;
        protected VFXDataBlock m_DataBlock;
        private VFXOutputSlot m_Slot;

        public VFXEdDataNodeBlock(VFXDataBlock datablock, VFXEdDataSource dataSource, string exposedName) : base(dataSource)
        {
            m_LibraryName = datablock.Name; // TODO dont store the same stuff at two different location
            m_DataBlock = datablock;
            m_exposedName = exposedName;
            
            // For selection
            target = ScriptableObject.CreateInstance<VFXEdDataNodeBlockTarget>();
            (target as VFXEdDataNodeBlockTarget).targetNodeBlock = this;

            m_Slot = new VFXOutputSlot(DataBlock.Property);
            m_Fields = new VFXUIPropertySlotField[1];
            m_Fields[0] = new VFXUIPropertySlotField(dataSource, m_Slot);
            AddChild(m_Fields[0]);

            AddChild(new VFXEdNodeBlockHeader(m_LibraryName, m_DataBlock.Icon, true));
            AddManipulator(new TooltipManipulator(GetTooltipText));
            Layout();
        }

        public List<string> GetTooltipText()
        {
            List<string> lines = new List<string>();
            lines = VFXModelDebugInfoProvider.GetInfo(lines, this, VFXModelDebugInfoProvider.InfoFlag.kDefault);
            return lines;
        }

        public override VFXPropertySlot GetSlot(string name)
        {
            return Slot.Name.Equals(name) ? Slot : null;
        }

        public override void SetSlotValue(string name, VFXValue value)
        {
            if (Slot.Name.Equals(name))
                Slot.Value = value;
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
