using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering.UIGen;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.UIGen
{
    public class UIViewEditorWindow<TUIView, TIContext, TContext> : EditorWindow
        where TUIView: UIView<TUIView, TIContext>, new()
        where TContext: class, TIContext, new()
        where TIContext : class
    {
        static bool LoadDefaultVisualTreeAsset(
            [NotNullWhen(true)] out VisualTreeAsset vsTree,
            [NotNullWhen(false)] out Exception error
        )
        {
            return LoadAsset(UIViewDefaults<TUIView>.DefaultTemplateAssetPath, out vsTree, out error);
        }

        public static bool LoadAsset<TAsset>(
            [DisallowNull] string path,
            [NotNullWhen(true)] out TAsset asset,
            [NotNullWhen(false)] out Exception error
        ) where TAsset : Object
        {
            try
            {
                asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            }
            catch (Exception e)
            {
                asset = null;
                error = e;
                return false;
            }

            if (asset == null)
            {
                error = new Exception($"Asset is missing at {path}").WithStackTrace();
                return false;
            }

            error = null;
            return true;
        }

        TUIView m_UIViewInstance;

        bool InstantiateDebugMenu(
            [NotNullWhen(false)] out Exception error
        )
        {
            if (!LoadDefaultVisualTreeAsset(out var vsTree, out error))
                return false;

            // TODO: [Fred] Must be initialized during allocation (faillable allocation & init)
            m_UIViewInstance = new TUIView();
            if (!m_UIViewInstance.SetVisualTreeAsset(vsTree, out error))
                return false;

            var context = new TContext();
            if (!m_UIViewInstance.AssignContext(context, out error))
                return false;

            return true;
        }

        bool TryCreateGUI(
            [NotNullWhen(false)] out Exception error
        )
        {
            error = default;

            if (m_UIViewInstance == null && !InstantiateDebugMenu(out error))
                return false;

            if (!m_UIViewInstance.AddTo(rootVisualElement, out error))
                return false;

            return true;
        }

        void CreateGUI()
        {
            if (!TryCreateGUI(out var error))
                Debug.LogException(error);
        }
    }
}
