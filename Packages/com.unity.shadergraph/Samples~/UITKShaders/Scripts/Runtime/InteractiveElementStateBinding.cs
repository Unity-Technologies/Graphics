using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Shaders.UITK.Sample
{
    /// <summary>
    /// Manipulator that writes the element's interactive state (Normal, Hover, Pressed,
    /// Selected, Disabled) into the <c>_State</c> float shader property. Numbering matches
    /// the UGUI <c>Selectable.SelectionState</c> enum so example shaders can be shared:
    /// Normal=0, Hover=1, Pressed=2, Selected=3, Disabled=4.
    /// </summary>
    public sealed class InteractiveElementStateBinding : Manipulator
    {
        public enum State
        {
            Normal = 0,
            Hover = 1,
            Pressed = 2,
            Selected = 3,
            Disabled = 4,
        }

        const string k_StateProperty = "_State";

        readonly Material m_FallbackMaterial;
        bool m_IsHovered;
        bool m_IsPressed;
        bool m_IsFocused;
        State m_LastWritten = (State)(-1);
        IVisualElementScheduledItem m_EnabledPoll;

        public InteractiveElementStateBinding() : this(null) {}

        public InteractiveElementStateBinding(Material fallbackMaterial)
        {
            m_FallbackMaterial = fallbackMaterial;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<FocusInEvent>(OnFocusIn);
            target.RegisterCallback<FocusOutEvent>(OnFocusOut);
            target.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            // UI Toolkit has no event for programmatic SetEnabled, so a slow re-check catches
            // enabled-state changes that arrive without pointer or focus traffic. The write is
            // change-guarded, so the poll costs a comparison per tick, not a material write.
            m_EnabledPoll = target.schedule.Execute(UpdateState).Every(250);

            UpdateState();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<FocusInEvent>(OnFocusIn);
            target.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            target.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            m_EnabledPoll?.Pause();
            m_EnabledPoll = null;
        }

        void OnAttachToPanel(AttachToPanelEvent evt) => UpdateState();
        void OnPointerEnter(PointerEnterEvent evt) { m_IsHovered = true; UpdateState(); }
        void OnPointerLeave(PointerLeaveEvent evt) { m_IsHovered = false; m_IsPressed = false; UpdateState(); }
        void OnPointerDown(PointerDownEvent evt) { m_IsPressed = true; UpdateState(); }
        void OnPointerUp(PointerUpEvent evt) { m_IsPressed = false; UpdateState(); }
        void OnFocusIn(FocusInEvent evt) { m_IsFocused = true; UpdateState(); }
        void OnFocusOut(FocusOutEvent evt) { m_IsFocused = false; UpdateState(); }

        void UpdateState()
        {
            State state;
            if (!target.enabledInHierarchy)
                state = State.Disabled;
            else if (m_IsPressed)
                state = State.Pressed;
            else if (m_IsHovered)
                state = State.Hover;
            else if (m_IsFocused)
                state = State.Selected;
            else
                state = State.Normal;

            if (state == m_LastWritten)
                return;
            m_LastWritten = state;
            UIShaderBinding.SetFloat(target, m_FallbackMaterial, k_StateProperty, (float)state);
        }
    }
}
