using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class TextureContolDrawData : ControlDrawData
    {
        private string[] m_TextureTypeNames;
        private string[] textureTypeNames
        {
            get
            {
                if (m_TextureTypeNames == null)
                    m_TextureTypeNames = Enum.GetNames(typeof(TextureType));
                return m_TextureTypeNames;
            }
        }

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.TextureNode;
            if (tNode == null)
                return;

            tNode.exposedState = (PropertyNode.ExposedState)EditorGUILayout.EnumPopup(new GUIContent("Exposed"), tNode.exposedState);
            tNode.defaultTexture = EditorGUILayout.MiniThumbnailObjectField(new GUIContent("Texture"), tNode.defaultTexture, typeof(Texture2D), null) as Texture2D;
            tNode.textureType = (TextureType)EditorGUILayout.Popup((int)tNode.textureType, textureTypeNames, EditorStyles.popup);
        }

        public override float GetHeight()
        {
            return 3 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class TextureNodeDrawData : MaterialNodeDrawData
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<TextureContolDrawData>();
            instance.Initialize(node);
            return new List<GraphElementPresenter> { instance };
        }
    }
}
