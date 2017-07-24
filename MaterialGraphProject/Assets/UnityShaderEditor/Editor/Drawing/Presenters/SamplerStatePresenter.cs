using System;
using System.Collections.Generic;
using UnityEngine.MaterialGraph;
using UIElements.GraphView;
using UnityEditor.Graphing.Drawing;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class SamplerStateControlPresenter : GraphControlPresenter
    {

        private string[] samplerFilterMode;
        private string[] samplerWrapMode;

        private string[] _samplerFilterMode
        {
            get
            {
                if (samplerFilterMode == null)
                    samplerFilterMode = Enum.GetNames(typeof(SamplerStateNode.FilterMode));
                return samplerFilterMode;
            }
        }

        private string[] _samplerWrapMode
        {
            get
            {
                if (samplerWrapMode == null)
                    samplerWrapMode = Enum.GetNames(typeof(SamplerStateNode.WrapMode));
                return samplerWrapMode;
            }
        }

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var cNode = node as UnityEngine.MaterialGraph.SamplerStateNode;
            if (cNode == null)
                return;

            cNode.filter = (SamplerStateNode.FilterMode)EditorGUILayout.Popup((int)cNode.filter, _samplerFilterMode, EditorStyles.popup);
            cNode.wrap = (SamplerStateNode.WrapMode)EditorGUILayout.Popup((int)cNode.wrap, _samplerWrapMode, EditorStyles.popup);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class SamplerStateNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<SamplerStateControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}
