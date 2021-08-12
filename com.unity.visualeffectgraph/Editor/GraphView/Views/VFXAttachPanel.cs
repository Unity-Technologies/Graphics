using System;
using System.Linq;

using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXAttachPanel : PopupWindowContent
    {
        private readonly VFXView m_vfxView;

        public VFXAttachPanel(VFXView vfxView)
        {
            m_vfxView = vfxView;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(250, 80);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.Space(4);
            var isAttached = m_vfxView.attachedComponent != null;
            var selectedVisualEffect = Selection.activeGameObject?.GetComponent<VisualEffect>();

            var isCompatible = selectedVisualEffect != null && selectedVisualEffect.visualEffectAsset == m_vfxView.controller.graph.visualEffectResource.asset;
            GUI.enabled = isAttached || selectedVisualEffect != null && isCompatible;
            var buttonContent = isAttached
                ? VFXView.Contents.detach
                : isCompatible ? VFXView.Contents.attachToSelection : VFXView.Contents.disabledAttachToSelection;

            if (GUILayout.Button(buttonContent, GUILayout.Height(24)))
            {
                if (isAttached)
                {
                    m_vfxView.Detach();
                }
                else
                {
                    m_vfxView.AttachToSelection();
                }
            }

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(VFXView.Contents.pickATarget);
            //var res = m_vfxView.controller.graph.visualEffectResource;
            //var s = $"ref:{AssetDatabase.GetAssetPath(res)}";
            //ObjectSelector.get.searchFilter = s;

            //var position = EditorGUILayout.s_LastRect = EditorGUILayout.GetControlRect(false, 18f, (GUILayoutOption[])null);
            //int controlId = GUIUtility.GetControlID("s_ObjectFieldHash".GetHashCode(), FocusType.Keyboard, position);
            //var result = EditorGUI.DoObjectField(
            //    EditorGUI.IndentedRect(position),
            //    EditorGUI.IndentedRect(position),
            //    controlId,
            //    m_vfxView.attachedComponent,
            //    null,
            //    typeof(VisualEffect),
            //    VFXValidator,
            //    true);
            var result = EditorGUILayout.ObjectField(m_vfxView.attachedComponent, typeof(VisualEffect), true, GUILayout.ExpandWidth(true));
            if (result is VisualEffect visualEffect)
            {
                if (visualEffect != m_vfxView.attachedComponent)
                {
                    m_vfxView.TryAttachTo(visualEffect);
                }
            }
            else if (result == null)
            {
                m_vfxView.Detach();
            }
        }

        //private UnityEngine.Object VFXValidator(UnityEngine.Object[] references, Type objtype, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
        //{
        //    return references
        //        .OfType<GameObject>()
        //        .Select(x => x.GetComponent<VisualEffect>())
        //        .FirstOrDefault(x => x.visualEffectAsset == this.m_vfxView.controller.graph.visualEffectResource.asset);
        //}
    }
}
