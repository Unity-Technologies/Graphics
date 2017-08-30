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
            VFXViewPresenter presenter = VFXViewWindow.viewPresenter;
            if (presenter.HasVFXAsset())
            {
                //var allBlocks = presenter.allChildren.OfType<VFXContextPresenter>().SelectMany(t => t.allChildren.OfType<VFXBlockPresenter>());

                //Debug.Log("Blocks:"+allBlocks.Count());
            }
        }
    }
}
