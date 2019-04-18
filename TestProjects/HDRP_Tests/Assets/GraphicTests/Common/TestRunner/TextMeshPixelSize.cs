using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

[RequireComponent(typeof(TextMesh)), ExecuteAlways]
public class TextMeshPixelSize : MonoBehaviour
{
	[SerializeField] int pixelSize = 8;

	[SerializeField] HDRP_TestSettings testSettings = null;
	[SerializeField] Camera targetCamera;

    [SerializeField]
    Vector2Int forceTargetDimensions = new Vector2Int(200, 150);
    [SerializeField]
    bool overrideTestSettings = false;

	[HideInInspector, SerializeField] TextMesh textMesh;

    ImageComparisonSettings safeSettings
    {
        get
        {
            ImageComparisonSettings o = null;

            if (testSettings != null) o = testSettings.ImageComparisonSettings;
            if (o == null && targetCamera!= null) o = targetCamera.gameObject.GetComponentInChildren<HDRP_TestSettings>()?.ImageComparisonSettings;
            if (o == null) o = FindObjectOfType<HDRP_TestSettings>()?.ImageComparisonSettings;
            if (o == null) o = new ImageComparisonSettings(){TargetWidth = forceTargetDimensions.x, TargetHeight = forceTargetDimensions.y}; // use overrides as defaults

            return o;
        }
    }

    [HideInInspector]
    Vector2Int targetSize
    {
        get
        {
            if (overrideTestSettings)
            {
                return forceTargetDimensions;
            }
            else
            {
                return new Vector2Int(
                    safeSettings.TargetWidth,
                    safeSettings.TargetHeight
                );
            }
        }
    }

    void CorrectSize()
    {
        if (targetCamera == null) targetCamera = FindObjectOfType<Camera>();
        if (targetCamera == null) return;

        if (textMesh == null) textMesh = GetComponent<TextMesh>();
        if (textMesh == null) return;

        float ratio = 1f * targetSize.x / targetSize.y;

        Vector3 pos = targetCamera.transform.InverseTransformPoint( transform.position );

        float zDistance = pos.z;
        if (zDistance < 0f ) return;

        float size = 1f;

        MeshRenderer rndr = GetComponent<MeshRenderer>();

        Vector2 fovs = new Vector2( targetCamera.fieldOfView * ratio, targetCamera.fieldOfView);

        float cameraPixelSize = GetCameraPixelSize();

        size = ( pixelSize + 2f ) * cameraPixelSize;

        textMesh.characterSize = size;
        textMesh.fontSize = 0;
        textMesh.richText = false;
    }

    float GetCameraPixelSize()
    {
        if (targetCamera == null) return 1f;

        Vector3 pos = targetCamera.transform.InverseTransformPoint( transform.position );

        float zDistance = pos.z;
        if (zDistance < 0f ) return 1f;

        float size = (targetCamera.orthographic ? targetCamera.orthographicSize * 2f : Mathf.Tan(Mathf.Deg2Rad * targetCamera.fieldOfView * .5f) * 2f * zDistance );

        return size / targetSize.y;
    }


#if UNITY_EDITOR
	void Update()
	{
	    CorrectSize();
	}

    [ContextMenu("Pixel Perfect Position")]
	public void CorrectPosition()
	{
	    if (targetCamera == null) return;

		Vector3 pos = targetCamera.transform.InverseTransformPoint( transform.position );

		float zDistance = pos.z;
		if (zDistance < 0f ) return;

		float cameraPixelSize = GetCameraPixelSize();

		float signX = Mathf.Sign( pos.x );
		float signY = Mathf.Sign( pos.y );

		pos.x = Mathf.Abs(pos.x);
		pos.y = Mathf.Abs(pos.y);

		pos.x = signX * ( pos.x - pos.x % cameraPixelSize ) + cameraPixelSize*0.5f;
		pos.y = signY * ( pos.y - pos.y % cameraPixelSize );

		pos = targetCamera.transform.TransformPoint(pos);
		transform.position = pos;
	}

#endif
}
