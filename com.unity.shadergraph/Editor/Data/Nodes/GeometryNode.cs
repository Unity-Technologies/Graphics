using System;
using System.Text.RegularExpressions;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    abstract class GeometryNode : AbstractMaterialNode
    {
        public virtual List<CoordinateSpace> validSpaces => new List<CoordinateSpace> {CoordinateSpace.Object, CoordinateSpace.View, CoordinateSpace.World, CoordinateSpace.Tangent};

        [SerializeField]
        private CoordinateSpace m_Space = CoordinateSpace.World;

        [PopupControl("Space")]
        public PopupList spacePopup 
        {
            get 
            {
                var names = validSpaces.Select(cs => cs.ToString().PascalToLabel()).ToArray();
                return new PopupList(names, (int)m_Space);
            }
            set
            {
                if (m_Space == (CoordinateSpace)value.selectedEntry)
                    return;

                m_Space = (CoordinateSpace)value.selectedEntry;
                Dirty(ModificationScope.Graph);
            }
        }
        public CoordinateSpace space => m_Space;

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }
    }
}
