using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

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
            float newWidth = Mathf.Max(element.resolvedStyle.minWidth.value, Mathf.Min(rect.width, maxSizeInView.x));
            float newHeight = Mathf.Max(element.resolvedStyle.minHeight.value, Mathf.Min(rect.height, maxSizeInView.y));

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

            tpl.CloneTree(this);

            this.AddStyleSheetPath("VFXBoard");
            AddToClassList("VFXBoard");

            m_ScrollView = this.Q<ScrollView>("scrollView");
            m_TitleLabel = this.Q<Label>("titleLabel");
            m_SubTitleLabel = this.Q<Label>("subTitleLabel");
            capabilities |= Capabilities.Movable;
            
            this.AddManipulator(new Dragger { clampToParentEdges = true });
            RegisterCallback<MouseDownEvent>(OnMouseClick, TrickleDown.TrickleDown);
            style.position = UnityEngine.UIElements.Position.Absolute;
            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.componentBoard, defaultRect));
        }

        public override Rect GetPosition()
        {
            return new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        public override void SetPosition(Rect newPos)
        {
            style.left = newPos.xMin;
            style.top = newPos.yMin;
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
            get { return m_ScrollView.contentContainer; }
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
