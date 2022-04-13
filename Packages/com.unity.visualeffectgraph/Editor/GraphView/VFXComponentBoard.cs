using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Experimental;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditor.VFX.UIElements;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    static class BoardPreferenceHelper
    {
        public enum Board
        {
            blackboard,
            componentBoard
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
                    if (float.TryParse(rectValues[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                        float.TryParse(rectValues[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                        float.TryParse(rectValues[2], NumberStyles.Float, CultureInfo.InvariantCulture, out width) &&
                        float.TryParse(rectValues[3], NumberStyles.Float, CultureInfo.InvariantCulture, out height))
                    {
                        blackBoardPosition = new Rect(x, y, width, height);
                    }
                }
            }

            return blackBoardPosition;
        }

        public static void SavePosition(Board board, Rect r)
        {
            EditorPrefs.SetString(string.Format(rectPreferenceFormat, board), string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", r.x, r.y, r.width, r.height));
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


    class VFXComponentBoard : GraphElement, IControlledElement<VFXViewController>, IVFXMovable, IVFXResizable
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

        VFXView m_View;
        VFXUIDebug m_DebugUI;

        public VFXComponentBoard(VFXView view)
        {
            m_View = view;
            var tpl = VFXView.LoadUXML("VFXComponentBoard");

            tpl.CloneTree(contentContainer);

            contentContainer.AddStyleSheetPath("VFXComponentBoard");

            m_RootElement = this.Query<VisualElement>("component-container");
            m_SubtitleIcon = this.Query<Image>("subTitle-icon");
            m_Subtitle = this.Query<Label>("subTitleLabel");
            m_SubtitleIcon.image = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "console.warnicon.sml.png");

            m_Stop = this.Query<Button>("stop");
            m_Stop.clickable.clicked += EffectStop;
            m_Play = this.Query<Button>("play");
            m_Play.clickable.clicked += EffectPlay;
            m_Step = this.Query<Button>("step");
            m_Step.clickable.clicked += EffectStep;
            m_Restart = this.Query<Button>("restart");
            m_Restart.clickable.clicked += EffectRestart;

            m_PlayRateSlider = this.Query<Slider>("play-rate-slider");
            m_PlayRateSlider.lowValue = Mathf.Pow(VisualEffectControl.minSlider, 1 / VisualEffectControl.sliderPower);
            m_PlayRateSlider.highValue = Mathf.Pow(VisualEffectControl.maxSlider, 1 / VisualEffectControl.sliderPower);
            m_PlayRateSlider.RegisterValueChangedCallback(evt => OnEffectSlider(evt.newValue));
            m_PlayRateField = this.Query<IntegerField>("play-rate-field");
            m_PlayRateField.RegisterCallback<ChangeEvent<int>>(OnPlayRateField);

            m_PlayRateMenu = this.Query<Button>("play-rate-menu");
            m_PlayRateMenu.AddStyleSheetPathWithSkinVariant("VFXControls");

            m_PlayRateMenu.clickable.clicked += OnPlayRateMenu;

            m_ParticleCount = this.Query<Label>("particle-count");

            Button button = this.Query<Button>("on-play-button");
            button.clickable.clicked += () => SendEvent(VisualEffectAsset.PlayEventName);
            button = this.Query<Button>("on-stop-button");
            button.clickable.clicked += () => SendEvent(VisualEffectAsset.StopEventName);

            m_EventsContainer = this.Query("events-container");

            m_DebugModes = this.Query<Button>("debug-modes");
            m_DebugModes.clickable.clicked += OnDebugModes;

            m_RecordBoundsButton = this.Query<Button>("record");
            m_RecordBoundsImage = this.Query<Image>("record-icon");
            m_RecordBoundsButton.clickable.clicked += OnRecordBoundsButton;
            m_RecordIcon = VFXView.LoadImage("d_Record");
            m_BoundsActionLabel = this.Query<Label>("bounds-label");
            m_BoundsToolContainer = this.Query("bounds-tool-container");
            m_BackgroundDefaultColor = m_BoundsToolContainer.style.backgroundColor;
            m_SystemBoundsContainer = this.Query<VFXBoundsSelector>("system-bounds-container");
            m_SystemBoundsContainer.RegisterCallback<MouseDownEvent>(OnMouseClickBoundsContainer);

            m_ApplyBoundsButton = this.Query<Button>("apply-bounds-button");
            m_ApplyBoundsButton.clickable.clicked += ApplyCurrentBounds;

            Detach();
            this.AddManipulator(new Dragger { clampToParentEdges = true });

            capabilities |= Capabilities.Movable;

            RegisterCallback<MouseDownEvent>(OnMouseClick);

            style.position = PositionType.Absolute;

            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.componentBoard, defaultRect));
        }

        public void ValidatePosition()
        {
            BoardPreferenceHelper.ValidatePosition(this, m_View, defaultRect);
        }

        static readonly Rect defaultRect = new Rect(200, 100, 300, 300);

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

        void OnMouseClick(MouseDownEvent e)
        {
            m_View.SetBoardToFront(this);
        }

        void OnMouseClickBoundsContainer(MouseDownEvent e)
        {
            if (e.button == (int)MouseButton.LeftMouse)
            {
                bool needClearSelection = false;
                foreach (var elem in m_SystemBoundsContainer.Children())
                {
                    var systemBound = elem as VFXComponentBoardBoundsSystemUI;
                    if (systemBound != null)
                        needClearSelection |= systemBound.Unselect();
                }
            }
        }

        void OnPlayRateMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (var value in VisualEffectControl.setPlaybackValues)
            {
                menu.AddItem(EditorGUIUtility.TextContent(string.Format("{0}%", value)), false, SetPlayRate, value);
            }
            menu.DropDown(m_PlayRateMenu.worldBound);
        }

        void OnPlayRateField(ChangeEvent<int> e)
        {
            SetPlayRate(e.newValue);
        }

        void SetPlayRate(object value)
        {
            if (m_AttachedComponent == null)
                return;
            float rate = (float)((int)value) * VisualEffectControl.valueToPlayRate;
            m_AttachedComponent.playRate = rate;
            UpdatePlayRate();
        }

        void OnDebugModes()
        {
            GenericMenu menu = new GenericMenu();
            foreach (VFXUIDebug.Modes mode in Enum.GetValues(typeof(VFXUIDebug.Modes)))
            {
                menu.AddItem(EditorGUIUtility.TextContent(mode.ToString()), false, SetDebugMode, mode);
            }
            menu.DropDown(m_DebugModes.worldBound);
        }

        void SetDebugMode(object mode)
        {
            m_DebugUI.SetDebugMode((VFXUIDebug.Modes)mode, this);
        }

        private VFXBoundsRecorder m_BoundsRecorder;
        void OnRecordBoundsButton()
        {
            if (m_BoundsRecorder != null)
            {
                m_BoundsRecorder.ToggleRecording();
            }
            UpdateRecordingButton();
        }

        void UpdateRecordingButton()
        {
            bool hasSomethingToRecord = m_BoundsRecorder != null && m_BoundsRecorder.NeedsAnyToBeRecorded();
            m_RecordBoundsButton.SetEnabled(hasSomethingToRecord);

            if (hasSomethingToRecord && m_BoundsRecorder.isRecording)
            {
                float remainder = Time.realtimeSinceStartup % 1.0f;
                if (remainder < 0.22f)
                {
                    m_RecordBoundsImage.style.backgroundImage = null;
                }
                else
                {
                    m_RecordBoundsImage.style.backgroundImage = m_RecordIcon;
                }

                m_BoundsToolContainer.style.backgroundColor = m_BackgroundRecordingColor;
                m_BoundsActionLabel.text = "Recording in progress...";
            }
            else
            {
                m_RecordBoundsImage.style.backgroundImage = m_RecordIcon;
                m_BoundsToolContainer.style.backgroundColor = m_BackgroundDefaultColor;
                m_BoundsActionLabel.text = "Bounds Recording";
            }
            if (!hasSomethingToRecord && m_BoundsRecorder.isRecording)
                m_BoundsRecorder.ToggleRecording();
        }

        public void DeactivateBoundsRecordingIfNeeded()
        {
            if (m_BoundsRecorder != null && m_BoundsRecorder.isRecording)
                m_BoundsRecorder.ToggleRecording();
        }

        void ApplyCurrentBounds()
        {
            if (m_View.IsAssetEditable())
                m_BoundsRecorder.ApplyCurrentBounds();
        }

        void DeleteBoundsRecorder()
        {
            if (m_BoundsRecorder != null)
            {
                m_BoundsRecorder.isRecording = false;
                m_BoundsRecorder = null;
            }
        }

        void UpdateBoundsRecorder()
        {
            if (m_AttachedComponent != null && m_View.controller.graph != null)
            {
                controller.RecompileExpressionGraphIfNeeded();
                bool wasRecording = false;
                if (m_BoundsRecorder != null)
                {
                    wasRecording = m_BoundsRecorder.isRecording;
                    m_BoundsRecorder.CleanUp();
                }
                m_BoundsRecorder = new VFXBoundsRecorder(m_AttachedComponent, m_View);
                if (wasRecording && !m_View.controller.isReentrant) //If this is called during an Undo/Redo, toggling the recording will cause a reentrant invalidation
                {
                    m_BoundsRecorder.ToggleRecording();
                }
                var systemNames = m_BoundsRecorder.systemNames;
                if (m_SystemBoundsContainer != null)
                {
                    foreach (var elem in m_SystemBoundsContainer.Children())
                    {
                        if (elem is VFXComponentBoardBoundsSystemUI)
                        {
                            (elem as VFXComponentBoardBoundsSystemUI).ReleaseBoundsRecorder();
                        }
                    }
                    m_SystemBoundsContainer.Clear();
                    m_SystemBoundsContainer.AddStyleSheetPath("VFXComponentBoard-bounds-list");
                }
                foreach (var system in systemNames)
                {
                    var tpl = VFXView.LoadUXML("VFXComponentBoard-bounds-list");
                    tpl.CloneTree(m_SystemBoundsContainer);
                    VFXComponentBoardBoundsSystemUI newUI = m_SystemBoundsContainer.Children().Last() as VFXComponentBoardBoundsSystemUI;
                    if (newUI != null)
                    {
                        newUI.Setup(system, m_BoundsRecorder);
                    }
                }
            }
        }

        void OnEffectSlider(float f)
        {
            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.playRate = VisualEffectControl.valueToPlayRate * Mathf.Pow(f, VisualEffectControl.sliderPower);
                UpdatePlayRate();
            }
        }

        void EffectStop()
        {
            if (m_AttachedComponent != null)
                m_AttachedComponent.ControlStop();
            if (m_DebugUI != null)
                m_DebugUI.Notify(VFXUIDebug.Events.VFXStop);
        }

        void EffectPlay()
        {
            if (m_AttachedComponent != null)
                m_AttachedComponent.ControlPlayPause();
            if (m_DebugUI != null)
                m_DebugUI.Notify(VFXUIDebug.Events.VFXPlayPause);
        }

        void EffectStep()
        {
            if (m_AttachedComponent != null)
                m_AttachedComponent.ControlStep();
            if (m_DebugUI != null)
                m_DebugUI.Notify(VFXUIDebug.Events.VFXStep);
        }

        void EffectRestart()
        {
            if (m_AttachedComponent != null)
                m_AttachedComponent.ControlRestart();
            if (m_DebugUI != null)
                m_DebugUI.Notify(VFXUIDebug.Events.VFXReset);
        }

        public void OnVisualEffectComponentChanged(IEnumerable<VisualEffect> visualEffects)
        {
            if (m_AttachedComponent != null
                && visualEffects.Contains(m_AttachedComponent)
                && m_AttachedComponent.visualEffectAsset != controller.graph.visualEffectResource.asset)
            {
                //The Visual Effect Asset has been changed and is no longer valid, we don't want to modify capacity on the wrong graph. We have to detach.
                m_View.attachedComponent = null;
            }
        }

        VisualEffect m_AttachedComponent;

        public VisualEffect GetAttachedComponent()
        {
            return m_AttachedComponent;
        }

        bool m_LastKnownPauseState;
        void UpdatePlayButton()
        {
            if (m_AttachedComponent == null)
                return;

            if (m_LastKnownPauseState != m_AttachedComponent.pause)
            {
                m_LastKnownPauseState = m_AttachedComponent.pause;
                if (m_LastKnownPauseState)
                {
                    m_Play.AddToClassList("paused");
                }
                else
                {
                    m_Play.RemoveFromClassList("paused");
                }
            }
        }

        public void Detach()
        {
            m_RootElement.SetEnabled(false);
            m_Subtitle.text = "Select a Game Object running this VFX";
            m_SubtitleIcon.style.display = DisplayStyle.Flex;

            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.playRate = 1;
                m_AttachedComponent.pause = false;
            }
            m_AttachedComponent = null;
            if (m_UpdateItem != null)
            {
                m_UpdateItem.Pause();
            }
            if (m_EventsContainer != null)
                m_EventsContainer.Clear();
            m_Events.Clear();
            if (m_DebugUI != null)
            {
                m_DebugUI.SetDebugMode(VFXUIDebug.Modes.None, this, true);
            }

            DeleteBoundsRecorder();
            RefreshInitializeErrors();
        }

        public void RefreshInitializeErrors()
        {
            var viewContexts = m_View.GetAllContexts();
            List<VFXContextUI> contextsToRefresh = new List<VFXContextUI>();
            foreach (var context in viewContexts)
            {
                if (context.controller.model is VFXBasicInitialize)
                {
                    contextsToRefresh.Add(context);
                }
            }

            foreach (var context in contextsToRefresh)
            {
                context.controller.model.RefreshErrors(m_View.controller.graph);
            }
        }

        public bool Attach(VisualEffect effect = null)
        {
            VisualEffect target = effect != null ? effect : Selection.activeGameObject?.GetComponent<VisualEffect>();
            if (target != null && m_View.controller?.graph != null && m_AttachedComponent != target)
            {
                if (m_AttachedComponent != null)
                {
                    m_AttachedComponent.playRate = 1;
                }

                m_AttachedComponent = target;
                m_Subtitle.text = m_AttachedComponent.name;
                m_LastKnownPauseState = !m_AttachedComponent.pause;
                m_AttachedComponent.playRate = m_LastKnownPlayRate >= 0 ? m_LastKnownPlayRate : 1;

                UpdatePlayButton();

                if (m_UpdateItem == null)
                    m_UpdateItem = schedule.Execute(Update).Every(100);
                else
                    m_UpdateItem.Resume();
                UpdateEventList();

                var debugMode = VFXUIDebug.Modes.None;
                if (m_DebugUI != null)
                {
                    debugMode = m_DebugUI.GetDebugMode();
                    m_DebugUI.Clear();
                }

                m_DebugUI = new VFXUIDebug(m_View);
                m_DebugUI.SetVisualEffect(m_AttachedComponent);
                m_DebugUI.SetDebugMode(debugMode, this, true);

                m_RootElement.SetEnabled(true);
                m_SubtitleIcon.style.display = DisplayStyle.None;
                UpdateBoundsRecorder();
                UpdateRecordingButton();
                RefreshInitializeErrors();

                return true;
            }

            return false;
        }

        public void SendEvent(string name)
        {
            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.SendEvent(name);
            }
        }

        IVisualElementScheduledItem m_UpdateItem;


        float m_LastKnownPlayRate = -1;


        int m_LastKnownParticleCount = -1;

        void Update()
        {
            if (m_AttachedComponent == null || controller == null)
            {
                Detach();
                return;
            }

            string path = m_AttachedComponent.name;

            UnityEngine.Transform current = m_AttachedComponent.transform.parent;
            while (current != null)
            {
                path = current.name + " > " + path;
                current = current.parent;
            }

            if (EditorSceneManager.loadedSceneCount > 1)
            {
                path = m_AttachedComponent.gameObject.scene.name + " : " + path;
            }

            if (m_Subtitle.text != path)
                m_Subtitle.text = path;

            if (m_ParticleCount != null)
            {
                int newParticleCount = 0;//m_AttachedComponent.aliveParticleCount
                if (m_LastKnownParticleCount != newParticleCount)
                {
                    m_LastKnownParticleCount = newParticleCount;
                    m_ParticleCount.text = m_LastKnownParticleCount.ToString();
                }
            }

            UpdatePlayRate();
            UpdatePlayButton();
            UpdateBoundsModes();
            m_ApplyBoundsButton.SetEnabled(m_BoundsRecorder.bounds.Any() && m_View.IsAssetEditable());
            UpdateRecordingButton();
        }

        void UpdatePlayRate()
        {
            if (Math.Abs(m_LastKnownPlayRate - m_AttachedComponent.playRate) > 1e-4)
            {
                m_LastKnownPlayRate = m_AttachedComponent.playRate;
                SetPlayrateSlider(m_AttachedComponent.playRate);
            }
        }

        void SetPlayrateSlider(float value)
        {
            float playRateValue = value * VisualEffectControl.playRateToValue;
            m_PlayRateSlider.value = Mathf.Pow(playRateValue, 1 / VisualEffectControl.sliderPower);
            if (m_PlayRateField != null && !m_PlayRateField.HasFocus())
                m_PlayRateField.value = Mathf.RoundToInt(playRateValue);
        }

        void ToggleAttach()
        {
            if (!object.ReferenceEquals(m_AttachedComponent, null))
            {
                Detach();
            }
            else
            {
                Attach();
            }
        }

        void Select()
        {
            if (m_AttachedComponent != null)
            {
                Selection.activeObject = m_AttachedComponent;
            }
        }

        VisualElement m_EventsContainer;
        VisualElement m_RootElement;

        Label m_Subtitle;
        Image m_SubtitleIcon;
        Button m_Stop;
        Button m_Play;
        Button m_Step;
        Button m_Restart;
        Slider m_PlayRateSlider;
        IntegerField m_PlayRateField;

        Button m_PlayRateMenu;
        Button m_DebugModes;

        Button m_RecordBoundsButton;
        Image m_RecordBoundsImage;
        Texture2D m_RecordIcon;
        Button m_ApplyBoundsButton;
        VFXBoundsSelector m_SystemBoundsContainer;
        VisualElement m_BoundsToolContainer;
        Label m_BoundsActionLabel;
        StyleColor m_BackgroundRecordingColor = new StyleColor(new Color(0.325f, 0.125f, 0.125f));
        StyleColor m_BackgroundDefaultColor;

        Label m_ParticleCount;

        public new void Clear()
        {
            Detach();
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            UpdateEventList();
            if (e.change != VFXViewController.Change.ui)
                UpdateBoundsRecorder();
        }

        static readonly string[] staticEventNames = new string[] { VisualEffectAsset.PlayEventName, VisualEffectAsset.StopEventName };


        static bool IsDefaultEvent(string evt)
        {
            return evt == VisualEffectAsset.PlayEventName || evt == VisualEffectAsset.StopEventName;
        }

        IEnumerable<string> GetEventNames()
        {
            return controller?.contexts.SelectMany(x => this.RecurseGetEventNames(x.model)) ?? Enumerable.Empty<string>();
        }

        IEnumerable<string> RecurseGetEventNames(VFXContext context)
        {
            switch (context)
            {
                case VFXBasicEvent basicEvent when !IsDefaultEvent(name):
                    yield return basicEvent.eventName;
                    break;
                case VFXSubgraphContext subgraphContext when subgraphContext.subChildren != null:
                {
                    foreach (var eventName in subgraphContext.subChildren.OfType<VFXContext>().SelectMany(RecurseGetEventNames))
                    {
                        yield return eventName;
                    }
                    break;
                }
            }
        }

        public void UpdateEventList()
        {
            if (m_AttachedComponent == null)
            {
                if (m_EventsContainer != null)
                    m_EventsContainer.Clear();
                m_Events.Clear();
            }
            else
            {
                var eventNames = GetEventNames().ToArray();

                foreach (var removed in m_Events.Keys.Except(eventNames).ToArray())
                {
                    var ui = m_Events[removed];
                    m_EventsContainer.Remove(ui);
                    m_Events.Remove(removed);
                }

                foreach (var added in eventNames.Except(m_Events.Keys).ToArray())
                {
                    var tpl = VFXView.LoadUXML("VFXComponentBoard-event");

                    tpl.CloneTree(m_EventsContainer);

                    VFXComponentBoardEventUI newUI = m_EventsContainer.Children().Last() as VFXComponentBoardEventUI;
                    if (newUI != null)
                    {
                        newUI.Setup();
                        newUI.name = added;
                        m_Events.Add(added, newUI);
                    }
                }

                if (!m_Events.Values.Any(t => t.nameHasFocus))
                {
                    SortEventList();
                }
            }
        }

        internal void ResetPlayRate()
        {
            m_LastKnownPlayRate = -1f;
            SetPlayrateSlider(1f);
        }

        void SortEventList()
        {
            var eventNames = m_Events.Keys.OrderBy(t => t);
            //Sort events
            VFXComponentBoardEventUI prev = null;
            foreach (var eventName in eventNames)
            {
                VFXComponentBoardEventUI current = m_Events[eventName];
                if (current != null)
                {
                    if (prev == null)
                    {
                        current.SendToBack();
                    }
                    else
                    {
                        current.PlaceInFront(prev);
                    }
                    prev = current;
                }
            }
        }

        void UpdateBoundsModes()
        {
            bool systemNamesChanged = false;
            foreach (var elem in m_SystemBoundsContainer.Children())
            {
                VFXComponentBoardBoundsSystemUI boundsModeElem = elem as VFXComponentBoardBoundsSystemUI;
                if (boundsModeElem != null)
                {
                    if (boundsModeElem.HasSystemBeenRenamed())
                    {
                        systemNamesChanged = true;
                        break;
                    }
                    boundsModeElem.UpdateLabel();
                }
            }
            if (systemNamesChanged)
                UpdateBoundsRecorder();
        }

        Dictionary<string, VFXComponentBoardEventUI> m_Events = new Dictionary<string, VFXComponentBoardEventUI>();

        public override void UpdatePresenterPosition()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.componentBoard, GetPosition());
        }

        public void OnMoved()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.componentBoard, GetPosition());
        }

        void IVFXResizable.OnStartResize() { }
        public void OnResized()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.componentBoard, GetPosition());
        }
    }
    class VFXComponentBoardEventUIFactory : UxmlFactory<VFXComponentBoardEventUI>
    { }
    class VFXComponentBoardEventUI : VisualElement
    {
        public VFXComponentBoardEventUI()
        {
        }

        public void Setup()
        {
            m_EventName = this.Query<TextField>("event-name");
            m_EventName.isDelayed = true;
            m_EventName.RegisterCallback<ChangeEvent<string>>(OnChangeName);
            m_EventSend = this.Query<Button>("event-send");
            m_EventSend.clickable.clicked += OnSend;
        }

        void OnChangeName(ChangeEvent<string> e)
        {
            var board = GetFirstAncestorOfType<VFXComponentBoard>();
            if (board != null)
            {
                board.controller.ChangeEventName(m_Name, e.newValue);
            }
        }

        public bool nameHasFocus
        {
            get { return m_EventName.HasFocus(); }
        }

        public new string name
        {
            get
            {
                return m_Name;
            }

            set
            {
                m_Name = value;
                if (m_EventName != null)
                {
                    if (!m_EventName.HasFocus())
                        m_EventName.SetValueWithoutNotify(m_Name);
                }
            }
        }

        string m_Name;
        TextField m_EventName;
        Button m_EventSend;

        void OnSend()
        {
            var board = GetFirstAncestorOfType<VFXComponentBoard>();
            if (board != null)
            {
                board.SendEvent(m_Name);
            }
        }
    }

    class VFXComponentBoardBoundsSystemUIFactory : UxmlFactory<VFXComponentBoardBoundsSystemUI>
    { }

    class VFXComponentBoardBoundsSystemUI : VisualElement
    {
        public VFXComponentBoardBoundsSystemUI()
        {
        }

        ~VFXComponentBoardBoundsSystemUI()
        {
            if (m_BoundsRecorder != null)
            {
                m_BoundsRecorder = null;
            }
        }

        public void Setup(string systemName, VFXBoundsRecorder boundsRecorder)
        {
            m_BoundsRecorder = boundsRecorder;
            m_CurrentMode = m_BoundsRecorder.GetSystemBoundsSettingMode(systemName);
            m_SystemName = systemName;
            m_SystemNameButton = this.Query<VFXBoundsRecorderField>("system-field");
            var initContextUI = m_BoundsRecorder.GetInitContextController(m_SystemName);
            m_SystemNameButton.Setup(initContextUI, m_BoundsRecorder.view);
            m_SystemNameButton.text = m_SystemName;
            InitBoundsModeElement();
            m_Colors = new Dictionary<string, StyleColor>()
            {
                {"included", m_SystemNameButton.style.color},
                {"excluded", new StyleColor(Color.gray * 0.8f) }
            };

            if (!m_BoundsRecorder.NeedsToBeRecorded(m_SystemName, out VFXBoundsRecorder.ExclusionCause cause))
            {
                m_SystemNameButton.text = $"{m_SystemName} {VFXBoundsRecorder.exclusionCauseString[cause]}";
                m_SystemNameButton.tooltip =
                    $"This system will not be taken into account in the recording because {VFXBoundsRecorder.exclusionCauseTooltip[cause]}";
                m_SystemNameButton.style.color = m_Colors["excluded"];
                m_SystemNameButton.SetEnabled(false);
            }
        }

        void InitBoundsModeElement()
        {
            m_BoundsMode = new VFXEnumField(s_EmptyEnumLabel, typeof(BoundsSettingMode));
            m_BoundsMode.OnValueChanged += OnValueChanged;
            m_BoundsMode.SetValue((int)m_CurrentMode);
            m_BoundsMode.AddToClassList("bounds-mode");
            Add(m_BoundsMode);
        }

        private List<string> m_BoundsModes = new List<string> { "Manual", "Recorded", "Automatic" };

        public void UpdateLabel()
        {
            m_CurrentMode = m_BoundsRecorder.GetSystemBoundsSettingMode(m_SystemName);
            m_BoundsMode.SetValue((int)m_CurrentMode);
            OnValueChanged();
            if (!m_BoundsRecorder.NeedsToBeRecorded(m_SystemName, out VFXBoundsRecorder.ExclusionCause cause))
            {
                m_SystemNameButton.text = $"{m_SystemName} {VFXBoundsRecorder.exclusionCauseString[cause]}";
                m_SystemNameButton.tooltip =
                    $"This system will not be taken into account in the recording because {VFXBoundsRecorder.exclusionCauseTooltip[cause]}";
                m_SystemNameButton.style.color = m_Colors["excluded"];
                m_SystemNameButton.SetEnabled(false);
            }
            else
            {
                m_SystemNameButton.text = m_SystemName;
                m_SystemNameButton.tooltip = "";
                m_SystemNameButton.SetEnabled(true);
                m_SystemNameButton.style.color = m_Colors["included"];
            }
        }

        public bool HasSystemBeenRenamed()
        {
            return !m_BoundsRecorder.systemNames.Contains(m_SystemName);
        }

        void SetSystemBoundMode(object mode)
        {
            m_CurrentMode = (BoundsSettingMode)mode;
            m_BoundsMode.SetValue((int)mode);
            m_BoundsRecorder.ModifyMode(m_SystemName, (BoundsSettingMode)mode);
        }

        void OnValueChanged()
        {
            if (m_CurrentMode != (BoundsSettingMode)m_BoundsMode.value)
            {
                m_CurrentMode = (BoundsSettingMode)m_BoundsMode.value;
                m_BoundsRecorder.ModifyMode(m_SystemName, m_CurrentMode);
            }
        }

        public void ReleaseBoundsRecorder()
        {
            m_BoundsRecorder.isRecording = false;
            m_BoundsRecorder = null;
        }

        public bool Unselect()
        {
            return m_SystemNameButton.Unselect();
        }

        string m_SystemName;
        VFXBoundsRecorderField m_SystemNameButton;
        VFXEnumField m_BoundsMode;
        BoundsSettingMode m_CurrentMode;
        VFXBoundsRecorder m_BoundsRecorder;
        Dictionary<string, StyleColor> m_Colors;
        private static Label s_EmptyEnumLabel = new Label();

        static class BoundsSystemContents
        {
            public static Dictionary<BoundsSettingMode, GUIContent> modesContent =
                new Dictionary<BoundsSettingMode, GUIContent>()
            {
                {
                    BoundsSettingMode.Automatic,
                    new GUIContent(BoundsSettingMode.Automatic.ToString(),
                        "Systems with the Automatic bounds setting will not be affected by the recording.")
                },
                {
                    BoundsSettingMode.Manual,
                    new GUIContent(BoundsSettingMode.Manual.ToString(),
                        "Systems with the Manual bounds setting will not be affected by the recording.")
                },
                {
                    BoundsSettingMode.Recorded,
                    new GUIContent(BoundsSettingMode.Recorded.ToString(),
                        "")
                },
            };
        }
    }
}
