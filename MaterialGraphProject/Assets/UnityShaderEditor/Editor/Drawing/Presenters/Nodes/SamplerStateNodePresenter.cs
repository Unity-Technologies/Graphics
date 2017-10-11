using System;
using System.Collections.Generic;
using UnityEngine.MaterialGraph;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

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
                    samplerFilterMode = Enum.GetNames(typeof(TextureSamplerState.FilterMode));
                return samplerFilterMode;
            }
        }

        private string[] _samplerWrapMode
        {
            get
            {
                if (samplerWrapMode == null)
                    samplerWrapMode = Enum.GetNames(typeof(TextureSamplerState.WrapMode));
                return samplerWrapMode;
            }
        }

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var cNode = node as SamplerStateNode;
            if (cNode == null)
                return;

            cNode.filter = (TextureSamplerState.FilterMode)EditorGUILayout.Popup((int)cNode.filter, _samplerFilterMode, EditorStyles.popup);
            cNode.wrap = (TextureSamplerState.WrapMode)EditorGUILayout.Popup((int)cNode.wrap, _samplerWrapMode, EditorStyles.popup);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }

#if WITH_PRESENTER
    [Serializable]
    public class SamplerStateNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = CreateInstance<SamplerStateControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }
#endif

    public class SamplerStateNodeView : MaterialNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<SamplerStateControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }
}
