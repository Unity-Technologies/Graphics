using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEditor;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    class AnyNodeControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.AnyNodeBase;
            if (tNode == null)
                return;

            var properties = tNode.properties;

            EditorGUI.BeginChangeCheck();
            UnityEngine.Graphing.ModificationScope modificationScope = UnityEngine.Graphing.ModificationScope.Node;

            foreach (AnyNodeProperty p in properties)
            {
                AnyNodePropertyState newState = (AnyNodePropertyState) EditorGUILayout.EnumPopup(p.name, p.state);
                if (newState != p.state)
                {
                    tNode.setPropertyState(p, newState);

                    modificationScope = UnityEngine.Graphing.ModificationScope.Graph;
                }

                bool disabled = p.state == AnyNodePropertyState.Slot;
                EditorGUI.BeginDisabledGroup(disabled);
                {
                    switch (p.propertyType)
                    {
                        case PropertyType.Color:
                            // TODO
                            break;
                        case PropertyType.Texture:
                            // TODO
                            break;
                        case PropertyType.Cubemap:
                            // TODO
                            break;
                        case PropertyType.Float:
                            p.value.x = EditorGUILayout.FloatField("", p.value.x);
                            break;
                        case PropertyType.Vector2:
                            {
                                Vector2 result = EditorGUILayout.Vector2Field("", new Vector2(p.value.x, p.value.y));
                                p.value.x = result.x;
                                p.value.y = result.y;
                            }
                            break;
                        case PropertyType.Vector3:
                            p.value = EditorGUILayout.Vector3Field("", p.value);
                            break;
                        case PropertyType.Vector4:
                            p.value = EditorGUILayout.Vector4Field("", p.value);
                            break;
                        case PropertyType.Matrix2:
                            //                        p.value = EditorGUILayout.Matrix2Field("", p.value);
                            break;
                        case PropertyType.Matrix3:
                            //                        p.value = EditorGUILayout.Matrix3Field("", p.value);
                            break;
                        case PropertyType.Matrix4:
                            //                        p.value = EditorGUILayout.Matrix4Field("", p.value);
                            break;
                    }
                }
                EditorGUI.EndDisabledGroup();
            }

            bool changed= EditorGUI.EndChangeCheck();

            if (changed)
            {
                if (tNode.onModified != null)
                    tNode.onModified(tNode, modificationScope);
            }
        }

        public override float GetHeight()
        {
            var tNode = node as UnityEngine.MaterialGraph.AnyNodeBase;
            if (tNode == null)
                return EditorGUIUtility.standardVerticalSpacing;

            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * tNode.propertyCount * 2
                + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class AnyNodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<AnyNodeControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter>(base.GetControlData()) { instance };
        }
    }
}
