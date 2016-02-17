using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdProcessingNodeBlock : VFXEdNodeBlock
    {

        public VFXBlockModel Model
        {
            get { return m_Model; }
        }
        private VFXBlockModel m_Model;
        private VFXParamValue[] m_Params;

        public VFXEdProcessingNodeBlock(VFXBlock block, VFXEdDataSource dataSource) : base(dataSource)
        {
            m_Model = new VFXBlockModel(block);
            m_Params = new VFXParamValue[block.m_Params.Length];
            for (int i = 0; i < m_Params.Length; ++i)
            {
                m_Params[i] = VFXParamValue.Create(block.m_Params[i].m_Type);
                m_Model.BindParam(m_Params[i], i);
            }
            m_Name = block.m_Name;

            AddChild(new VFXEdNodeBlockHeader(dataSource, m_Name, VFXEditor.styles.GetIcon(block.m_IconPath == "" ? "Default" : block.m_IconPath), block.m_Params.Length > 0 ? true : false));
            AddManipulator(new ImguiContainer());

            Layout();
        }

        public override void OnRemoved()
        {
            // process model
            Model.Detach();
        }

        // Retrieve the height of a given param
        protected override float GetParamHeight(VFXParam param)
        {
            float height = VFXEditorMetrics.NodeBlockParameterHeight;
            switch (param.m_Type)
            {
                case VFXParam.Type.kTypeFloat2:
                case VFXParam.Type.kTypeFloat3:
                case VFXParam.Type.kTypeFloat4:
                    height += VFXEditorMetrics.NodeBlockAdditionalHeight;
                    break;

                default:
                    break;
            }
            return height;
        }

        // Retrieve the total height of a block
        protected override float GetHeight()
        {
            float height = VFXEditorMetrics.NodeBlockHeaderHeight;
            for (int i = 0; i < m_Model.Desc.m_Params.Length; ++i)
                height += GetParamHeight(m_Model.Desc.m_Params[i]);
            return height;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            Rect r = GetDrawableRect();

            if (!collapsed)
            {
                float currentY = r.y + VFXEditorMetrics.NodeBlockHeaderHeight;
                float posX = VFXEditorMetrics.NodeBlockParameterLabelPosition.x;
                for (int i = 0; i < m_Model.Desc.m_Params.Length; ++i)
                {
                    VFXParam.Type paramType = m_Model.Desc.m_Params[i].m_Type;

                    Rect rect = new Rect(r.x + posX, currentY, r.width - posX, GetParamHeight(m_Model.Desc.m_Params[i]) - 2);
                    GUI.Box(new Rect(r.x, currentY, VFXEditorMetrics.DataAnchorSize.x, VFXEditorMetrics.DataAnchorSize.y), "", VFXEditor.styles.ConnectorLeft);
                    currentY += rect.height;

                    switch (paramType)
                    {
                        case VFXParam.Type.kTypeFloat:
                            m_Params[i].SetValue(EditorGUI.FloatField(rect, m_Model.Desc.m_Params[i].m_Name, m_Params[i].GetValue<float>()));
                            break;

                        case VFXParam.Type.kTypeFloat2:
                            m_Params[i].SetValue(EditorGUI.Vector2Field(rect, m_Model.Desc.m_Params[i].m_Name, m_Params[i].GetValue<Vector2>()));
                            break;

                        case VFXParam.Type.kTypeFloat3:
                            m_Params[i].SetValue(EditorGUI.Vector3Field(rect, m_Model.Desc.m_Params[i].m_Name, m_Params[i].GetValue<Vector3>()));
                            break;

                        case VFXParam.Type.kTypeFloat4:
                            m_Params[i].SetValue(EditorGUI.Vector4Field(rect, m_Model.Desc.m_Params[i].m_Name, m_Params[i].GetValue<Vector4>()));
                            break;

                        case VFXParam.Type.kTypeInt:
                            m_Params[i].SetValue(EditorGUI.IntField(rect, m_Model.Desc.m_Params[i].m_Name, m_Params[i].GetValue<int>()));
                            break;

                        case VFXParam.Type.kTypeUint:
                            m_Params[i].SetValue<uint>((uint)EditorGUI.IntField(rect, m_Model.Desc.m_Params[i].m_Name, (int)m_Params[i].GetValue<uint>()));
                            break;

                        default: // TODO Texture
                            GUI.Label(rect, VFXParam.GetNameFromType(paramType) + " " + m_Model.Desc.m_Params[i].m_Name, VFXEditor.styles.NodeBlockParameter);
                            break;
                    }

                }
            }
        }

    }
}
