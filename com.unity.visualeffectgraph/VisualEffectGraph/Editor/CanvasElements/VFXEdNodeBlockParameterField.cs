using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNodeBlockParameterField : CanvasElement
    {
        public VFXParamValue Value { get { return m_Value; } }
        public bool Visible { get { return m_Visible; } set { m_Visible = value; } }
        public string Name { get { return m_Name; } }
        public VFXParam.Type Type { get { return m_Value.ValueType; } }
        public VFXEdDataAnchor Input { get { return m_Input; } }
        public VFXEdDataAnchor Output { get { return m_Output; } }

        protected string m_Name;
        protected VFXParamValue m_Value;
        protected bool m_Visible;
        private VFXEdDataAnchor m_Input;
        private VFXEdDataAnchor m_Output;

        public VFXEdNodeBlockParameterField(VFXEdDataSource datasource, string name, VFXParamValue value, bool bConnectable, Direction paramDirection, int index)
        {
            m_Name = name;
            m_Value = value;

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
               scale = new Vector2(parent.scale.x, GetParamHeight(Type));
               if(m_Output != null) {
                    m_Output.translation = new Vector2(scale.x - VFXEditorMetrics.DataAnchorSize.x, m_Output.translation.y);
                }
            }

            else
                scale = Vector2.zero;
        }

        protected static float GetParamHeight(VFXParam.Type type)
        {

            float height = VFXEditorMetrics.NodeBlockParameterHeight;
            /*switch (type)
            {
                case VFXParam.Type.kTypeFloat2:
                case VFXParam.Type.kTypeFloat3:
                case VFXParam.Type.kTypeFloat4:
                    height += VFXEditorMetrics.NodeBlockAdditionalHeight;
                    break;
                default:
                    break;
            }*/
            return height;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            if(!collapsed)
            {
                Rect r = GetDrawableRect();

                Rect fieldrect = VFXEditorMetrics.ParameterFieldRectOffset.Remove(r);
                Rect labelrect = new Rect(fieldrect.x, fieldrect.y, VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.height);
                Rect editrect = new Rect(fieldrect.x +
                VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.y, fieldrect.width - VFXEditorMetrics.ParameterFieldLabelWidth, fieldrect.height);

                EditorGUI.LabelField(labelrect, m_Name);

                switch (Type)
                {
                    case VFXParam.Type.kTypeFloat:
                        m_Value.SetValue(EditorGUI.FloatField(editrect, "", m_Value.GetValue<float>()));
                        break;

                    case VFXParam.Type.kTypeFloat2:
                        m_Value.SetValue(EditorGUI.Vector2Field(editrect, "", m_Value.GetValue<Vector2>()));
                        break;

                    case VFXParam.Type.kTypeFloat3:
                        m_Value.SetValue(EditorGUI.Vector3Field(editrect, "", m_Value.GetValue<Vector3>()));
                        break;

                    case VFXParam.Type.kTypeFloat4:
                        m_Value.SetValue(EditorGUI.Vector4Field(editrect, "", m_Value.GetValue<Vector4>()));
                        break;

                    case VFXParam.Type.kTypeInt:
                        m_Value.SetValue(EditorGUI.IntField(editrect, "", m_Value.GetValue<int>()));
                        break;

                    case VFXParam.Type.kTypeUint:
                        m_Value.SetValue<uint>((uint)EditorGUI.IntField(editrect, "", (int)m_Value.GetValue<uint>()));
                        break;

                    default: // TODO Texture
                        GUI.Label(editrect, VFXParam.GetNameFromType(Type) + " " + "", VFXEditor.styles.NodeBlockParameter);
                        break;
                }
            }
        }
    }
}
