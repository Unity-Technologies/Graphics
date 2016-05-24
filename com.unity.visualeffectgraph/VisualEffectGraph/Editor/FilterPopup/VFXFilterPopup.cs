using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Experimental
{
    internal class VFXFilterPopup : IManipulate {

        public VFXFilterPopup()
        {

        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.KeyDown += KeyDown;
        }

        private bool KeyDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.keyCode == KeyCode.Tab)
            {
                if(parent.selection.Count > 0)
                {
                    CanvasElement selected = ((VFXEdCanvas)parent).selection[0];

                    if(selected is VFXEdContextNode)
                    {
                        VFXFilterWindow.Show(new Rect(e.mousePosition.x - 120, e.mousePosition.y - 10, 240, 0), new VFXBlockProvider(e.mousePosition, ((VFXEdContextNode)selected).Model, (VFXEdDataSource)parent.dataSource ));
                    }
                    else if(((VFXEdCanvas)parent).selection[0] is VFXEdDataNode)
                    {
                        VFXFilterWindow.Show(new Rect(e.mousePosition.x - 120, e.mousePosition.y - 10, 240, 0), new VFXDataBlockProvider(e.mousePosition, ((VFXEdDataNode)selected).Model, (VFXEdDataSource)parent.dataSource ));
                    }
                }
                else
                {
                    VFXFilterWindow.Show(new Rect(e.mousePosition.x - 120, e.mousePosition.y - 10, 240, 0), new VFXNodeProvider(e.mousePosition, (VFXEdDataSource)parent.dataSource, (VFXEdCanvas)parent));
                }
                return true;
            }

            return false;
        }
    }
}
