
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Type = System.Type;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    partial class VFXEditableDataAnchor : VFXDataAnchor
    {
        PropertyRM      m_PropertyRM;

        VFXView m_View;


        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXEditableDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            Profiler.BeginSample("VFXEditableDataAnchor.Create");

            var anchor = new VFXEditableDataAnchor(controller.orientation, controller.direction, controller.portType, node);
            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            Profiler.EndSample();
            return anchor;
        }

        protected VFXEditableDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
            Profiler.BeginSample("VFXEditableDataAnchor.VFXEditableDataAnchor");
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            Profiler.EndSample();
        }

        public void AssetMoved()
        {
            m_PropertyRM.UpdateGUI(true);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            evt.menu.AppendAction("Copy Value", OnCopyValue);
            evt.menu.AppendAction("Paste Value", OnPasteValue, OnValidatePasteValue);
        }

        static object s_Clipboard;

        void OnCopyValue(DropdownMenuAction a)
        {
            s_Clipboard = controller.value;
        }
        void OnPasteValue(DropdownMenuAction a)
        {
            controller.value = VFXConverter.ConvertTo(s_Clipboard, portType);
        }

        DropdownMenuAction.Status OnValidatePasteValue(DropdownMenuAction a)
        {
            return s_Clipboard != null && controller.editable && VFXConverter.CanConvertTo(s_Clipboard.GetType(), portType) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            m_View = GetFirstAncestorOfType<VFXView>();
            if( m_View == null)
            {
                //This can happen with asynchnous events.
                return;
            }
            m_View.allDataAnchors.Add(this);
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            if (m_View != null)
                m_View.allDataAnchors.Remove(this);
        }

        public float GetPreferredLabelWidth()
        {
            if (m_PropertyRM == null) return 0;
            return m_PropertyRM.GetPreferredLabelWidth();
        }

        public float GetPreferredControlWidth()
        {
            if (m_PropertyRM == null) return 0;
            return m_PropertyRM.GetPreferredControlWidth();
        }

        public void SetLabelWidth(float label)
        {
            m_PropertyRM.SetLabelWidth(label);
        }

        public void ForceUpdate()
        {
            m_PropertyRM.ForceUpdate();
        }

        void BuildProperty()
        {
            Profiler.BeginSample("VFXNodeUI.BuildProperty");
            if (m_PropertyRM != null)
            {
                Remove(m_PropertyRM);
            }
            m_PropertyRM = PropertyRM.Create(controller, VFXNodeUI.DefaultLabelWidth);
            if (m_PropertyRM != null)
            {
                Add(m_PropertyRM);
            }
            Profiler.EndSample();
        }

        public override void SelfChange(int change)
        {
            Profiler.BeginSample("VFXEditableDataAnchor.SelfChange");
            base.SelfChange(change);

            if (m_PropertyRM == null || !m_PropertyRM.IsCompatible(controller))
                BuildProperty();

            OnRecompile(false);
            Profiler.EndSample();
        }

        public void OnRecompile(bool valueOnly)
        {
            if (m_PropertyRM != null && controller != null)
            {
                if (!valueOnly)
                {
                    controller.UpdateInfos();
                    bool editable = controller.editable;
                    m_PropertyRM.propertyEnabled = editable && controller.expandedInHierachy;
                    m_PropertyRM.indeterminate = !editable && controller.indeterminate;
                    m_PropertyRM.Update();
                }
                else
                    m_PropertyRM.UpdateValue();
            }
        }

        public Rect internalRect
        {
            get
            {
                Rect layout = this.layout;
                return new Rect(0.0f, 0.0f, layout.width, layout.height);
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return internalRect.Contains(localPoint);
        }
    }
}
