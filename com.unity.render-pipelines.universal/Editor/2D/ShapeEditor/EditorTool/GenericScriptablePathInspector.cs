using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal class GenericScriptablePathInspector<U, T> : ScriptablePathInspector where U : ScriptableData<T>
    {
        private List<U> m_DataObjects = new List<U>();
        private List<U> m_SelectedDataObjects = new List<U>();
        private Editor m_CachedEditor = null;

        private void OnEnable()
        {
            PrepareDataObjects();
        }

        private void OnDestroy()
        {
            DestroyDataObjects();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DoCustomDataInspector();
        }

        protected void DoCustomDataInspector()
        {
            PrepareDataObjects();

            if (m_SelectedDataObjects.Count > 0)
            {
                CreateCachedEditor(m_SelectedDataObjects.ToArray(), null, ref m_CachedEditor);

                EditorGUI.BeginChangeCheck();

                m_CachedEditor.OnInspectorGUI();

                if (EditorGUI.EndChangeCheck())
                    SetDataObjects();
            }
        }

        private void PrepareDataObjects()
        {
            var elementCount = 0;

            m_SelectedDataObjects.Clear();

            foreach (var path in paths)
                elementCount += path.pointCount;

            while (m_DataObjects.Count < elementCount)
                CreateDataObject();

            var index = 0;
            foreach (var path in paths)
            {
                var genericPath = path as GenericScriptablePath<T>;
                var customDataArray = genericPath.data;
                var length = customDataArray.Length;

                for (var i = 0; i < length; ++i)
                {
                    var dataObject = m_DataObjects[index + i];
                    dataObject.data = customDataArray[i];

                    if (path.selection.Contains(i))
                    {
                        dataObject.owner = path.owner;
                        dataObject.index = i;
                        m_SelectedDataObjects.Add(dataObject);
                    }
                }

                index += length;
            }
        }

        private void SetDataObjects()
        {
            var index = 0;
            foreach (var path in paths)
            {
                var genericPath = path as GenericScriptablePath<T>;
                var customDataArray = genericPath.data;
                var length = customDataArray.Length;

                for (var i = 0; i < length; ++i)
                    customDataArray[i] = m_DataObjects[index + i].data;

                genericPath.data = customDataArray;

                index += length;
            }
        }

        private U CreateDataObject()
        {
            var dataObject = ScriptableObject.CreateInstance<U>();
            m_DataObjects.Add(dataObject);
            return dataObject;
        }

        private void DestroyDataObjects()
        {
            foreach (var customDataObject in m_DataObjects)
                DestroyImmediate(customDataObject);

            m_DataObjects.Clear();
            m_SelectedDataObjects.Clear();
        }
    }
}
