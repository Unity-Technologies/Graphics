using NUnit.Framework;

namespace UnityEngine.Rendering
{
    public class CoreRenderPipelinePreferencesTests
    {
        [Test]
        public void RegisterInvalidPreferenceColorName()
        {
            bool RegisterColor(string colorName)
            {
                try
                {
                    CoreRenderPipelinePreferences.RegisterPreferenceColor(colorName, Color.black);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            Assert.False(RegisterColor(null));
            Assert.False(RegisterColor(""));
        }

        [Test]
        public void RegisterPreferenceColor()
        {
            var color = CoreRenderPipelinePreferences.RegisterPreferenceColor("TEST/DEFAULT_GREEN", Color.green);
            Assert.True(color() == Color.green);
        }
    }
}
