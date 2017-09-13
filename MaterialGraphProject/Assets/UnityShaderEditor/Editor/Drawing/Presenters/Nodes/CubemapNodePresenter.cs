using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
 /*   class CubeContolPresenter : GraphControlPresenter
    {
        //private string[] m_TextureTypeNames;
        //private string[] textureTypeNames
        /* {
             get
             {
                 if (m_TextureTypeNames == null)
                     m_TextureTypeNames = Enum.GetNames(typeof(TextureType));
                 return m_TextureTypeNames;
             }
         }*

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.CubemapNode;
            if (tNode == null)
                return;

            tNode.exposedState = (PropertyNode.ExposedState)EditorGUILayout.EnumPopup(new GUIContent("Exposed"), tNode.exposedState);
            tNode.defaultCube = EditorGUILayout.MiniThumbnailObjectField(new GUIContent("Cubemap"), tNode.defaultCube, typeof(Cubemap), null) as Cubemap;
            //tNode.textureType = (TextureType)EditorGUILayout.Popup((int)tNode.textureType, textureTypeNames, EditorStyles.popup);
        }

        public override float GetHeight()
        {
            return 3 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class CubeNodePresenter : MaterialNodePresenter
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = CreateInstance<CubeContolPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter> { instance };
        }
    }*/
}
