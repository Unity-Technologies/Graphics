using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable StringLiteralTypo

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    class SearcherExampleHostWindow : EditorWindow
    {
        [NonSerialized]
        List<SearcherItem> m_SearcherItems;

        VisualElement m_DummyVisualElement;

        [MenuItem("Searcher/Searcher Example Host Window")]
        public static void ShowAllInOne()
        {
            GetWindow<SearcherExampleHostWindow>();
        }

        public void OnEnable()
        {
            m_SearcherItems = new List<SearcherItem>(
                SearcherExamplesData.BookItems
                    .Concat(SearcherExamplesData.FoodItems)
                    .Concat(SearcherExamplesData.WeirdItems));

            m_DummyVisualElement = new Label { text = "Click here" };
            m_DummyVisualElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_DummyVisualElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1.0f);
            m_DummyVisualElement.StretchToParentSize();
            rootVisualElement.Add(m_DummyVisualElement);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.RegisterCallback<MouseDownEvent>(OnMouseDown);
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            // Focus required for KeyDownEvent to be dispatched.
            // We're using the KeyDownEvent to display the Searcher when pressing "Space".
            m_DummyVisualElement.focusable = true;
            m_DummyVisualElement.Focus();
        }

        void OnDisable()
        {
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Space)
                SearcherWindow.Show(this, m_SearcherItems, "OnKeyDown", item =>
                {
                    Debug.Log("Searcher item selected: " + (item?.Name ?? "<none>"));
                    return true;
                }, evt.originalMousePosition);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            SearcherWindow.Show(this, m_SearcherItems, "OnMouseDown", item =>
            {
                Debug.Log("Searcher item selected: " + (item?.Name ?? "<none>"));
                return true;
            }, evt.mousePosition);
        }
    }
}
