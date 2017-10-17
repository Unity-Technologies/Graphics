using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    public class VFXGizmo
    {
        public static void OnDrawGizmo()
        {
            /*VFXViewPresenter presenter = VFXViewPresenter.viewPresenter;
            if (presenter.HasVFXAsset())
            {
                //var allBlocks = presenter.allChildren.OfType<VFXContextPresenter>().SelectMany(t => t.allChildren.OfType<VFXBlockPresenter>());

                //Debug.Log("Blocks:"+allBlocks.Count());
            }*/
        }

        public static void OnDrawComponentGizmo(Object component)
        {
            VFXComponent comp = component as VFXComponent;

            if (VFXViewWindow.currentWindow == null) return;


            VFXView view = VFXViewWindow.currentWindow.graphView as VFXView;

            VFXBlockUI selectedBlock = view.selection.OfType<VFXBlockUI>().FirstOrDefault();

            if (selectedBlock != null)
            {
                selectedBlock.GetPresenter<VFXBlockPresenter>().DrawGizmos(comp);
            }
            else
            {
                VFXOperatorUI selectedOperator = view.selection.OfType<VFXOperatorUI>().FirstOrDefault();

                if (selectedOperator != null)
                {
                    selectedOperator.GetPresenter<VFXSlotContainerPresenter>().DrawGizmos(comp);
                }
                else
                {
                    VFXParameterUI selectedParameter = view.selection.OfType<VFXParameterUI>().FirstOrDefault();
                    if (selectedParameter != null)
                    {
                        selectedParameter.GetPresenter<VFXSlotContainerPresenter>().DrawGizmos(comp);
                    }
                }
            }
        }
    }
}
