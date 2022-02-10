using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UIToolkit event sent to ask for the searcher to be displayed.
    /// </summary>
    public class PromptSearcherEvent : EventBase<PromptSearcherEvent>
    {
        /// <summary>
        /// The location where the searcher should be displayed.
        /// </summary>
        public Vector2 MenuPosition;

        /// <summary>
        /// Gets a PromptSearcherEvent from the pool of events and initializes it.
        /// </summary>
        /// <param name="menuPosition">The location where the searcher should be displayed.</param>
        /// <returns>A freshly initialized event.</returns>
        public static PromptSearcherEvent GetPooled(Vector2 menuPosition)
        {
            var e = GetPooled();
            e.MenuPosition = menuPosition;
            return e;
        }

        /// <summary>
        /// Initializes the event.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            this.SetEventPropagationToNormal();
        }
    }
}
