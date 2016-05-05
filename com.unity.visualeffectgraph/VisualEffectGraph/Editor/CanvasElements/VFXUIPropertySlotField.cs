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
    internal class VFXUIPropertySlotField : CanvasElement, VFXPropertySlotObserver
    {
        public VFXPropertySlot Slot                 { get { return m_Slot; } }
        public VFXProperty Property                 { get { return Slot.Property; } }
        public VFXPropertyTypeSemantics Semantics   { get { return Property.m_Type; } }
        public string Name                          { get { return Property.m_Name; } }
        public VFXValueType ValueType               { get { return Slot.ValueType; } }
        public VFXUIPropertyAnchor Anchor           { get { return m_Anchor; } }

        private VFXEdDataSource m_DataSource;
        private VFXPropertySlot m_Slot;
        private VFXUIPropertyAnchor m_Anchor;
        private Direction m_Direction;
        private uint m_Depth;

        private bool m_Enabled; // Can we edit param value directly (it is disabled if linked or one of its children is linked)
        private bool m_ChildrenCollapsed = true;
        private bool m_FieldCollapsed; // Use another bool than collapsed as we dont want it to be propagated to children when uncollapsing

        private new VFXUIPropertySlotField[] m_Children;

        public VFXUIPropertySlotField(VFXEdDataSource dataSource, VFXPropertySlot slot, uint depth = 0)
        {
            m_DataSource = dataSource; 
            m_Slot = slot;
            m_Depth = depth;

            slot.AddObserver(this);

            if (slot is VFXInputSlot)           m_Direction = Direction.Input;
            else if (slot is VFXOutputSlot)     m_Direction = Direction.Output;
            else throw new ArgumentException("Invalid property slot");

            m_Anchor = new VFXUIPropertyAnchor(this, dataSource, Vector3.zero, m_Direction);
            AddChild(m_Anchor);

            m_FieldCollapsed = depth > 0;

            if (Slot.GetNbChildren() > 0)
            {
                 AddManipulator(new PropertySlotFieldCollapse(GetCollapserRect()));
            }

            m_Children = new VFXUIPropertySlotField[Slot.GetNbChildren()];
            for (int i = 0; i < Slot.GetNbChildren(); ++i)
                AddChild(m_Children[i] = new VFXUIPropertySlotField(m_DataSource, Slot.GetChild(i), m_Depth + 1));
        }

        public virtual void OnSlotEvent(VFXPropertySlot.Event type, VFXPropertySlot slot)
        {
            if (!Collapsed())
            {
                Invalidate();
                Canvas2D canvas = ParentCanvas();
                if(canvas != null) canvas.Repaint();
            }
        }

        private Rect GetCollapserRect()
        {
            return new Rect((VFXEditorMetrics.ParameterFieldIndentWidth * m_Depth)+ VFXEditorMetrics.ParameterFieldRectOffset.left, 0.0f, 16.0f, 16.0f);
        }

        public override bool DispatchEvents(Event evt, Canvas2D parent)
        {
            return Collapsed() ? false : base.DispatchEvents(evt, parent);
        }

        public override void Layout()
        {
            base.Layout();

            if (!Collapsed())
            {
                scale = new Vector2(parent.scale.x, VFXEditorMetrics.NodeBlockParameterHeight + (VFXEditorMetrics.NodeBlockParameterHeight + VFXEditorMetrics.NodeBlockParameterSpacingHeight) * GetNbChildrenUncollapsed());
                float childY = VFXEditorMetrics.NodeBlockParameterHeight + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
                foreach (var child in m_Children)
                {
                    Vector3 childPos = child.translation;
                    childPos.y = childY;
                    child.translation = childPos;
                    childY += child.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
                }
                if (m_Direction == Direction.Output)
                    m_Anchor.translation = new Vector2(scale.x - VFXEditorMetrics.DataAnchorSize.x, m_Anchor.translation.y);
            }
            else
            {
                scale = Vector2.zero;
                translation = Vector2.zero;
            }

            base.Layout();
        }

        public bool Collapsed() 
        { 
            return collapsed || m_FieldCollapsed; 
        }

        public bool IsConnected()
        {
            return Slot.CurrentValueRef != Slot;
        }

        public void CollapseChildren(bool collapse)
        {
            m_ChildrenCollapsed = collapse;
            UpdateModel(UpdateType.Update);
            foreach (var child in m_Children)
            {
                child.m_FieldCollapsed = m_ChildrenCollapsed;
                if (m_ChildrenCollapsed)
                    child.CollapseChildren(true);
            }
        }

        public void ToggleCollapseChildren()
        {
            m_ChildrenCollapsed = !m_ChildrenCollapsed;
            CollapseChildren(m_ChildrenCollapsed);
        }

        public void DisconnectChildren()
        {
            foreach (var child in m_Children)
            {
                m_DataSource.RemoveConnectedEdges<VFXUIPropertyEdge,VFXUIPropertyAnchor>(child.m_Anchor);
                child.DisconnectChildren();
            }
        }

        public int GetNbChildrenUncollapsed()
        {
            int nbChildren = 0;
            foreach (var child in m_Children)
                if (!child.Collapsed())
                    nbChildren += 1 + child.GetNbChildrenUncollapsed();
            return nbChildren;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            if (!Collapsed())
            {
                Rect r = GetDrawableRect();


                Rect collapserRect = GetCollapserRect();
                collapserRect.position += r.position;

                Rect fieldrect = VFXEditorMetrics.ParameterFieldRectOffset.Remove(r);
                Rect labelrect = new Rect(fieldrect.x + 20.0f + m_Depth * VFXEditorMetrics.ParameterFieldIndentWidth, fieldrect.y, VFXEditorMetrics.ParameterFieldLabelWidth -20 , VFXEditorMetrics.NodeBlockParameterHeight);
                Rect editrect = new Rect(fieldrect.x + 20.0f +  VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.y, fieldrect.width - VFXEditorMetrics.ParameterFieldLabelWidth -20, VFXEditorMetrics.NodeBlockParameterHeight);

                //Rect lineRect = new Rect(r.x, r.y-(VFXEditorMetrics.NodeBlockParameterSpacingHeight/2), r.width, 1);
                //EditorGUI.DrawRect(lineRect, new Color(0, 0, 0, 0.25f));

                EditorGUI.BeginDisabledGroup(IsConnected());
                EditorGUI.LabelField(labelrect, Name);

                Semantics.OnCanvas2DGUI(m_Slot, editrect);

                // Collapse icon
                if (m_Children.Length > 0)
                {
                    if(m_ChildrenCollapsed)
                    {
                        GUI.Box(collapserRect, "", VFXEditor.styles.CollapserClosed);
                    }
                    else
                    {
                        GUI.Box(collapserRect, "", VFXEditor.styles.CollapserOpen);
                    }
                }
                    
                else
                {
                    GUI.Box(collapserRect, "", VFXEditor.styles.CollapserDisabled);
                }
                    //EditorGUI.LabelField(collapserRect,m_ChildrenCollapsed ? "+" : "-");

                EditorGUI.EndDisabledGroup();
            }
        }

        public override void UpdateModel(UpdateType t)
        {
            Slot.UpdateCollapsed(m_ChildrenCollapsed);
        }
    }
}

