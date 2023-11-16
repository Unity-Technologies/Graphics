using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public partial class RenderGraphViewer
    {
        enum DisplayState
        {
            Empty,
            Populated
        }

        abstract class OverlayBase : Overlay
        {
            const string kContentContainerName = "content-container";
            const string kEmptyContentsMessageName = "empty-contents-message";

            protected VisualElement root;

            protected bool FindMainScrollView(out ScrollView scrollView)
            {
                scrollView = root?.Q<ScrollView>(kContentContainerName);
                return scrollView != null;
            }

            protected bool FindEmptyContentsMessage(out VisualElement element)
            {
                 element = root?.Q<VisualElement>(kEmptyContentsMessageName);
                 return element != null;
            }

            protected void Init(string title)
            {
                displayName = title;
                defaultSize = new Vector2(300, 300);
                minSize = new Vector2(100, 100);
                maxSize = new Vector2(1500, 1500);
            }

            protected void SetDisplayState(DisplayState state)
            {
                if (FindMainScrollView(out var mainScrollView))
                    mainScrollView.style.display = state == DisplayState.Empty ? DisplayStyle.None : DisplayStyle.Flex;

                if (FindEmptyContentsMessage(out var emptyContentsMessage))
                    emptyContentsMessage.style.display =
                        state == DisplayState.Empty ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
