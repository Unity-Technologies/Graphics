using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    public class VFXGizmo
    {
        public static void OnDrawGizmo()
        {
        }

        public static void OnDrawComponentGizmo(Object component)
        {
            VisualEffect comp = component as VisualEffect;

            if (VFXViewWindow.currentWindow == null) return;


            VFXView view = VFXViewWindow.currentWindow.graphView as VFXView;

            if (comp.visualEffectAsset != view.controller.model) return;

            VFXBlockUI selectedBlock = view.selection.OfType<VFXBlockUI>().FirstOrDefault();

            if (selectedBlock != null)
            {
                selectedBlock.controller.DrawGizmos(comp);
            }
            else
            {
                VFXOperatorUI selectedOperator = view.selection.OfType<VFXOperatorUI>().FirstOrDefault();

                if (selectedOperator != null)
                {
                    selectedOperator.controller.DrawGizmos(comp);
                }
                else
                {
                    VFXParameterUI selectedParameter = view.selection.OfType<VFXParameterUI>().FirstOrDefault();
                    if (selectedParameter != null)
                    {
                        selectedParameter.controller.DrawGizmos(comp);
                    }
                    else
                    {
                        VFXContextUI selectedContext = view.selection.OfType<VFXContextUI>().FirstOrDefault();
                        if (selectedContext != null)
                        {
                            selectedContext.controller.DrawGizmos(comp);
                        }
                    }
                }
            }
        }
    }
}
