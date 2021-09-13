using UnityEngine;

namespace UnityEditor.ShaderGraph.Drawing.Colors
{
    // Defines how the ColorManager interacts with various providers
    interface IColorProvider
    {
        string GetTitle();

        bool AllowCustom();

        bool ClearOnDirty();

        void ApplyColor(IShaderNodeView nodeView);
        void ClearColor(IShaderNodeView nodeView);
    }

    internal abstract class ColorProviderFromCode : IColorProvider
    {
        protected abstract bool GetColorFromNode(AbstractMaterialNode node, out Color color);

        public abstract string GetTitle();

        public abstract bool AllowCustom();

        public abstract bool ClearOnDirty();

        public virtual void ApplyColor(IShaderNodeView nodeView)
        {
            if (GetColorFromNode(nodeView.node, out var color))
            {
                nodeView.SetColor(color);
            }
            else
            {
                nodeView.ResetColor();
            }
        }

        public virtual void ClearColor(IShaderNodeView nodeView)
        {
            nodeView.ResetColor();
        }
    }

    internal abstract class ColorProviderFromStyleSheet : IColorProvider
    {
        protected abstract bool GetClassFromNode(AbstractMaterialNode node, out string ussClass);

        public abstract string GetTitle();

        public abstract bool AllowCustom();

        public abstract bool ClearOnDirty();

        public virtual void ApplyColor(IShaderNodeView nodeView)
        {
            if (GetClassFromNode(nodeView.node, out var ussClass))
            {
                nodeView.colorElement.AddToClassList(ussClass);
            }
        }

        public virtual void ClearColor(IShaderNodeView nodeView)
        {
            if (GetClassFromNode(nodeView.node, out var ussClass))
            {
                nodeView.colorElement.RemoveFromClassList(ussClass);
            }
        }
    }
}
