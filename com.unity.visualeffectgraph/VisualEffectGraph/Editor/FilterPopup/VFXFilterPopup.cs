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

        public static void ShowNewBlockPopup(VFXEdContextNode contextNode, Vector2 mousePosition, Canvas2D canvas, bool bFromAnotherPopupWindow)
        {
           Vector2 pos = mousePosition;
           if (bFromAnotherPopupWindow)
                pos = Event.current.mousePosition;

           VFXFilterWindow.Show(pos, new VFXBlockProvider(mousePosition, contextNode.Model, (VFXEdDataSource)canvas.dataSource));
        }

        public static void ShowReplaceBlockPopup(VFXEdContextNode contextNode, VFXEdProcessingNodeBlock nodeBlock, Vector2 mousePosition, Canvas2D canvas, bool bFromAnotherPopupWindow)
        {
           Vector2 pos = mousePosition;
           if (bFromAnotherPopupWindow)
                pos = Event.current.mousePosition;

           VFXFilterWindow.Show(pos, new VFXBlockProvider(mousePosition, contextNode.Model, nodeBlock.Model, (VFXEdDataSource)canvas.dataSource));
        }

        public static void ShowNewDataBlockPopup(VFXEdDataNode dataNode, Vector2 mousePosition, Canvas2D canvas, bool bFromAnotherPopupWindow)
        {
            Vector2 pos = mousePosition;
            if (bFromAnotherPopupWindow)
                pos = Event.current.mousePosition;

            VFXFilterWindow.Show(pos, new VFXDataBlockProvider(mousePosition, dataNode.Model, (VFXEdDataSource)canvas.dataSource ));
        }

        public static void ShowNewNodePopup(Vector2 mousePosition, Canvas2D canvas, bool bFromAnotherPopupWindow)
        {
            Vector2 pos = mousePosition;
            if (bFromAnotherPopupWindow)
                pos = Event.current.mousePosition;

            VFXFilterWindow.Show(pos, new VFXNodeProvider(mousePosition, (VFXEdDataSource)canvas.dataSource, (VFXEdCanvas)canvas));
        }

        public static void ShowNewDataNodePopup(Vector2 mousePosition, Canvas2D canvas, bool bFromAnotherPopupWindow)
        {
            Vector2 pos = mousePosition;
            if (bFromAnotherPopupWindow)
                pos = Event.current.mousePosition;

            VFXFilterWindow.Show(pos, new VFXDataNodeProvider(mousePosition, (VFXEdDataSource)canvas.dataSource, (VFXEdCanvas)canvas));
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
                        ShowNewBlockPopup((VFXEdContextNode)selected, e.mousePosition, parent, false);
                    }
                    else if(((VFXEdCanvas)parent).selection[0] is VFXEdDataNode)
                    {
                        ShowNewDataBlockPopup((VFXEdDataNode)selected, e.mousePosition, parent, false);
                    }
                }
                else
                {
                    ShowNewNodePopup(e.mousePosition, parent, false);
                }
                return true;
            }

            return false;
        }
    }
}
