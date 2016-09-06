using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public struct TextureSettings
{
	public uint spotCookieSize;
	public uint pointCookieSize;

	static public TextureSettings Default
	{
		get
		{
			TextureSettings settings;
			settings.spotCookieSize = 128;
			settings.pointCookieSize = 512;
			return settings;
		}
	}
}
