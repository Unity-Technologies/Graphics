namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Customize the content of a toolbar (non-overlay ones).
    /// </summary>
    public interface IToolbarProvider
    {
        /// <summary>
        /// Determines whether the button should be displayed in the toolbar.
        /// </summary>
        /// <param name="buttonName">The name of the button.</param>
        /// <returns>True if the button should be displayed in the toolbar.</returns>
        bool ShowButton(string buttonName);
    }
}
