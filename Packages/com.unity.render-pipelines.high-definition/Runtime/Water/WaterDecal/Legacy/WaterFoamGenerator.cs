using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Controls the type of the procedural foam generator.
    /// </summary>
    [Obsolete("WaterFoamGenerator has been deprecated. Use WaterDecal instead.")]
    public enum WaterFoamGeneratorType
    {
        /// <summary>
        /// Disk foam generator.
        /// </summary>
        Disk = 0,
        /// <summary>
        /// Square foam generator.
        /// </summary>
        Rectangle = 1,
        /// <summary>
        /// Texture foam generator.
        /// </summary>
        Texture = 2,
        /// <summary>
        /// Material foam generator.
        /// </summary>
        Material = 3,
    }

    /// <summary>
    /// Procedural water foam generator component.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [AddComponentMenu("")] // Hide in menu
    public partial class WaterFoamGenerator : WaterDecal
    {
        /// <summary>
        /// Specifies the type of the generator. This parameter defines which parameters will be used to render it.
        /// </summary>
        [Obsolete("WaterFoamGenerator has been deprecated. Use WaterDecal instead.")]
        public WaterFoamGeneratorType type = WaterFoamGeneratorType.Disk;

        /// <summary>
        /// Specifies the texture used for the foam.
        /// </summary>
        [Obsolete("WaterFoamGenerator has been deprecated. Use WaterDecal instead.")]
        public Texture texture = null;

        private void Awake()
        {
            k_Migration.Migrate(this);
        }
    }
}
