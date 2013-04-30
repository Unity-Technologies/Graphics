using UnityEngine;
using UnityEditor;
using System.Collections;

public class test : Editor {



	[MenuItem("CONTEXT/MeshRenderer/CreateInstance")]
	// Use this for initialization
	static void Start (MenuCommand context) {
		var renderer = context.context as Renderer;

		renderer.sharedMaterial = Instantiate (renderer.sharedMaterial) as Material;
	}
}
