using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    internal class SortingLayerDropDown
    {
        private class LayerSelectionData
        {
            public SerializedObject serializedObject;
            public Object[]         targets;
            public int              layerID;
            public System.Action<SerializedObject> onSelectionChanged;

            public LayerSelectionData(SerializedObject so, int lid, Object[] tgts, System.Action<SerializedObject> selectionChangedCallback)
            {
                serializedObject = so;
                layerID = lid;
                targets = tgts;
                onSelectionChanged = selectionChangedCallback;
            }
        }

        private static class Styles
        {
            public static GUIContent sortingLayerAll = EditorGUIUtility.TrTextContent("All");
            public static GUIContent sortingLayerNone = EditorGUIUtility.TrTextContent("None");
            public static GUIContent sortingLayerMixed = EditorGUIUtility.TrTextContent("Mixed...");
        }

        SortingLayer[] m_AllSortingLayers;
        GUIContent[] m_AllSortingLayerNames;
        List<int> m_ApplyToSortingLayersList;
        SerializedProperty m_ApplyToSortingLayers;

        public void OnEnable(SerializedObject serializedObject, string propertyName)
        {
            m_ApplyToSortingLayers = serializedObject.FindProperty(propertyName);
            m_ApplyToSortingLayersList = new List<int>(m_ApplyToSortingLayers.arraySize);

            m_AllSortingLayers = SortingLayer.layers;
            m_AllSortingLayerNames = m_AllSortingLayers.Select(x => new GUIContent(x.name)).ToArray();
        }

        void UpdateApplyToSortingLayersArray(object layerSelectionDataObject)
        {
            LayerSelectionData layerSelectionData = (LayerSelectionData)layerSelectionDataObject; 

            m_ApplyToSortingLayers.ClearArray();
            for (int i = 0; i < m_ApplyToSortingLayersList.Count; ++i)
            {
                m_ApplyToSortingLayers.InsertArrayElementAtIndex(i);
                m_ApplyToSortingLayers.GetArrayElementAtIndex(i).intValue = m_ApplyToSortingLayersList[i];
            }

            if (layerSelectionData.onSelectionChanged != null)
                layerSelectionData.onSelectionChanged(layerSelectionData.serializedObject);
            
            layerSelectionData.serializedObject.ApplyModifiedProperties();

            if (layerSelectionData.targets is Light2D[])
            {
                foreach (Light2D light in layerSelectionData.targets)
                {
                    if (light != null && light.lightType == Light2D.LightType.Global)
                        Light2DManager.ErrorIfDuplicateGlobalLight(light);
                }
            }
        }

        void OnNoSortingLayerSelected(object selectionData)
        {
            m_ApplyToSortingLayersList.Clear();
            UpdateApplyToSortingLayersArray(selectionData);
        }

        void OnAllSortingLayersSelected(object selectionData)
        {
            m_ApplyToSortingLayersList.Clear();
            m_ApplyToSortingLayersList.AddRange(m_AllSortingLayers.Select(x => x.id));
            UpdateApplyToSortingLayersArray(selectionData);
        }

        void OnSortingLayerSelected(object layerSelectionDataObject)
        {

            LayerSelectionData layerSelectionData = (LayerSelectionData)layerSelectionDataObject;

            int layerID = (int)layerSelectionData.layerID;

            if (m_ApplyToSortingLayersList.Contains(layerID))
                m_ApplyToSortingLayersList.RemoveAll(id => id == layerID);
            else
                m_ApplyToSortingLayersList.Add(layerID);

            UpdateApplyToSortingLayersArray(layerSelectionDataObject);
        }

        public void OnTargetSortingLayers(SerializedObject serializedObject, Object[] targets, GUIContent labelContent, System.Action<SerializedObject> selectionChangedCallback)
        {
            Rect totalPosition = EditorGUILayout.GetControlRect();
            GUIContent actualLabel = EditorGUI.BeginProperty(totalPosition, labelContent, m_ApplyToSortingLayers);
            Rect position = EditorGUI.PrefixLabel(totalPosition, actualLabel);

            m_ApplyToSortingLayersList.Clear();
            int applyToSortingLayersSize = m_ApplyToSortingLayers.arraySize;
            for (int i = 0; i < applyToSortingLayersSize; ++i)
            {
                int layerID = m_ApplyToSortingLayers.GetArrayElementAtIndex(i).intValue;
                if (SortingLayer.IsValid(layerID))
                    m_ApplyToSortingLayersList.Add(layerID);
            }

            GUIContent selectedLayers;
            if (m_ApplyToSortingLayersList.Count == 1)
                selectedLayers = new GUIContent(SortingLayer.IDToName(m_ApplyToSortingLayersList[0]));
            else if (m_ApplyToSortingLayersList.Count == m_AllSortingLayers.Length)
                selectedLayers = Styles.sortingLayerAll;
            else if (m_ApplyToSortingLayersList.Count == 0)
                selectedLayers = Styles.sortingLayerNone;
            else
                selectedLayers = Styles.sortingLayerMixed;

            if (EditorGUI.DropdownButton(position, selectedLayers, FocusType.Keyboard, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                menu.allowDuplicateNames = true;

                LayerSelectionData layerSelectionData = new LayerSelectionData(serializedObject, 0, targets, selectionChangedCallback);
                menu.AddItem(Styles.sortingLayerNone, m_ApplyToSortingLayersList.Count == 0, OnNoSortingLayerSelected, layerSelectionData);
                menu.AddItem(Styles.sortingLayerAll, m_ApplyToSortingLayersList.Count == m_AllSortingLayers.Length, OnAllSortingLayersSelected, layerSelectionData);
                menu.AddSeparator("");

                for (int i = 0; i < m_AllSortingLayers.Length; ++i)
                {
                    var sortingLayer = m_AllSortingLayers[i];
                    layerSelectionData = new LayerSelectionData(serializedObject, sortingLayer.id, targets, selectionChangedCallback);
                    menu.AddItem(m_AllSortingLayerNames[i], m_ApplyToSortingLayersList.Contains(sortingLayer.id), OnSortingLayerSelected, layerSelectionData);
                }

                menu.DropDown(position);
            }

            EditorGUI.EndProperty();
        }
    }
}
