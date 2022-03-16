using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IBaseModelView
    {
        /// <summary>
        /// Instantiates and initializes the VisualElements that makes the UI.
        /// </summary>
        void BuildUI();

        /// <summary>
        /// Updates the UI using data from the model.
        /// </summary>
        void UpdateFromModel();
    }
}
