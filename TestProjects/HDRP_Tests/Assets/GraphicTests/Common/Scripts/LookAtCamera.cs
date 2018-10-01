using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
	public new Camera camera;
	public Vector3 additionalRotation = new Vector3(0f, 180f, 0f);

	[ContextMenu("Look At")]
	public void LookAt()
	{
		if (camera == null && Camera.main == null) return;

		transform.rotation = Quaternion.LookRotation( ( (camera==null) ? Camera.main.transform.position : camera.transform.position ) - transform.position ) * Quaternion.Euler(additionalRotation);
	}
}
