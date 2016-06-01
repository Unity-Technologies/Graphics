using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataNodeBlock : VFXEdNodeBlockDraggable
    {
        public VFXDataBlockModel Model      { get { return m_Model; }}
        public VFXPropertySlot Slot         { get { return m_Model.Slot; } }
        public VFXDataBlockDesc Desc        { get { return m_Model.Desc; } }
        public VFXUIPropertyAnchor Anchor   { get { return m_Fields[0].Anchor; } }

        public override VFXElementModel GetAbstractModel() { return Model; }

        public string ExposedName { get { return m_Model.ExposedName; } }

        private VFXDataBlockModel m_Model;

        public VFXEdDataNodeBlock(VFXDataBlockModel model, VFXEdDataSource dataSource)
            : base(dataSource)
        {
            m_Model = model;

            m_LibraryName = Desc.Name; // TODO dont store the same stuff at two different location
            
            // For selection
            target = ScriptableObject.CreateInstance<VFXEdDataNodeBlockTarget>();
            (target as VFXEdDataNodeBlockTarget).targetNodeBlock = this;

            m_Fields = new VFXUIPropertySlotField[1];
            m_Fields[0] = new VFXUIPropertySlotField(dataSource, Slot);
            AddChild(m_Fields[0]);

            AddChild(new VFXEdNodeBlockHeaderEditable(model, VFXEditor.styles.GetIcon(Desc.Icon), true));
            AddManipulator(new TooltipManipulator(GetTooltipText));
            Layout();
        }

        public override void UpdateModel(UpdateType t)
        {
            Model.UpdateCollapsed(collapsed);
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
