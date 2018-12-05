using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TextMesh)), ExecuteAlways]
public class TextMeshPixelSize : MonoBehaviour
{
	[SerializeField] int pixelSize = 8;

	[SerializeField] HDRP_TestSettings testSettings;
	[SerializeField] Camera targetCamera;

	[HideInInspector, SerializeField] TextMesh textMesh;

    void CorrectSize()
    {
        if (targetCamera == null) targetCamera = FindObjectOfType<Camera>();
        if (targetCamera == null) return;

        if (testSettings == null) testSettings = targetCamera.gameObject.GetComponentInChildren<HDRP_TestSettings>();
        if (testSettings == null) testSettings = FindObjectOfType<HDRP_TestSettings>();
        if (testSettings == null) return;

        if (textMesh == null) textMesh = GetComponent<TextMesh>();
        if (textMesh == null) return;

        float ratio = 1f * testSettings.ImageComparisonSettings.TargetWidth / testSettings.ImageComparisonSettings.TargetHeight;

        Vector3 pos = targetCamera.transform.InverseTransformPoint( transform.position );

        float zDistance = pos.z;
        if (zDistance < 0f ) return;

        float size = 1f;

        MeshRenderer rndr = GetComponent<MeshRenderer>();

        Vector2 fovs = new Vector2( targetCamera.fieldOfView * ratio, targetCamera.fieldOfView);

        float cameraPixelSize = ( targetCamera.orthographic?
                targetCamera.orthographicSize * 2f :
                Mathf.Tan( Mathf.Deg2Rad * targetCamera.fieldOfView * .5f ) * 2f * zDistance
            ) / testSettings.ImageComparisonSettings.TargetHeight;

        size = ( pixelSize + 2f ) * cameraPixelSize;

        textMesh.characterSize = size;
        textMesh.fontSize = 0;
        textMesh.richText = false;
    }


#if UNITY_EDITOR
	void Update()
	{
	    CorrectSize();
	}

    [ContextMenu("Pixel Perfect Position")]
	public void CorrectPosition()
	{
		Vector3 pos = targetCamera.transform.InverseTransformPoint( transform.position );

		float zDistance = pos.z;
		if (zDistance < 0f ) return;

		float cameraPixelSize = ( targetCamera.orthographic?
			targetCamera.orthographicSize * 2f :
			Mathf.Tan( Mathf.Deg2Rad * targetCamera.fieldOfView * .5f ) * 2f * zDistance
		) / testSettings.ImageComparisonSettings.TargetHeight;

		float signX = Mathf.Sign( pos.x );
		float signY = Mathf.Sign( pos.y );

		pos.x = Mathf.Abs(pos.x);
		pos.y = Mathf.Abs(pos.y);

		pos.x = signX * ( pos.x - pos.x % cameraPixelSize );
		pos.y = signY * ( pos.y - pos.y % cameraPixelSize );

		pos = targetCamera.transform.TransformPoint(pos);
		transform.position = pos;
	}

#endif
}
