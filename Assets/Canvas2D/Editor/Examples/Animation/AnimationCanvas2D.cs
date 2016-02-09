using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental;
using Object = UnityEngine.Object;

#pragma warning disable 0414
#pragma warning disable 0219

namespace UnityEditor
{
    internal class AnimationCanvas2D : EditorWindow
    {
        [MenuItem("Window/Canvas2D/Animation Example")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(AnimationCanvas2D));
        }

        private Canvas2D m_Canvas = null;
        private EditorWindow m_HostWindow = null;
        private List<CanvasElement> m_Data = new List<CanvasElement>();

        private int k_sideDashBoardSize = 200;
        private int k_sideDashBoardPadding = 4;

        public void AddElement(CanvasElement e)
        {
            m_Data.Add(e);
            m_Canvas.ReloadData();

            var scaling = e.scale;
            m_Canvas.Animate(e).Lerp("m_Scale", new Vector3(0.1f, 0.1f, 0.1f), scaling);
        }

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_Canvas = new Canvas2D(this, m_HostWindow, new AnimationDataSource(m_Data));

                // draggable manipulator allows to move the canvas around. Note that individual elements can have the draggable manipulator on themselves
                m_Canvas.AddManipulator(new Draggable(2, EventModifiers.None));
                m_Canvas.AddManipulator(new Draggable(0, EventModifiers.Alt));

                // make the canvas zoomable
                m_Canvas.AddManipulator(new Zoomable());

                // allow framing the selection when hitting "F" (frame) or "A" (all). Basically shows how to trap a key and work with the canvas selection
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.All));
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.Selection));

                // The following manipulator show how to work with canvas2d overlay and background rendering
                m_Canvas.AddManipulator(new RectangleSelect());
                m_Canvas.AddManipulator(new ScreenSpaceGrid());

                m_Canvas.AddManipulator(new ContextualMenu((e, parent, customData) =>
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Add FlexBox"), false, (canvas) =>
                    {
                        var pos = m_Canvas.MouseToCanvas(e.mousePosition);
                        AddElement(new FlexBox(pos, 250.0f));
                    }
                        , this);

                    menu.ShowAsContext();
                    return false;
                }));
            }

            Rebuild();
        }

        private void Rebuild()
        {
            if (m_Canvas == null)
                return;

            m_Canvas.Clear();
            m_Canvas.ReloadData();
            m_Canvas.ZSort();
        }

        void OnGUI()
        {
            m_HostWindow = this;
            if (m_Canvas == null)
            {
                InitializeCanvas();
            }

            m_Canvas.OnGUI(this, new Rect(0, 0, position.width - k_sideDashBoardSize, position.height));

            GUILayout.BeginArea(new Rect(position.width - k_sideDashBoardSize + k_sideDashBoardPadding, 0, k_sideDashBoardSize - (k_sideDashBoardPadding * 2), position.height));

            GUILayout.Space(3f);
            GUILayout.Label("This example shows the Canvas2D.Animate feature which enabled any CanvasElement property to be animated. You can chain animations together to create sequences, or animate many parameters simultaneously.", EditorStyles.helpBox);

            GUILayout.Space(3f);
            if (GUILayout.Button(new GUIContent("Clear Canvas")))
            {
                m_Data.Clear();
                Rebuild();
            }

            GUILayout.Space(3f);
            if (GUILayout.Button(new GUIContent("Create Animated FlexBox", "Add a new animated flexbox to the canvas")))
            {
                Vector2 spawnPosition = new Vector2(UnityEngine.Random.Range(m_Canvas.clientRect.x, m_Canvas.clientRect.width),
                        UnityEngine.Random.Range(m_Canvas.clientRect.y, m_Canvas.clientRect.height - 50.0f));
                AddElement(new FlexBox(spawnPosition, 250.0f));
            }

            m_Canvas.showQuadTree = GUILayout.Toggle(m_Canvas.showQuadTree, new GUIContent("Show Debug Info", "Turns debug info on and off"));

            GUILayout.EndArea();
        }
    }
}
