using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEditor.VFX
{
    [CustomEditor(typeof(VFXGraphAsset))]
    public class VFXGraphAssetEditor : Editor
    {
        private void ProcessJSonTokenGUI(JToken node, string parentPath, string currentName)
        {
            if (node.Type == JTokenType.Object || node.Type == JTokenType.Array)
            {
                //Object type
                var currentPath = string.Format("{0}.{1}", parentPath, currentName);
                if (!m_foldoutState.ContainsKey(currentPath))
                {
                    m_foldoutState.Add(currentPath, true);
                }
                var oldFoldoutState = m_foldoutState[currentPath];
                var newFoldoutState = EditorGUILayout.Foldout(oldFoldoutState, currentName);

                if (newFoldoutState != oldFoldoutState)
                {
                    m_foldoutState[currentPath] = newFoldoutState;
                    Repaint();
                }

                if (newFoldoutState)
                {
                    EditorGUI.indentLevel++;
                    if (node.Type == JTokenType.Object)
                    {
                        foreach (var property in node.Children<JProperty>())
                        {
                            if (property.Name == "JSONnodeData") //deeper serialize *inception*
                            {
                                var innerJson = property.Value.Value<string>();
                                var innerToken = JToken.Parse(innerJson);
                                ProcessJSonTokenGUI(innerToken, currentPath, property.Name);
                                var newInnerJson = JsonConvert.SerializeObject(innerToken);
                                if (innerJson != newInnerJson)
                                {
                                    property.Value.Replace(newInnerJson);
                                }
                            }
                            else
                            {
                                ProcessJSonTokenGUI(property.Value, currentPath, property.Name);
                            }
                        }
                    }
                    else if (node.Type == JTokenType.Array)
                    {
                        var children = node.Children().ToArray();
                        for (int iChild = 0; iChild < children.Length; ++iChild)
                        {
                            ProcessJSonTokenGUI(children[iChild], currentPath, string.Format("{0}[{1}]", currentName, iChild));
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                //Concrete type
                if (node.Type == JTokenType.Float)
                {
                    var oldFloat = node.Value<float>();
                    var newFloat = EditorGUILayout.FloatField(currentName, oldFloat);
                    if (Mathf.Abs(oldFloat - newFloat) > float.Epsilon)
                    {
                        node.Replace(newFloat);
                    }
                }
                else if (node.Type == JTokenType.Integer)
                {
                    var oldInt = node.Value<int>();
                    var newInt = EditorGUILayout.IntField(currentName, oldInt);
                    if (oldInt != newInt)
                    {
                        node.Replace(newInt);
                    }
                }
                else if (node.Type == JTokenType.Boolean)
                {
                    var oldToggle = node.Value<bool>();
                    var newToggle = EditorGUILayout.Toggle(currentName, oldToggle);
                    if (oldToggle != newToggle)
                    {
                        node.Replace(newToggle);
                    }
                }
                else if (node.Type == JTokenType.String)
                {
                    if (currentName != "assemblyName")
                    {
                        var oldString = node.Value<string>();
                        var newString = EditorGUILayout.TextField(currentName, oldString);
                        if (oldString != newString)
                        {
                            node.Replace(newString);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(currentName, node.ToString());
                }
            }
        }

        static Dictionary<string, bool> m_foldoutState = new Dictionary<string, bool>(); //share foldout state between asset, ease comparison
        static bool m_debugMode = false;
        static bool m_readOnly = true;
        static Vector2 m_scrollViewPosition;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            GUILayout.BeginHorizontal();
            var newDebugMode = EditorGUILayout.Toggle("Debug", m_debugMode);
            if (m_debugMode != newDebugMode)
            {
                m_debugMode = newDebugMode;
                Repaint();
            }
            EditorGUI.BeginDisabledGroup(!m_debugMode);
            var newReadOnly = EditorGUILayout.Toggle("ReadOnly", m_readOnly);
            if (m_readOnly != newReadOnly)
            {
                m_readOnly = newReadOnly;
                Repaint();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            if (m_debugMode)
            {
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Collapse all"))
                {
                    m_foldoutState = m_foldoutState.ToDictionary(e => e.Key, e => false);
                    Repaint();
                }
                if (GUILayout.Button("Expand all"))
                {
                    m_foldoutState = new Dictionary<string, bool>();
                    Repaint();
                }

                GUILayout.EndHorizontal();
                var jsonDataProperties = serializedObject.FindProperty("m_Root.m_SerializableChildren");

                var newScrollViewPosition = GUILayout.BeginScrollView(m_scrollViewPosition);
                for (int i = 0; i < jsonDataProperties.arraySize; ++i)
                {
                    var jsonDataPropertyRoot = jsonDataProperties.GetArrayElementAtIndex(i);
                    var jsonNodeData = jsonDataPropertyRoot.FindPropertyRelative("JSONnodeData");
                    var oldJson = jsonNodeData.stringValue;

                    var token = JToken.Parse(oldJson);
                    if (newScrollViewPosition != m_scrollViewPosition)
                    {
                        m_scrollViewPosition = newScrollViewPosition;
                        Repaint();
                    }

                    EditorGUI.BeginChangeCheck();
                    ProcessJSonTokenGUI(token, "", string.Format("m_SerializableChildren[{0}]", i));
                    if (!m_readOnly && EditorGUI.EndChangeCheck())
                    {
                        jsonNodeData.stringValue = JsonConvert.SerializeObject(token);
                    }
                }
                GUILayout.EndScrollView();
            }
            if (serializedObject.ApplyModifiedProperties())
                ((VFXGraphAsset)target).root.Invalidate(VFXModel.InvalidationCause.kStructureChanged);

        }
    }
}