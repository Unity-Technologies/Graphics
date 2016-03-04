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
        public VFXParamValue Value { get { return m_ParamValue; } }
        public bool Visible { get { return m_Visible; } set { m_Visible = value; } }
        public string Name { get { return m_Name; } }
        public string Tag {get { return m_Tag; } }
        public VFXParam.Type Type { get { return m_ParamValue.ValueType; } }
        public VFXEdDataAnchor Input { get { return m_Input; } }
        public VFXEdDataAnchor Output { get { return m_Output; } }

        protected string m_Name;
        protected string m_Tag = "";
        protected VFXParamValue m_ParamValue;
        protected bool m_Visible;
        private VFXEdDataAnchor m_Input;
        private VFXEdDataAnchor m_Output;

        public VFXEdNodeBlockParameterField(VFXEdDataSource datasource, string name, VFXParamValue value, bool bConnectable, Direction paramDirection, int index)
        {
            m_Name = name;
            m_ParamValue = value;

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

        public VFXEdNodeBlockParameterField(VFXEdDataSource datasource, string name, string tag, VFXParamValue value, bool bConnectable, Direction paramDirection, int index) 
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
               scale = new Vector2(parent.scale.x, GetParamHeight(Type));
               if(m_Output != null) {
                    m_Output.translation = new Vector2(scale.x - VFXEditorMetrics.DataAnchorSize.x, m_Output.translation.y);
                }
            }

            else
                scale = Vector2.zero;
        }

        public bool IsConnected()
        {
            return !Value.IsBound();
        }

        protected static float GetParamHeight(VFXParam.Type type)
        {

            float height = VFXEditorMetrics.NodeBlockParameterHeight;
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


                if (IsConnected())
                    GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.25f);

                EditorGUI.LabelField(labelrect, m_Name);

                switch (Type)
                {
                    case VFXParam.Type.kTypeFloat:
                        m_ParamValue.SetValue(EditorGUI.FloatField(editrect, "", m_ParamValue.GetValue<float>()));
                        break;

                    case VFXParam.Type.kTypeFloat2:
                        m_ParamValue.SetValue(EditorGUI.Vector2Field(editrect, "", m_ParamValue.GetValue<Vector2>()));
                        break;

                    case VFXParam.Type.kTypeFloat3:
                        m_ParamValue.SetValue(EditorGUI.Vector3Field(editrect, "", m_ParamValue.GetValue<Vector3>()));
                        break;

                    case VFXParam.Type.kTypeFloat4:
                        m_ParamValue.SetValue(EditorGUI.Vector4Field(editrect, "", m_ParamValue.GetValue<Vector4>()));
                        break;

                    case VFXParam.Type.kTypeInt:
                        m_ParamValue.SetValue(EditorGUI.IntField(editrect, "", m_ParamValue.GetValue<int>()));
                        break;

                    case VFXParam.Type.kTypeUint:
                        m_ParamValue.SetValue<uint>((uint)EditorGUI.IntField(editrect, "", (int)m_ParamValue.GetValue<uint>()));
                        break;

                    case VFXParam.Type.kTypeTexture2D:
                        m_ParamValue.SetValue<Texture2D>((Texture2D)EditorGUI.ObjectField(editrect, m_ParamValue.GetValue<Texture2D>(),typeof(Texture2D)));
                        break;
                    case VFXParam.Type.kTypeTexture3D:
                        m_ParamValue.SetValue<Texture3D>((Texture3D)EditorGUI.ObjectField(editrect, m_ParamValue.GetValue<Texture3D>(),typeof(Texture3D)));
                        break;
                    default: // TODO Texture
                        GUI.Label(editrect, VFXParam.GetNameFromType(Type) + " " + "", VFXEditor.styles.NodeBlockParameter);
                        break;
                }

                GUI.color = Color.white;
            }
        }
    }
}
