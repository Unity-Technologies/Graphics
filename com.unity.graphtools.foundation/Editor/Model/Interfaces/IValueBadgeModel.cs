namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for values displayed in badges.
    /// </summary>
    public interface IValueBadgeModel : IBadgeModel
    {
        /// <summary>
        /// The port to which the value is associated.
        /// </summary>
        IPortModel ParentPortModel { get; }

        /// <summary>
        /// The value to display.
        /// </summary>
        string DisplayValue { get; }
    }
}
