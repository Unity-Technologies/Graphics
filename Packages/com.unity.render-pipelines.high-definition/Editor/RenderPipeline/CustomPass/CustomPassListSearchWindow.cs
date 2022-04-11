using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class CustomPassListSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        Texture2D icon;
        Action<Type> createCustomPassCallback;

        public void Initialize(Action<Type> createCustomPassCallback)
            => this.createCustomPassCallback = createCustomPassCallback;

        void OnEnable()
        {
            // Transparent icon to trick search window into indenting items
            if (icon == null)
                icon = new Texture2D(1, 1);
            icon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            icon.Apply();
        }

        void OnDisable()
        {
            if (icon != null)
            {
                DestroyImmediate(icon);
                icon = null;
            }
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Custom Passes"), 0),
            };

            foreach (var customPassType in TypeCache.GetTypesDerivedFrom<CustomPass>())
            {
                if (customPassType.IsAbstract)
                    continue;

                if (customPassType.GetCustomAttribute<HideInInspector>() != null)
                    continue;

                tree.Add(new SearchTreeEntry(new GUIContent(ObjectNames.NicifyVariableName(customPassType.Name), icon))
                {
                    level = 1,
                    userData = customPassType
                });
            }

            return tree;
        }

        // Called when the user validate a choice
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            createCustomPassCallback?.Invoke(searchTreeEntry.userData as Type);
            return true;
        }
    }
}
