using System;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;

namespace  UnityEditor.VFX.UI
{
    class VFXBlackboardField : BlackboardField, IControlledElement<VFXParameterController>
    {
        public VFXBlackboardRow owner
        {
            get; set;
        }

        public VFXBlackboardField() : base()
        {
            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
            RegisterCallback<MouseCaptureOutEvent>(OnMouseHover);
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            this.Q<Pill>().AddManipulator(new ContextualMenuManipulator(PillBuildContextualMenu));
        }

        void PillBuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Delete", (a) => Delete(), DropdownMenuAction.AlwaysEnabled);

            evt.StopPropagation();
        }

        void Delete()
        {
            if (selected)
                GetFirstAncestorOfType<VFXView>().DeleteSelection();
            else
                GetFirstAncestorOfType<VFXView>().DeleteElements(new GraphElement[] { this });
        }

        Controller IControlledElement.controller
        {
            get { return owner.controller; }
        }
        public VFXParameterController controller
        {
            get { return owner.controller; }
        }
        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e) {}

        public void SelfChange()
        {
            if (controller.isOutput)
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectAssetEditorUtility.editorResourcesPath + "/VFX/output-dot.png");
            else if (controller.exposed)
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectAssetEditorUtility.editorResourcesPath + "/VFX/exposed-dot.png");
            else
                icon = null;

            if ((!controller.isOutput && !controller.model.outputSlots.Any(t => t.HasLink(true))) || (controller.isOutput && !controller.model.inputSlots.Any(t => t.HasLink(true))))
                AddToClassList("unused");
            else
                RemoveFromClassList("unused");
        }

        void OnMouseHover(EventBase evt)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view != null)
            {
                foreach (var parameter in view.graphElements.ToList().OfType<VFXParameterUI>().Where(t => t.controller.parentController == controller))
                {
                    if (evt.eventTypeId == MouseEnterEvent.TypeId())
                        parameter.AddToClassList("hovered");
                    else
                        parameter.RemoveFromClassList("hovered");
                }
            }
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse)
                e.StopPropagation();
        }
    }
}
