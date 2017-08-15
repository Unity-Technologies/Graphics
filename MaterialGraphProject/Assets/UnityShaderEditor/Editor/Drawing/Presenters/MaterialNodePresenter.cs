using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class MaterialNodePresenter : GraphNodePresenter
    {
        [SerializeField]
        NodePreviewPresenter m_NodePreviewPresenter;

        [SerializeField]
        int m_Version;

        public bool requiresTime
        {
            get { return node is IRequiresTime; }
        }

        public override IEnumerable<GraphElementPresenter> elements
        {
            // TODO JOCE Sub ideal to use yield, but will do for now.
            get
            {
                foreach (var element in base.elements)
                {
                    yield return element;
                }
                yield return m_NodePreviewPresenter;
            }
        }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            var towatch = new List<UnityEngine.Object>();
            towatch.AddRange(base.GetObjectsToWatch());
            towatch.Add(m_NodePreviewPresenter);
            return towatch.ToArray();
        }

        public override void OnModified(ModificationScope scope)
        {
            m_Version++;
            base.OnModified(scope);
            // TODO: Propagate callback rather than setting property
            if (m_NodePreviewPresenter != null)
                m_NodePreviewPresenter.modificationScope = scope;
        }

        protected MaterialNodePresenter()
        {}

        public override void Initialize(INode inNode)
        {
            base.Initialize(inNode);
            m_Version = 0;
            AddPreview(inNode);
        }

        private void AddPreview(INode inNode)
        {
            var materialNode = inNode as AbstractMaterialNode;
            if (materialNode == null || !materialNode.hasPreview)
                return;

            m_NodePreviewPresenter = CreateInstance<NodePreviewPresenter>();
            m_NodePreviewPresenter.Initialize(materialNode);
        }
    }
}
