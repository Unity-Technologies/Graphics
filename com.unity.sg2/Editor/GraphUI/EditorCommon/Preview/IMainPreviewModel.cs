namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Class to be implemented by anyone who wants to provide data to the main preview
    /// </summary>
    public interface IMainPreviewModel
    {
        /// <summary>
        /// Implementors should return the unique ID used to identify and generate the preview data associated with them
        /// </summary>
        string GetPreviewListenerID();
    }
}
