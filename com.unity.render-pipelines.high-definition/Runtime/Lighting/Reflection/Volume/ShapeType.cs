namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// The shape of the influence volume
    /// </summary>
    public enum InfluenceShape
    {
        /// <summary>A Box shape.</summary>
        Box,
        /// <summary>A Sphere shape.</summary>
        Sphere,
    }

    /// <summary>
    /// The shape of the proxy volume
    /// </summary>
    public enum ProxyShape
    {
        /// <summary>A Box shape.</summary>
        Box,
        /// <summary>A Sphere shape.</summary>
        Sphere,
        /// <summary>A sphere at the infinity.</summary>
        Infinite
    }
}
