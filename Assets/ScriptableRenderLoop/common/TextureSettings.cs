using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public struct TextureSettings
{
    public uint spotCookieSize;
    public uint pointCookieSize;
    public uint reflectionCubemapSize;

    public static TextureSettings Default
    {
        get
        {
            TextureSettings settings;
            settings.spotCookieSize = 128;
            settings.pointCookieSize = 512;
            settings.reflectionCubemapSize = 128;
            return settings;
        }
    }
}
