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

    public override void OnInspectorGUI()
    {
        

        GUILayout.BeginVertical();
        GUILayout.Label("Exposed Parameters",GUI.skin.box,GUILayout.ExpandWidth(true));
        int cpt = 0;
        foreach ( var parm in m_ViewPresenter.allChildren.OfType<VFXParameterPresenter>().Where(t=>t.exposed).OrderBy(t=>t.order) )
        {
            OnParamGUI(parm,cpt++);
        }
        GUILayout.Label("Local Parameters", GUI.skin.box, GUILayout.ExpandWidth(true));
        cpt = 0;
        foreach (var parm in m_ViewPresenter.allChildren.OfType<VFXParameterPresenter>().Where(t => !t.exposed).OrderBy(t => t.order))
        {
            OnParamGUI(parm,cpt++);
        }
    }
    struct ParamInfo
    {
        public bool adv;
        public VFXPropertyIM propertyIM;
    }

    Dictionary<VFXParameterPresenter, ParamInfo> m_AdvDictionary = new Dictionary<VFXParameterPresenter, ParamInfo>();

    void OnParamGUI(VFXParameterPresenter parameter,int order)
    {
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();

        bool orderChange = parameter.order != order;
        if( orderChange)
        {
            parameter.order = order;
        }
        ParamInfo infos;
        m_AdvDictionary.TryGetValue(parameter, out infos);

        infos.adv = EditorGUILayout.ToggleLeft(string.Format("{0} : name ({1})", parameter.order +1,parameter.anchorType.UserFriendlyName()),infos.adv,GUILayout.Width(140));

        parameter.exposedName = EditorGUILayout.TextField(parameter.exposedName);

        if (orderChange || EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo(parameter, "VFX parameter");
            EditorUtility.SetDirty(parameter);
        }
        GUILayout.EndHorizontal();
        if (infos.adv)
        {
            if(infos.propertyIM == null)
            {
                infos.propertyIM = VFXPropertyIM.Create(parameter.anchorType, 80);
            }

            if (infos.propertyIM != null)
            {
                infos.propertyIM.OnGUI("value",parameter.value);

                if (infos.propertyIM.isNumeric)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    bool minChecked = GUILayout.Toggle(parameter.minValue != null, "");
                    GUI.enabled = minChecked;
                    if (infos.propertyIM != null)
                    {
                        object val = parameter.minValue;
                        if (val == null)
                            val = System.Activator.CreateInstance(parameter.anchorType);
                        val = infos.propertyIM.OnGUI("min", val);
                        if (minChecked)
                            parameter.minValue = val;
                        else
                            parameter.minValue = null;

                    }

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
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
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
        }
        m_AdvDictionary[parameter] = infos;
    }
}
