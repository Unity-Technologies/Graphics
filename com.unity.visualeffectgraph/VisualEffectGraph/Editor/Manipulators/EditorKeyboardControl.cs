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
            SceneView.onSceneGUIDelegate += OnSceneGUIKeyDown;
        }

        public void OnSceneGUIKeyDown(SceneView sceneview)
        {
            Event e = Event.current;
            if(e.type == EventType.keyDown)
                    OnKeyDown(e);
        }

        private bool OnCanvasKeyDown(CanvasElement element, Event e, Canvas2D parent)
        {
            return OnKeyDown(e);
        }

        private bool OnKeyDown(Event e)
        { 
            if (e.type == EventType.used)
                return false;


            bool needRefresh = false;

            var component = VFXEditor.component;
            switch(e.keyCode)
            {
                case KeyCode.Alpha1: component.playRate = 0.01f; needRefresh = true; break;
                case KeyCode.Alpha2: component.playRate = 0.1f; needRefresh = true; break;
                case KeyCode.Alpha3: component.playRate = 0.25f; needRefresh = true; break;
                case KeyCode.Alpha4: component.playRate = 0.5f; needRefresh = true; break;
                case KeyCode.Alpha5: component.playRate = 1.0f; needRefresh = true; break;
                case KeyCode.Alpha6: component.playRate = 2.0f; needRefresh = true; break;
                case KeyCode.Alpha7: component.playRate = 8.0f; needRefresh = true; break;

                case KeyCode.Space: component.Reinit(); needRefresh = true; break;
                case KeyCode.RightArrow:
                    component.pause = true;
                    component.AdvanceOneFrame();
                    break;
                case KeyCode.LeftArrow:
                    component.pause = true;
                    float pr = component.playRate;
                    component.playRate = -pr;
                    component.AdvanceOneFrame();
                    component.playRate = pr;
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
