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
    internal class VFXEdProcessingNodeBlock : VFXEdNodeBlockDraggable
    {
        public VFXBlockModel Model { get { return m_Model; } }
        public VFXBlockDesc Desc { get { return Model.Desc; } }
        public VFXEdNodeBlockHeaderToggleable Header { get { return m_Header; } }

        private VFXProperty[] Properties { get { return Model.Properties; } }

        public override VFXElementModel GetAbstractModel() { return Model; }

        private VFXBlockModel m_Model;
        private VFXEdNodeBlockHeaderToggleable m_Header;

        public VFXEdProcessingNodeBlock(VFXBlockModel model, VFXEdDataSource dataSource) : base(dataSource)
        {
            m_Model = model;
            
            // For selection
            target = ScriptableObject.CreateInstance<VFXEdProcessingNodeBlockTarget>();
            (target as VFXEdProcessingNodeBlockTarget).targetNodeBlock = this;

            if (Properties != null && Properties.Length > 0)
            {
                int nbProperties = Properties.Length;
                m_Fields = new VFXUIPropertySlotField[nbProperties];
                for (int i = 0; i < nbProperties; ++i)
                {
                    m_Fields[i] = new VFXUIPropertySlotField(dataSource, Model.GetSlot(i));
                    AddChild(m_Fields[i]);
                }
            }
            else
                m_Fields = new VFXUIPropertySlotField[0];

            m_LibraryName = Model.Desc.Name;

            m_Header = new VFXEdNodeBlockHeaderToggleable(Desc.Category.Replace('/', ' ') + " : " + Desc.Name, VFXEditor.styles.GetIcon(Desc.Icon == "" ? "Default" : Desc.Icon), model.Properties.Length > 0, Model);
            AddChild(m_Header);
            AddManipulator(new TooltipManipulator(GetTooltipText));
            Layout();
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            Model.Detach();
        }

        public List<string> GetTooltipText()
        {
            List<string> lines = new List<string>();
            lines = VFXModelDebugInfoProvider.GetInfo(lines, Model, VFXModelDebugInfoProvider.InfoFlag.kDefault);
            return lines;
        }

        public override VFXPropertySlot GetSlot(string name)
        {
            for (int i = 0; i < Model.GetNbSlots(); ++i)
                if (Model.GetSlot(i).Name.Equals(name))
                    return Model.GetSlot(i);
            return null;
        }

        public override void SetSlotValue(string name, VFXValue value)
        {
            for (int i = 0; i < Model.GetNbSlots(); ++i)
                if (Model.GetSlot(i).Name.Equals(name))
                {
                    Model.GetSlot(i).Value = value;
                    break;
                }
        }

        protected override GUIStyle GetNodeBlockStyle()
        {
            return VFXEditor.styles.NodeBlock;
        }

        protected override GUIStyle GetNodeBlockSelectedStyle()
        {
            return VFXEditor.styles.NodeBlockSelected;
        }

        public override void UpdateModel(UpdateType t)
        {
            Model.UpdateCollapsed(collapsed);
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            Rect r = GetDrawableRect();
            Texture2D tile = VFXEditor.styles.DisabledScopeTileable;

            Rect tileRect = new Rect(0, 0, r.width / tile.width, r.height / tile.height);

            if (!Model.Enabled)
                //EditorGUI.DrawRect(r, new Color(0.25f,0.25f,0.25f,0.5f));
                GUI.DrawTextureWithTexCoords(r, tile, tileRect);
        }
    }
}
