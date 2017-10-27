using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class MasterNode : AbstractMaterialNode, IMasterNode
    {
        [SerializeField]
        protected SurfaceMaterialOptions m_MaterialOptions = new SurfaceMaterialOptions();

        public MasterNode()
        {
            name = "MasterNode";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public virtual ShaderGraphRequirements GetNodeSpecificRequirements()
        {
            return ShaderGraphRequirements.none;
        }

        public SurfaceMaterialOptions options
        {
            get { return m_MaterialOptions; }

        }

        public abstract IEnumerable<string> GetSubshader(ShaderGraphRequirements graphRequirements, MasterRemapGraph remapper);
    }
}
