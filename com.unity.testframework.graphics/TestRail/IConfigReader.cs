namespace TestRailGraphics.Configuration
{
    /// <summary>
    /// A config reader is required to be supplied. This will probably vary by application so you will need to implement this.
    /// </summary>
    public interface IConfigReader
    {
        string TestRailUser { get; }
        string TestRailPass { get; }

        /// <summary>
        /// Should the "Testrail" middleware be used?
        /// </summary>
        bool TestrailEnabled { get; }

        /// <summary>
        /// Should return any other configuration values you need within your middleware/plugins.
        /// </summary>
        T GetConfigEntry<T>(string entryName);
    }
}