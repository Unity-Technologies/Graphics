using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The dynamic border of <see cref="BlockNode"/>s, that have an etch.
    /// </summary>
    public class DynamicBlockBorder : DynamicBorder
    {
        static Color[] s_EtchColors = new Color[4];
        static Vector2[] s_EtchCorners = new Vector2[4];

        /// <summary>
        /// The <see cref="BlockNode"/> this border is for.
        /// </summary>
        BlockNode Node { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="DynamicBlockBorder"/> class.
        /// </summary>
        /// <param name="view"></param>
        public DynamicBlockBorder(BlockNode view)
            : base(view)
        {
            Node = view;

            s_EtchCorners[0] = Vector2.zero;
            s_EtchCorners[1] = Vector2.zero;
        }

        /// <inheritdoc />
        protected override void DrawBorder(MeshGenerationContext mgc, Rect rect, float wantedWidth, Color[] colors,Vector2[] corners)
        {
            GraphViewStaticBridge.Border(mgc, rect, colors, wantedWidth,corners, ContextType.Editor);

            var bounds = Node.EtchBorder.worldBound;
            bounds.height += Node.Etch.worldBound.height;

            var rectEtch = this.WorldToLocal(bounds);
            rectEtch.y -= wantedWidth - 1;
            rectEtch.height += wantedWidth - 1;

            s_EtchColors[0] = colors[0];
            s_EtchColors[1] = Color.clear;
            s_EtchColors[2] = colors[0];
            s_EtchColors[3] = colors[0];

            s_EtchCorners[2] = corners[2];
            s_EtchCorners[3] = corners[3];

            GraphViewStaticBridge.Border(mgc, rectEtch, s_EtchColors, wantedWidth,s_EtchCorners, ContextType.Editor);
        }
    }
}
