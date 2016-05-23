using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;


namespace UnityEditor.Experimental
{
    public class EditorKeyboardControl : IManipulate
    {

        public EditorKeyboardControl()
        {

        }

        public void AttachTo(CanvasElement e)
        {
            e.KeyDown += OnCanvasKeyDown;
        }

        private bool OnCanvasKeyDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if ( e.rawType != EventType.keyDown)
                return false;


            bool needRefresh = false;
            switch(e.keyCode)
            {
                case KeyCode.Alpha1: VFXEditor.AssetModel.component.playRate = 0.01f; needRefresh = true; break;
                case KeyCode.Alpha2: VFXEditor.AssetModel.component.playRate = 0.1f; needRefresh = true; break;
                case KeyCode.Alpha3: VFXEditor.AssetModel.component.playRate = 0.25f; needRefresh = true; break;
                case KeyCode.Alpha4: VFXEditor.AssetModel.component.playRate = 0.5f; needRefresh = true; break;
                case KeyCode.Alpha5: VFXEditor.AssetModel.component.playRate = 1.0f; needRefresh = true; break;
                case KeyCode.Alpha6: VFXEditor.AssetModel.component.playRate = 2.0f; needRefresh = true; break;
                case KeyCode.Alpha7: VFXEditor.AssetModel.component.playRate = 8.0f; needRefresh = true; break;

                case KeyCode.Space: VFXEditor.AssetModel.component.Reinit(); needRefresh = true; break;
                case KeyCode.RightArrow:
                    VFXEditor.AssetModel.component.pause = true;
                    VFXEditor.AssetModel.component.AdvanceOneFrame();
                    break;
                case KeyCode.LeftArrow:
                    VFXEditor.AssetModel.component.pause = true;
                    float pr = VFXEditor.AssetModel.component.playRate;
                    VFXEditor.AssetModel.component.playRate = -pr;
                    VFXEditor.AssetModel.component.AdvanceOneFrame();
                    VFXEditor.AssetModel.component.playRate = pr;
                    needRefresh = true;
                    break;
                default:
                    break;
            }

            if (needRefresh)
            {
                e.Use();
            }

            return needRefresh;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }
    }
}
