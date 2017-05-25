using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateGI : MonoBehaviour {
	private Renderer rend;

	IEnumerator Start()
	{
		rend = GetComponent<Renderer>();
		while (true)
		{
			RendererExtensions.UpdateGIMaterials(rend);
			yield return null;
		}
	}
}
