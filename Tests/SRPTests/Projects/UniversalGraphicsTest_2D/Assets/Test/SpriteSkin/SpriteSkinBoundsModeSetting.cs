using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Unity.U2D.Animation.Tests.RuntimeTests
{
    /// <summary>
    /// Graphics-test bootstrap: when <see cref="PendingBoundsMode"/> is set before the scene loads, applies that
    /// <see cref="BoundsMode"/> to every <see cref="SpriteSkin"/> in the loaded scene, then clears the pending
    /// value so it does not affect the next scene.
    /// </summary>
    /// <remarks>
    /// Add one instance to a bootstrap GameObject in the test scene. From the test enumerator, assign
    /// <c>PendingBoundsMode</c> immediately before <c>SceneManager.LoadScene</c> / <c>UniversalGraphicsTests.RunGraphicsTest</c>.
    /// Runs early in the load sequence so other components see the updated <c>boundsMode</c> in their <c>Awake</c>/<c>OnEnable</c>.
    /// </remarks>
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    public sealed class SpriteSkinBoundsModeSetting : MonoBehaviour
    {
        /// <summary>
        /// When non-null before this component's <c>Awake</c>, that mode is applied to all <see cref="SpriteSkin"/>
        /// instances (including on inactive GameObjects), then reset to null.
        /// </summary>
        public static BoundsMode? PendingBoundsMode { get; set; }

        void Awake()
        {
            if (!PendingBoundsMode.HasValue)
                return;

            BoundsMode mode = PendingBoundsMode.Value;
            PendingBoundsMode = null;

            SpriteSkin[] skins = FindObjectsByType<SpriteSkin>(FindObjectsInactive.Include);
            for (var i = 0; i < skins.Length; i++)
                skins[i].boundsMode = mode;
        }
    }
}
