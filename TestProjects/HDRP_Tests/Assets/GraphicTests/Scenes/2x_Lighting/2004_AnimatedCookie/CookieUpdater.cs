using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CookieUpdater : MonoBehaviour
{
	[SerializeField] Material crtMaterial = null;

	[SerializeField] CustomRenderTexture[] customRenderTextures = null;

	int frames = 0;

	void Start()
	{
		frames = 0;
	}

	void Update()
	{
		if (crtMaterial != null)
			crtMaterial.SetFloat("_MyTime", frames / 60f);

		if (customRenderTextures != null)
			foreach(CustomRenderTexture crt in customRenderTextures)
				if (crt != null)
					crt.Update();

		++frames;
	}
}
