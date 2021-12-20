using UnityEngine;
using System.Collections;

public class MouseLock : MonoBehaviour {

	void OnApplicationFocus(bool status)
	{
		if (status)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}
}
