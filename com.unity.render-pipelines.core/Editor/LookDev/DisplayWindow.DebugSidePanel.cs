using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.LookDev
{
    partial class DisplayWindow
    {
        static partial class Style
        {
            internal const string k_DebugViewLabel = "Selected View";
            internal const string k_DebugShadowLabel = "Display Shadows";
            internal const string k_DebugViewMode = "View Mode";

            internal static readonly Texture2D k_LockOpen = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Unlocked", forceLowRes: true);
            internal static readonly Texture2D k_LockClose = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Locked", forceLowRes: true);

            // /!\ WARNING:
            //The following const are used in the uss.
            //If you change them, update the uss file too.
            internal const string k_DebugToolbarName = "debugToolbar";
        }

        MultipleSourceToggle m_Shadow;
        MultipleSourcePopupField m_DebugView;
        List<string> listDebugMode;

        bool cameraSynced
            => LookDev.currentContext.cameraSynced;

        ViewContext lastFocusedViewContext
            => LookDev.currentContext.GetViewContent(LookDev.currentContext.layout.lastFocusedView);

        TargetDebugView targetDebugView
        {
            get => layout.debugPanelSource;
            set
            {
                if (layout.debugPanelSource != value)
                {
                    layout.debugPanelSource = value;
                    UpdateSideDebugPanelProperties();
                }
            }
        }

        bool debugView1SidePanel
            => targetDebugView == TargetDebugView.First
            || targetDebugView == TargetDebugView.Both;

        bool debugView2SidePanel
            => targetDebugView == TargetDebugView.Second
            || targetDebugView == TargetDebugView.Both;

        void ApplyInFilteredViewsContext(Action<ViewContext> action)
        {
            if (debugView1SidePanel)
                action.Invoke(LookDev.currentContext.GetViewContent(ViewIndex.First));
            if (debugView2SidePanel)
                action.Invoke(LookDev.currentContext.GetViewContent(ViewIndex.Second));
        }

        T GetInFilteredViewsContext<T>(Func<ViewContext, T> getter, out bool multipleDifferentValue)
        {
            T res1 = default;
            T res2 = default;
            multipleDifferentValue = false;
            bool view1 = debugView1SidePanel;
            bool view2 = debugView2SidePanel;

            if (view1)
                res1 = getter.Invoke(LookDev.currentContext.GetViewContent(ViewIndex.First));
            if (view2)
                res2 = getter.Invoke(LookDev.currentContext.GetViewContent(ViewIndex.Second));
            if (view1 && view2 && !res1.Equals(res2))
            {
                multipleDifferentValue = true;
                return default;
            }
            else
                return view1 ? res1 : res2;
        }

        void ReadValueFromSourcesWithoutNotify<T, K>(K element, Func<ViewContext, T> from)
            where K : BaseField<T>, IMultipleSource
        {
            bool multipleDifferentValue;
            T newValue = GetInFilteredViewsContext(from, out multipleDifferentValue);
            if (element is MultipleSourcePopupField)
                element.inMultipleValueState = multipleDifferentValue;
            element.SetValueWithoutNotify(newValue);
            element.inMultipleValueState = multipleDifferentValue;
        }

        #region Hack_Support_UIElement_MixedValueState

        class MultipleDifferentValue : TextElement
        {
            public new class UxmlFactory : UxmlFactory<MultipleDifferentValue, UxmlTraits> {}

            public new class UxmlTraits : TextElement.UxmlTraits {}

            public new static readonly string ussClassName = "unity-multipledifferentevalue";

            public MultipleDifferentValue()
            {
                AddToClassList(ussClassName);
                text = "-";
            }
        }

        interface IMultipleSource
        {
            bool inMultipleValueState { get; set; }
        }

        class MultipleSourceToggle : Toggle, IMultipleSource
        {
            MultipleDifferentValue m_MultipleOverlay;
            bool m_InMultipleValueState = false;

            public bool inMultipleValueState
            {
                get => m_InMultipleValueState;
                set
                {
                    if (value != m_InMultipleValueState)
                    {
                        m_MultipleOverlay.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                        m_InMultipleValueState = value;
                    }
                }
            }

            public MultipleSourceToggle() : base()
            {
                m_MultipleOverlay = new MultipleDifferentValue();
                this.Q(name: "unity-checkmark").Add(m_MultipleOverlay);
                m_MultipleOverlay.style.display = DisplayStyle.None;
            }

            public MultipleSourceToggle(string label) : base(label)
            {
                m_MultipleOverlay = new MultipleDifferentValue();
                this.Q(name: "unity-checkmark").Add(m_MultipleOverlay);
                m_MultipleOverlay.style.display = DisplayStyle.None;
            }

            public override void SetValueWithoutNotify(bool newValue)
            {
                if (inMultipleValueState)
                    inMultipleValueState = false;
                base.SetValueWithoutNotify(newValue);
            }

            public override bool value
            {
                get => inMultipleValueState ? default : base.value;
                    set
                    {
                        if (inMultipleValueState)
                            inMultipleValueState = false;
                        base.value = value;
                    }
            }
        }

        class MultipleSourcePopupField : PopupField<string>, IMultipleSource
        {
            internal readonly int count;
            MultipleDifferentValue m_MultipleOverlay;
            bool m_InMultipleValueState = false;

            public bool inMultipleValueState
            {
                get => m_InMultipleValueState;
                set
                {
                    if (value != m_InMultipleValueState)
                    {
                        m_MultipleOverlay.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                        m_InMultipleValueState = value;
                    }
                }
            }

            public MultipleSourcePopupField(string label, List<string> choices, int defaultIndex = 0)
                : base(
                    label,
                    choices,
                    defaultIndex,
                    null,
                    null)
            {
                count = choices.Count;
                m_MultipleOverlay = new MultipleDifferentValue();
                Add(m_MultipleOverlay);
                m_MultipleOverlay.style.display = DisplayStyle.None;
            }

            public override void SetValueWithoutNotify(string newValue)
            {
                // forbid change from not direct selection as it can be find different value at opening
                if (!inMultipleValueState)
                    base.SetValueWithoutNotify(newValue);
            }

            public override string value
            {
                get => inMultipleValueState ? default : base.value;
                    set
                    {
                        //when actively changing in the drop down, quit mixed value state
                        if (inMultipleValueState)
                            inMultipleValueState = false;
                        base.value = value;
                    }
            }
        }

        #endregion

        void CreateDebug()
        {
            if (m_MainContainer == null || m_MainContainer.Equals(null))
                throw new System.MemberAccessException("m_MainContainer should be assigned prior CreateEnvironment()");

            m_DebugContainer = new VisualElement() { name = Style.k_DebugContainerName };
            m_MainContainer.Add(m_DebugContainer);
            if (sidePanel == SidePanel.Debug)
                m_MainContainer.AddToClassList(Style.k_ShowDebugPanelClass);

            AddDebugViewSelector();

            AddDebugShadow();
            AddDebugViewMode();

            //[TODO: finish]
            //Toggle greyBalls = new Toggle("Grey balls");
            //greyBalls.SetValueWithoutNotify(LookDev.currentContext.GetViewContent(LookDev.currentContext.layout.lastFocusedView).debug.greyBalls);
            //greyBalls.RegisterValueChangedCallback(evt =>
            //{
            //    LookDev.currentContext.GetViewContent(LookDev.currentContext.layout.lastFocusedView).debug.greyBalls = evt.newValue;
            //});
            //m_DebugContainer.Add(greyBalls);

            //[TODO: debug why list sometimes empty on resource reloading]
            //[TODO: display only per view]

            if (sidePanel == SidePanel.Debug)
                UpdateSideDebugPanelProperties();
        }

        void AddDebugViewSelector()
        {
            ToolbarRadio viewSelector = new ToolbarRadio()
            {
                name = Style.k_DebugToolbarName
            };
            viewSelector.AddRadios(new[]
            {
                Style.k_Camera1Icon,
                Style.k_LinkIcon,
                Style.k_Camera2Icon
            });
            viewSelector.SetValueWithoutNotify((int)targetDebugView);
            viewSelector.RegisterValueChangedCallback(evt
                => targetDebugView = (TargetDebugView)evt.newValue);
            m_DebugContainer.Add(viewSelector);
        }

        void AddDebugShadow()
        {
            m_Shadow = new MultipleSourceToggle(Style.k_DebugShadowLabel);
            ReadValueFromSourcesWithoutNotify(m_Shadow, view => view.debug.shadow);
            m_Shadow.RegisterValueChangedCallback(evt
                => ApplyInFilteredViewsContext(view => view.debug.shadow = evt.newValue));
            m_DebugContainer.Add(m_Shadow);
        }

        void AddDebugViewMode(int pos = -1)
        {
            //if debugPanel is open on script reload, at this time
            //RenderPipelineManager.currentPipeline is still null.
            //So compute the list on next frame only.
            if (LookDev.dataProvider == null)
            {
                EditorApplication.delayCall += () => AddDebugViewMode(2); //2 = hardcoded position of this field
                return;
            }

            if (m_DebugView != null && m_DebugContainer.Contains(m_DebugView))
                m_DebugContainer.Remove(m_DebugView);

            listDebugMode = new List<string>(LookDev.dataProvider?.supportedDebugModes ?? Enumerable.Empty<string>());
            listDebugMode.Insert(0, "None");
            m_DebugView = new MultipleSourcePopupField(Style.k_DebugViewMode, listDebugMode);
            if (sidePanel == SidePanel.Debug)
                ReadValueFromSourcesWithoutNotify(m_DebugView, view => listDebugMode[view.debug.viewMode + 1]);
            m_DebugView.RegisterValueChangedCallback(evt
                => ApplyInFilteredViewsContext(view => view.debug.viewMode = listDebugMode.IndexOf(evt.newValue) - 1));
            if (pos == -1)
                m_DebugContainer.Add(m_DebugView);
            else
                m_DebugContainer.Insert(pos, m_DebugView);
        }

        void UpdateSideDebugPanelProperties()
        {
            ReadValueFromSourcesWithoutNotify(m_Shadow, view => view.debug.shadow);
            if (m_DebugView != null)
                ReadValueFromSourcesWithoutNotify(m_DebugView, view => listDebugMode[view.debug.viewMode + 1]);
        }
    }
}
