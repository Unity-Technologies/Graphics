using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.UI
{
    static class BoardPreferenceHelper
    {
        public enum Board
        {
            blackboard,
            componentBoard,
            customAttributeBoard
        }


        const string rectPreferenceFormat = "vfx-{0}-rect";
        const string visiblePreferenceFormat = "vfx-{0}-visible";


        public static bool IsVisible(Board board, bool defaultState)
        {
            return EditorPrefs.GetBool(string.Format(visiblePreferenceFormat, board), defaultState);
        }

        public static void SetVisible(Board board, bool value)
        {
            EditorPrefs.SetBool(string.Format(visiblePreferenceFormat, board), value);
        }

        public static Rect LoadPosition(Board board, Rect defaultPosition)
        {
            string str = EditorPrefs.GetString(string.Format(rectPreferenceFormat, board));

            Rect blackBoardPosition = defaultPosition;
            if (!string.IsNullOrEmpty(str))
            {
                var rectValues = str.Split(',');

                if (rectValues.Length == 4)
                {
                    float x, y, width, height;
                    if (float.TryParse(rectValues[0], out x) && float.TryParse(rectValues[1], out y) && float.TryParse(rectValues[2], out width) && float.TryParse(rectValues[3], out height))
                    {
                        blackBoardPosition = new Rect(x, y, width, height);
                    }
                }
            }

            return blackBoardPosition;
        }

        public static void SavePosition(Board board, Rect r)
        {
            EditorPrefs.SetString(string.Format(rectPreferenceFormat, board), string.Format("{0},{1},{2},{3}", r.x, r.y, r.width, r.height));
        }

        public static readonly Vector2 sizeMargin = Vector2.one * 30;

        public static bool ValidatePosition(GraphElement element, VFXView view, Rect defaultPosition)
        {
            Rect viewrect = view.contentRect;
            Rect rect = element.GetPosition();
            bool changed = false;

            if (!viewrect.Contains(rect.position))
            {
                Vector2 newPosition = defaultPosition.position;
                if (!viewrect.Contains(defaultPosition.position))
                {
                    newPosition = sizeMargin;
                }

                rect.position = newPosition;

                changed = true;
            }

            Vector2 maxSizeInView = viewrect.max - rect.position - sizeMargin;
            float newWidth = Mathf.Max(element.style.minWidth, Mathf.Min(rect.width, maxSizeInView.x));
            float newHeight = Mathf.Max(element.style.minHeight, Mathf.Min(rect.height, maxSizeInView.y));

            if (Mathf.Abs(newWidth - rect.width) > 1)
            {
                rect.width = newWidth;
                changed = true;
            }

            if (Mathf.Abs(newHeight - rect.height) > 1)
            {
                rect.height = newHeight;
                changed = true;
            }

            if (changed)
            {
                element.SetPosition(rect);
            }

            return false;
        }
    }

    class VFXBoard : GraphElement, IControlledElement<VFXViewController>, IVFXMovable, IVFXResizable
    {
        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    Clear();
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }

        protected VFXView m_View;

        BoardPreferenceHelper.Board m_Board;


        readonly Rect m_DefaultRect;
        public VFXBoard(VFXView view, BoardPreferenceHelper.Board board, Rect defaultRect)
        {
            m_Board = board;
            m_View = view;
            m_DefaultRect = defaultRect;

            var tpl = Resources.Load<VisualTreeAsset>("uxml/VFXBoard");

            tpl.CloneTree(this, new Dictionary<string, VisualElement>());

            AddStyleSheetPath("VFXBoard");
            AddToClassList("VFXBoard");

            m_ScrollView = this.Q<ScrollView>("scrollView");
            m_TitleLabel = this.Q<Label>("titleLabel");
            m_SubTitleLabel = this.Q<Label>("subTitleLabel");
            capabilities |= Capabilities.Movable;
            
            this.AddManipulator(new Dragger { clampToParentEdges = true });
            RegisterCallback<MouseDownEvent>(OnMouseClick, TrickleDown.TrickleDown);
            style.positionType = PositionType.Absolute;
            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.componentBoard, defaultRect));
        }

        public override Rect GetPosition()
        {
            return new Rect(style.positionLeft, style.positionTop, style.width, style.height);
        }

        public override void SetPosition(Rect newPos)
        {
            style.positionLeft = newPos.xMin;
            style.positionTop = newPos.yMin;
            style.width = newPos.width;
            style.height = newPos.height;
        }

        public override void UpdatePresenterPosition()
        {
            BoardPreferenceHelper.SavePosition(m_Board, GetPosition());
        }
        public void ValidatePosition()
        {
            BoardPreferenceHelper.ValidatePosition(this, m_View, m_DefaultRect);
        }

        public void OnMoved()
        {
            BoardPreferenceHelper.SavePosition(m_Board, GetPosition());
        }

        void IVFXResizable.OnStartResize() { }
        public void OnResized()
        {
            BoardPreferenceHelper.SavePosition(m_Board, GetPosition());
        }

        ScrollView m_ScrollView;
        Label m_TitleLabel;
        Label m_SubTitleLabel;


        public override VisualElement contentContainer
        {
            get { return m_ScrollView; }
        }

        void OnMouseClick(MouseDownEvent e)
        {
            m_View.SetBoardToFront(this);
        }
        public virtual void OnControllerChanged(ref ControllerChangedEvent e) { }
        public override string title
        {
            set
            { m_TitleLabel.text = value; }
        }

        public string subTitle
        {
            set { m_SubTitleLabel.text = value; }
        }
    }
}
