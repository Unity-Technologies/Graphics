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
