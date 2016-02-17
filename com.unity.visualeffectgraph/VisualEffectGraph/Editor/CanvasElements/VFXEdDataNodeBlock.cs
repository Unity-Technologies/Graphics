using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataNodeBlock : VFXEdNodeBlock
    {
        VFXDataBlock m_DataBlock;
        VFXParamValue[] m_Params;

        public VFXEdDataNodeBlock(VFXDataBlock datablock) :base()
        {
            m_Name = datablock.name;
            m_DataBlock = datablock;
            m_Params = new VFXParamValue[m_DataBlock.Parameters.Count];

            int i = 0;
            foreach(KeyValuePair<string, VFXParam.Type> kvp in m_DataBlock.Parameters) {
                m_Params[i] = VFXParamValue.Create(kvp.Value);
                i++;
            }

            AddChild(new VFXEdNodeBlockHeader(m_Name, m_DataBlock.icon, datablock.Parameters.Count > 0));
            AddManipulator(new ImguiContainer());


            Layout();
        }

        public override void OnRemoved()
        {
            
        }

        protected override float GetHeight()
        {
            float height = VFXEditorMetrics.NodeBlockHeaderHeight;
            foreach(KeyValuePair<string, VFXParam.Type> kvp in m_DataBlock.Parameters) {
                height += GetParamHeight(kvp.Value);
            }
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

                int i = 0;
                foreach(KeyValuePair<string, VFXParam.Type> kvp in m_DataBlock.Parameters)
                {
                    VFXParam.Type paramType = kvp.Value;

                    Rect rect = new Rect(r.x + posX, currentY, r.width - posX, GetParamHeight(kvp.Value) - 2);
                    GUI.Box(new Rect(r.width + r.x - VFXEditorMetrics.DataAnchorSize.x, currentY, VFXEditorMetrics.DataAnchorSize.x, VFXEditorMetrics.DataAnchorSize.y), "", VFXEditor.styles.ConnectorRight);
                    currentY += rect.height;

                    switch (kvp.Value)
                    {
                        case VFXParam.Type.kTypeFloat:
                            m_Params[i].SetValue(EditorGUI.FloatField(rect, kvp.Key, m_Params[i].GetValue<float>()));
                            break;

                        case VFXParam.Type.kTypeFloat2:
                            m_Params[i].SetValue(EditorGUI.Vector2Field(rect, kvp.Key, m_Params[i].GetValue<Vector2>()));
                            break;

                        case VFXParam.Type.kTypeFloat3:
                            m_Params[i].SetValue(EditorGUI.Vector3Field(rect, kvp.Key, m_Params[i].GetValue<Vector3>()));
                            break;

                        case VFXParam.Type.kTypeFloat4:
                            m_Params[i].SetValue(EditorGUI.Vector4Field(rect, kvp.Key, m_Params[i].GetValue<Vector4>()));
                            break;

                        case VFXParam.Type.kTypeInt:
                            m_Params[i].SetValue(EditorGUI.IntField(rect, kvp.Key, m_Params[i].GetValue<int>()));
                            break;

                        case VFXParam.Type.kTypeUint:
                            m_Params[i].SetValue<uint>((uint)EditorGUI.IntField(rect, kvp.Key, (int)m_Params[i].GetValue<uint>()));
                            break;

                        default: // TODO Texture
                            GUI.Label(rect, VFXParam.GetNameFromType(paramType) + " " + kvp.Key, VFXEditor.styles.NodeBlockParameter);
                            break;
                    }
                    i++;
                }
            }
        }
    }
}
