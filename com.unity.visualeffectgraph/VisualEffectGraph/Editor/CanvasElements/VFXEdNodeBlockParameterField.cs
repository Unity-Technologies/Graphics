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
    internal class VFXUIPropertySlotField : CanvasElement
    {
        public VFXPropertySlot Slot                 { get { return m_Slot; } }
        public VFXProperty Property                 { get { return Slot.Property; } }
        public VFXPropertyTypeSemantics Semantics   { get { return Property.m_Type; } }
        public string Name                          { get { return Property.m_Name; } }
        public VFXValueType ValueType               { get { return Slot.ValueType; } }
        public VFXUIPropertyAnchor Anchor           { get { return m_Anchor; } }

        private VFXPropertySlot m_Slot;
        private VFXUIPropertyAnchor m_Anchor;
        private Direction m_Direction;
        private uint m_Depth;

        private VFXUIPropertySlotField[] m_Children;

        public VFXUIPropertySlotField(VFXEdDataSource dataSource, VFXPropertySlot slot, uint depth = 0)
        {
            m_Slot = slot;
            m_Depth = depth;

            if (slot is VFXInputSlot)           m_Direction = Direction.Input;
            else if (slot is VFXOutputSlot)     m_Direction = Direction.Output;
            else throw new ArgumentException("Invalid property slot");

            m_Anchor = new VFXUIPropertyAnchor(dataSource, Vector3.zero, Slot, m_Direction);
            AddChild(m_Anchor);

            AddManipulator(new ImguiContainer());

            m_Children = new VFXUIPropertySlotField[Slot.GetNbChildren()];
            for (int i = 0; i < Slot.GetNbChildren(); ++i)
                AddChild(m_Children[i] = new VFXUIPropertySlotField(dataSource, Slot.GetChild(i), m_Depth + 1));
        }

        public override bool DispatchEvents(Event evt, Canvas2D parent)
        {
            return collapsed ? false : base.DispatchEvents(evt, parent);
        }

        public override void Layout()
        {
            base.Layout();

            if (!collapsed)
            {
                scale = new Vector2(parent.scale.x, VFXEditorMetrics.NodeBlockParameterHeight + (VFXEditorMetrics.NodeBlockParameterHeight + VFXEditorMetrics.NodeBlockParameterSpacingHeight) * GetNbChildrenDeep());
                float childY = VFXEditorMetrics.NodeBlockParameterHeight + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
                foreach (var child in m_Children)
                {
                    Vector3 childPos = child.translation;
                    childPos.y = childY;
                    child.translation = childPos;
                    //child.translation.y = childY;
                    childY += child.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
                }
                if (m_Direction == Direction.Output)
                    m_Anchor.translation = new Vector2(scale.x - VFXEditorMetrics.DataAnchorSize.x, m_Anchor.translation.y);
            }
            else
                scale = Vector2.zero;
        }

        public bool IsConnected()
        {
            return Slot.CurrentValueRef != Slot;
        }

        public int GetNbChildrenDeep()
        {
            int nbChildren = m_Children.Length;
            foreach (var child in m_Children)
                nbChildren += child.GetNbChildrenDeep();
            return nbChildren;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            EventType t = Event.current.type;
            if (!collapsed)
            {
                Rect r = GetDrawableRect();

                Rect fieldrect = VFXEditorMetrics.ParameterFieldRectOffset.Remove(r);
                Rect labelrect = new Rect(fieldrect.x + m_Depth * VFXEditorMetrics.ParameterFieldIndentWidth, fieldrect.y, VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.height);
                Rect editrect = new Rect(fieldrect.x + VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.y, fieldrect.width - VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.height);

                if (IsConnected())
                    GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.25f);

                EditorGUI.LabelField(labelrect, Name);

                switch (ValueType)
                {
                    case VFXValueType.kFloat:
                        m_Slot.SetValue(EditorGUI.FloatField(editrect, "", m_Slot.GetValue<float>()));
                        break;
                    case VFXValueType.kFloat2:
                        m_Slot.SetValue(EditorGUI.Vector2Field(editrect, "", m_Slot.GetValue<Vector2>()));
                        break;
                    case VFXValueType.kFloat3:
                        m_Slot.SetValue(EditorGUI.Vector3Field(editrect, "", m_Slot.GetValue<Vector3>()));
                        break;
                    case VFXValueType.kFloat4:
                        m_Slot.SetValue(EditorGUI.Vector4Field(editrect, "", m_Slot.GetValue<Vector4>()));
                        break;
                    case VFXValueType.kInt:
                        m_Slot.SetValue(EditorGUI.IntField(editrect, "", m_Slot.GetValue<int>()));
                        break;
                    case VFXValueType.kUint:
                        m_Slot.SetValue<uint>((uint)EditorGUI.IntField(editrect, "", (int)m_Slot.GetValue<uint>()));
                        break;
                    case VFXValueType.kTexture2D:
                        m_Slot.SetValue<Texture2D>((Texture2D)EditorGUI.ObjectField(editrect, m_Slot.GetValue<Texture2D>(), typeof(Texture2D)));
                        break;
                    case VFXValueType.kTexture3D:
                        m_Slot.SetValue<Texture3D>((Texture3D)EditorGUI.ObjectField(editrect, m_Slot.GetValue<Texture3D>(), typeof(Texture3D)));
                        break;
                }
                GUI.color = Color.white;
            }
        }
    }

    [Obsolete]
    internal class VFXEdNodeBlockParameterField : CanvasElement
    {
        public VFXPropertySlot Value { get { return m_Slot; } }
        public bool Visible { get { return m_Visible; } set { m_Visible = value; } }
        public string Name { get { return m_Name; } }
        public string Tag {get { return m_Tag; } }
        public VFXValueType Type { get { return m_Slot.ValueType; } }
        public VFXEdDataAnchor Input { get { return m_Input; } }
        public VFXEdDataAnchor Output { get { return m_Output; } }

        protected string m_Name;
        protected string m_Tag = "";
        protected VFXPropertySlot m_Slot;
        protected bool m_Visible;
        private VFXEdDataAnchor m_Input;
        private VFXEdDataAnchor m_Output;

        public VFXEdNodeBlockParameterField(VFXEdDataSource datasource, string name, VFXPropertySlot slot, bool bConnectable, Direction paramDirection, int index)
        {
            m_Name = name;
            m_Slot = slot;

            m_Input = null;
            m_Output = null;

            if(bConnectable)
            {
                switch(paramDirection)
                {
                    case Direction.Input:
                        m_Input = new VFXEdDataAnchor(Vector3.zero, Type, datasource, Direction.Input, index);
                        AddChild(m_Input);
                        break;
                    case Direction.Output:
                        m_Output = new VFXEdDataAnchor(Vector3.zero, Type, datasource, Direction.Output, index);
                        AddChild(m_Output);
                        break;
                    case Direction.Bidirectional:
                        m_Input = new VFXEdDataAnchor(Vector3.zero, Type, datasource, Direction.Input, index);
                        AddChild(m_Input);
                        m_Output = new VFXEdDataAnchor(Vector3.zero, Type, datasource, Direction.Output, index);
                        AddChild(m_Output);
                        break;
                    default:
                        break;
                }
            }

            AddManipulator(new ImguiContainer());
        }

        public VFXEdNodeBlockParameterField(VFXEdDataSource datasource, string name, string tag, VFXPropertySlot value, bool bConnectable, Direction paramDirection, int index) 
            : this(datasource,name,value,bConnectable,paramDirection,index)
        {
            m_Tag = tag;
        }

        public override bool DispatchEvents(Event evt, Canvas2D parent)
        {
            if (!collapsed)
                return base.DispatchEvents(evt, parent);
            else
                return false;
        }

        public override void Layout()
        {
            base.Layout();

            if (!collapsed) {
               scale = new Vector2(parent.scale.x, VFXEditorMetrics.NodeBlockParameterHeight);
               if(m_Output != null) {
                    m_Output.translation = new Vector2(scale.x - VFXEditorMetrics.DataAnchorSize.x, m_Output.translation.y);
                }
            }

            else
                scale = Vector2.zero;
        }

        public bool IsConnected()
        {
            return Value.CurrentValueRef != Value;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            EventType t = Event.current.type;
            if(!collapsed)
            {
                Rect r = GetDrawableRect();

                Rect fieldrect = VFXEditorMetrics.ParameterFieldRectOffset.Remove(r);
                Rect labelrect = new Rect(fieldrect.x, fieldrect.y, VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.height);
                Rect editrect = new Rect(fieldrect.x +
                VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.y, fieldrect.width - VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.height);


                if (IsConnected())
                    GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.25f);

                EditorGUI.LabelField(labelrect, m_Name);

                switch (Type)
                {
                    case VFXValueType.kFloat:
                        m_Slot.SetValue(EditorGUI.FloatField(editrect, "", m_Slot.GetValue<float>()));
                        break;

                    case VFXValueType.kFloat2:
                        m_Slot.SetValue(EditorGUI.Vector2Field(editrect, "", m_Slot.GetValue<Vector2>()));
                        break;

                    case VFXValueType.kFloat3:
                        m_Slot.SetValue(EditorGUI.Vector3Field(editrect, "", m_Slot.GetValue<Vector3>()));
                        break;

                    case VFXValueType.kFloat4:
                        m_Slot.SetValue(EditorGUI.Vector4Field(editrect, "", m_Slot.GetValue<Vector4>()));
                        break;

                    case VFXValueType.kInt:
                        m_Slot.SetValue(EditorGUI.IntField(editrect, "", m_Slot.GetValue<int>()));
                        break;

                    case VFXValueType.kUint:
                        m_Slot.SetValue<uint>((uint)EditorGUI.IntField(editrect, "", (int)m_Slot.GetValue<uint>()));
                        break;

                    case VFXValueType.kTexture2D:
                        m_Slot.SetValue<Texture2D>((Texture2D)EditorGUI.ObjectField(editrect, m_Slot.GetValue<Texture2D>(), typeof(Texture2D)));
                        break;
                    case VFXValueType.kTexture3D:
                        m_Slot.SetValue<Texture3D>((Texture3D)EditorGUI.ObjectField(editrect, m_Slot.GetValue<Texture3D>(), typeof(Texture3D)));
                        break;
                    default: // TODO Texture
                        GUI.Label(editrect, VFXValue.TypeToName(Type) + " " + "", VFXEditor.styles.NodeBlockParameter);
                        break;
                }

                GUI.color = Color.white;
            }
        }
    }
}
