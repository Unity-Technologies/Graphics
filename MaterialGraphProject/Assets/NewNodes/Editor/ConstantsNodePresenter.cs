using System;
using System.Collections.Generic;
using UnityEngine.MaterialGraph;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    class ConstantsContolPresenter : GraphControlPresenter
    {
        private string[] m_ConstantTypeNames;
        private string[] constantTypeNames
        {
            get
            {
                if (m_ConstantTypeNames == null)
                    m_ConstantTypeNames = Enum.GetNames(typeof(ConstantType));
                return m_ConstantTypeNames;
            }
        }

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var cNode = node as UnityEngine.MaterialGraph.ConstantsNode;
            if (cNode == null)
                return;

            cNode.constant = (ConstantType)EditorGUILayout.Popup((int)cNode.constant, constantTypeNames, EditorStyles.popup);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }

#if WITH_PRESENTER
    [Serializable]
    public class ConstantsNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = CreateInstance<ConstantsContolPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }
#endif

    public class ConstantsNodeView : MaterialNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<ConstantsContolPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }
}
