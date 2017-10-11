using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class LevelsControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as LevelsNode;
            if (tNode == null)
                return;

            /*tNode.inputMin   = EditorGUILayout.FloatField("InputMin:", tNode.inputMin);
            tNode.inputMax   = EditorGUILayout.FloatField("InputMax:", tNode.inputMax);
            tNode.inputGamma = EditorGUILayout.FloatField("InputGamma:", tNode.inputGamma);
            tNode.outputMin  = EditorGUILayout.FloatField("OutputMin:", tNode.outputMin);
            tNode.outputMax  = EditorGUILayout.FloatField("OutputMax:", tNode.outputMax);*/
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight * 5 + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

#if WITH_PRESENTER
    [Serializable]
    public class LevelsNodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = CreateInstance<LevelsControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }
#endif

    public class LevelsNodeView : MaterialNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<LevelsControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }
}
