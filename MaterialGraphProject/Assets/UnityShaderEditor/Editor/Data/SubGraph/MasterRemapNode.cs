using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Master/Remap-Node")]
    public class MasterRemapNode : AbstractSubGraphNode, IMasterNode
    {
        [SerializeField]
        protected SurfaceMaterialOptions m_MaterialOptions = new SurfaceMaterialOptions();

        public SurfaceMaterialOptions options
        {
            get { return m_MaterialOptions; }
        }

        public virtual PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        [SerializeField]
        private string m_SerializedRemapGraph = string.Empty;

        [Serializable]
        private class RemapGraphHelper
        {
            public MasterRemapGraphAsset remapGraph;
        }

        protected override AbstractSubGraph referencedGraph
        {
            get
            {
                if (remapGraphAsset == null)
                    return null;

                return remapGraphAsset.remapGraph;
            }
        }

#if UNITY_EDITOR
        [ObjectControl("Remap")]
        public MasterRemapGraphAsset remapGraphAsset
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedRemapGraph))
                    return null;

                var helper = new RemapGraphHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedRemapGraph, helper);
                return helper.remapGraph;
            }
            set
            {
                if (remapGraphAsset == value)
                    return;

                var helper = new RemapGraphHelper();
                helper.remapGraph = value;
                m_SerializedRemapGraph = EditorJsonUtility.ToJson(helper, true);
                UpdateSlots();

                if (onModified != null)
                    onModified(this, ModificationScope.Topological);
            }
        }
#else
        public MaterialSubGraphAsset subGraphAsset {get; set; }
#endif

        public MasterRemapNode()
        {
            name = "RemapMaster";
        }

        public IEnumerable<string> GetSubshader(ShaderGraphRequirements graphRequirements, MasterRemapGraph remapper)
        {

            if (referencedGraph == null)
                return new string[]{};

            var masterNodes = referencedGraph.activeNodes.OfType<IMasterNode>().ToList();

            if (masterNodes.Count == 0)
                return new string[]{};

            var results = new List<string>();
            foreach (var master in masterNodes)
                results.AddRange(master.GetSubshader(graphRequirements, referencedGraph as MasterRemapGraph));

            return results;
        }
    }
}
