using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.UIGen
{
    // store static default values per type.
    public struct UIViewDefaults<TUIView>
    {
        public static string DefaultTemplateAssetPath = "";
    }

    public abstract class UIView<TSelf, TContext>
    {
        TContext m_Context;
        TemplateContainer m_Container;

        public static bool FromVisualTreeAsset(
            [DisallowNull] VisualTreeAsset treeAsset,
            [NotNullWhen(true)] out TSelf view,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }

        public bool AddTo(
            [DisallowNull] VisualElement root,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }

        public bool AssignContext(
            [DisallowNull] TContext context,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }

        protected abstract bool BindContext(
            [DisallowNull] TContext context,
            [DisallowNull] TemplateContainer container,
            [NotNullWhen(false)] out Exception error
        );

        protected abstract bool UnbindContext(
            [DisallowNull] TContext context,
            [DisallowNull] TemplateContainer container,
            [NotNullWhen(false)] out Exception error
        );
    }
}
