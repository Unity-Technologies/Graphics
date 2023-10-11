using UnityEngine;

namespace UnityEditor.ShaderGraph.Drawing.Colors
{
    class HeatmapColors : ColorProviderFromCode
    {
        public const string Title = "Heatmap";

        protected override bool GetColorFromNode(AbstractMaterialNode node, out Color color)
        {
            var projectHeatValues = ShaderGraphProjectSettings.instance.GetHeatValues();
            if (projectHeatValues != null)
            {
                return projectHeatValues.TryGetCategoryColor(node, out color);
            }

            color = Color.black;
            return false;
        }

        public override string GetTitle() => Title;

        // Custom colors are handled through a custom heatmap asset.
        public override bool AllowCustom() => false;

        public override bool ClearOnDirty() => false;
    }
}
