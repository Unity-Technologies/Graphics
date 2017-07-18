using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using Object = UnityEngine.Object;
using UnityEditorInternal;

[CustomEditor(typeof(VFXAsset))]
public class VFXAssetEditor : Editor
{
    VFXViewPresenter m_ViewPresenter;
    void OnEnable()
    {
        VFXAsset asset = (VFXAsset)target;
        m_ViewPresenter = ScriptableObject.CreateInstance<VFXViewPresenter>();
        m_ViewPresenter.SetVFXAsset(asset, false);
        m_AdvDictionary.Clear();
    }

    void OnDisable()
    {
    }

    public void OnSceneGUI()
    {
    }

    VFXParameterPresenter[] m_ExposedList;
    VFXParameterPresenter[] m_ExposedListCopy;

    public override void OnInspectorGUI()
    {

        m_ExposedListCopy = (VFXParameterPresenter[])m_ExposedList.Clone();
        if ( list == null)
        {
            m_ExposedList = m_ViewPresenter.allChildren.OfType<VFXParameterPresenter>().Where(t => t.exposed).OrderBy(t => t.order).ToArray();
            list = new ReorderableList(m_ExposedList,typeof(VFXParameterPresenter),true,false,false,false);
            list.elementHeightCallback = GetExposedListElementHeight;
            list.drawElementCallback = DrawExposedListElement;
            list.drawHeaderCallback = DrawExposedHeader;
        }

        list.DoLayoutList();
        
        for (int i = 0; i < m_ExposedList.Length; ++i)
        {
            if( m_ExposedList[i].order != i )
            {
                var parameter = m_ExposedList[i];
                Undo.RegisterCompleteObjectUndo(parameter, "VFX parameter");
                parameter.order = i;
                EditorUtility.SetDirty(parameter);
            }
        }
    }

    public void DrawExposedHeader(Rect rect)
    {
        GUI.Label(rect,"Exposed Parameters");
    }

    private float GetExposedListElementHeight(int index)
    {
        ParamInfo infos;
        m_AdvDictionary.TryGetValue(m_ExposedList[index], out infos); 

        return infos.adv ? 80 : 24;
    }
    private void DrawExposedListElement(Rect rect, int index, bool selected, bool focused)
    {
        OnParamGUI(rect,m_ExposedList[index], index);
    }

    struct ParamInfo
    {
        public bool adv;
        public VFXPropertyIM propertyIM;
    }


    ReorderableList list;
    Dictionary<VFXParameterPresenter, ParamInfo> m_AdvDictionary = new Dictionary<VFXParameterPresenter, ParamInfo>();

    void OnParamGUI(Rect rect,VFXParameterPresenter parameter, int order)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();

        bool orderChange = parameter.order != order;
        if (orderChange)
        {
            parameter.order = order;
        }
        ParamInfo infos;
        m_AdvDictionary.TryGetValue(parameter, out infos);


        rect.xMin += 8;
        Rect toggleRect = rect;
        

        int labelWidth = 130;
        int toggleWidth = 20;
        int offsetWidth = 20;
        int lineHeight = 15;
        toggleRect.width = labelWidth + toggleWidth;
        toggleRect.height = lineHeight;

        infos.adv = EditorGUI.Foldout(toggleRect, infos.adv, string.Format("{0} : name ({1})", parameter.order + 1, parameter.anchorType.UserFriendlyName()));

        Rect fieldRect = rect;

        fieldRect.xMin += labelWidth + toggleWidth + offsetWidth;
        fieldRect.height = lineHeight;

        parameter.exposedName = EditorGUI.TextField(fieldRect, parameter.exposedName);

        if (orderChange || EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo(parameter, "VFX parameter");
            EditorUtility.SetDirty(parameter);
        }
        GUILayout.EndHorizontal();
        if (infos.adv)
        {
            if (infos.propertyIM == null)
            {
                infos.propertyIM = VFXPropertyIM.Create(parameter.anchorType, labelWidth);
            }

            if (infos.propertyIM != null)
            {
                float marginHeight = 2;
                Rect areaRect = rect;
                //if (Event.current.type == EventType.Repaint)
                {
                    areaRect.yMin += lineHeight + marginHeight;

                    areaRect.xMin += offsetWidth;
                    areaRect.height = lineHeight;
                    areaRect.xMin += toggleWidth;
                    parameter.value = infos.propertyIM.OnGUI(areaRect,"value", parameter.value);

                    areaRect.xMin -= offsetWidth;

                    areaRect.yMin += lineHeight + marginHeight;
                    areaRect.height = lineHeight;
                    if (infos.propertyIM.isNumeric)
                    {
                        toggleRect = areaRect;
                        toggleRect.width = toggleWidth;
                        bool minChecked = GUI.Toggle(toggleRect,parameter.minValue != null, "");
                        GUI.enabled = minChecked;
                        if (infos.propertyIM != null)
                        {
                            object val = parameter.minValue;
                            if (val == null)
                                val = System.Activator.CreateInstance(parameter.anchorType);

                            toggleRect.xMin = toggleRect.xMax;
                            toggleRect.xMax = areaRect.xMax;
                            val = infos.propertyIM.OnGUI(toggleRect, "min", val);
                            if (minChecked)
                                parameter.minValue = val;
                            else
                                parameter.minValue = null;
                        }
                        areaRect.yMin += lineHeight + marginHeight;
                        areaRect.height = lineHeight;
                        toggleRect = areaRect;
                        toggleRect.width = toggleWidth;
                        GUI.enabled = true;
                        bool maxChecked = GUI.Toggle(toggleRect, parameter.maxValue != null, "");
                        GUI.enabled = maxChecked;
                        if (infos.propertyIM != null)
                        {
                            object val = parameter.maxValue;
                            if (val == null)
                                val = System.Activator.CreateInstance(parameter.anchorType);

                            toggleRect.xMin = toggleRect.xMax;
                            toggleRect.xMax = areaRect.xMax;
                            val = infos.propertyIM.OnGUI(toggleRect, "max", val);
                            if (maxChecked)
                                parameter.maxValue = val;
                            else
                                parameter.maxValue = null;
                        }

                        /*
                        GUI.enabled = true;
                        bool maxChecked = GUILayout.Toggle(parameter.maxValue != null, "");
                        GUI.enabled = maxChecked;
                        if (infos.propertyIM != null)
                        {
                            object val = parameter.maxValue;
                            if (val == null)
                                val = System.Activator.CreateInstance(parameter.anchorType);
                            val = infos.propertyIM.OnGUI("max", val);
                            if (maxChecked)
                                parameter.maxValue = val;
                            else
                                parameter.maxValue = null;
                        }
                        */
                        GUI.enabled = true;
                    }
                }
            }
        }
        m_AdvDictionary[parameter] = infos;

        GUILayout.EndVertical();
    }
}
