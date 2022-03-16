using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Adds a text to a class that is used by the searcher to display help text in the details panel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SeacherHelpAttribute : Attribute
    {
        /// <summary>
        /// Help text related to the class tagged by the attribute.
        /// </summary>
        public string HelpText { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeacherHelpAttribute"/> class.
        /// </summary>
        public SeacherHelpAttribute(string helpText)
        {
            HelpText = helpText;
        }
    }
}
