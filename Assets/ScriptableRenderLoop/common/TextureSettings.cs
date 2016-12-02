namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [System.Serializable]
    public struct TextureSettings
    {
        public int spotCookieSize;
        public int pointCookieSize;
        public int reflectionCubemapSize;

        public static TextureSettings Default
        {
            get
            {
                TextureSettings settings = new TextureSettings();
                settings.spotCookieSize = 128;
                settings.pointCookieSize = 512;
                settings.reflectionCubemapSize = 128;
                return settings;
            }
        }
    }
}
