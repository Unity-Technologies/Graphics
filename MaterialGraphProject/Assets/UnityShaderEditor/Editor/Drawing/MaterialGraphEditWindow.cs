using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphEditWindow : AbstractGraphEditWindow
    {
        [SerializeField]
        private Object m_Selected;

        [SerializeField]
        private MaterialGraphAsset m_InMemoryAsset;

        public override IGraphAsset inMemoryAsset
        {
            get { return m_InMemoryAsset; }
            set { m_InMemoryAsset = value as MaterialGraphAsset; }
        }

        public override Object selected
        {
            get { return m_Selected; }
            set { m_Selected = value; }
        }

        public override AbstractGraphPresenter CreateDataSource()
        {
            return CreateInstance<MaterialGraphPresenter>();
        }

        public override GraphView CreateGraphView()
        {
            return new MaterialGraphView(this);
        }
    }
}
