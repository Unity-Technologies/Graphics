using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TextMesh)), ExecuteInEditMode]
public class TextMeshPixelSize : MonoBehaviour
{
#if UNITY_EDITOR

	[SerializeField] int pixelSize = 8;

	[SerializeField] HDRP_TestSettings testSettings;
	[SerializeField] new Camera camera;
	
	[HideInInspector, SerializeField] TextMesh textMesh;
	
	[SerializeField] bool pixelPerfect = false;
	void Update()
	{
		if (camera == null) camera = FindObjectOfType<Camera>();
		if (camera == null) return;

		if (testSettings == null) testSettings = camera.gameObject.GetComponentInChildren<HDRP_TestSettings>();
		if (testSettings == null) testSettings = FindObjectOfType<HDRP_TestSettings>();
		if (testSettings == null) return;

		if (textMesh == null) textMesh = GetComponent<TextMesh>();
		if (textMesh == null) return;

		float ratio = 1f * testSettings.ImageComparisonSettings.TargetWidth / testSettings.ImageComparisonSettings.TargetHeight;
		
		Vector3 pos = camera.transform.InverseTransformPoint( transform.position );

		float zDistance = pos.z;
		if (zDistance < 0f ) return;

		float size = 1f;

		MeshRenderer rndr = GetComponent<MeshRenderer>();

		Vector2 fovs = new Vector2( camera.fieldOfView * ratio, camera.fieldOfView);

		float cameraPixelSize = ( camera.orthographic?
			camera.orthographicSize * 2f :
			Mathf.Tan( Mathf.Deg2Rad * camera.fieldOfView * .5f ) * 2f * zDistance
		) / testSettings.ImageComparisonSettings.TargetHeight;

		// Correct position for pixel perfect
		if (pixelPerfect)
		{
			pixelPerfect = false;

			CorrectPosition();
		}
		
		size = ( pixelSize + 2f ) * cameraPixelSize;

		textMesh.characterSize = size;
		textMesh.fontSize = 0;
		textMesh.richText = false;
	}

#endif

	public void CorrectPosition()
	{
		Vector3 pos = camera.transform.InverseTransformPoint( transform.position );
		
		float zDistance = pos.z;
		if (zDistance < 0f ) return;

		float cameraPixelSize = ( camera.orthographic?
			camera.orthographicSize * 2f :
			Mathf.Tan( Mathf.Deg2Rad * camera.fieldOfView * .5f ) * 2f * zDistance
		) / testSettings.ImageComparisonSettings.TargetHeight;

		float signX = Mathf.Sign( pos.x );
		float signY = Mathf.Sign( pos.y );

		pos.x = Mathf.Abs(pos.x);
		pos.y = Mathf.Abs(pos.y);

		pos.x = signX * ( pos.x - pos.x % cameraPixelSize );
		pos.y = signY * ( pos.y - pos.y % cameraPixelSize );

		pos = camera.transform.TransformPoint(pos);
		transform.position = pos;
	}
}
