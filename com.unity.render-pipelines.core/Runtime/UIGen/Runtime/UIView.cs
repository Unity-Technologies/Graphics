using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.UIGen
{
    // store static default values per type.
    public struct UIViewDefaults<TUIView>
    {
        public static string DefaultTemplateAssetPath = "";
    }

    public abstract class UIView<TSelf, TContext>
        where TSelf: UIView<TSelf, TContext>, new()
        where TContext: class
    {
        TContext m_Context;
        TemplateContainer m_Container;

        // TODO: [Fred] Very bad practice, visual tree asset should be assigned once at initialization and
        //  be constant. Otherwise we don't know what happens if it changes during its lifetime
        [MustUseReturnValue]
        public bool SetVisualTreeAsset(
            [DisallowNull] VisualTreeAsset asset,
            out Exception error
        )
        {
            m_Container = asset.Instantiate();
            error = default;
            return true;
        }

        public bool AddTo(
            [DisallowNull] VisualElement root,
            [NotNullWhen(false)] out Exception error
        )
        {
            root.Add(m_Container);
            error = null;
            return true;
        }

        public bool AssignContext(
            [DisallowNull] TContext context,
            [NotNullWhen(false)] out Exception error
        )
        {
            if (context == null && m_Context == null
                || context != null && context.Equals(m_Context))
            {
                // nothing to do
                error = null;
                return true;
            }

            if (m_Context != null)
            {
                if (!UnbindContext(m_Context, m_Container, out error))
                    return false;
                m_Context = null;
            }

            if (context != null)
            {
                m_Context = context;
                if (!BindContext(context, m_Container, out error))
                    return false;
            }

            error = default;
            return true;
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
