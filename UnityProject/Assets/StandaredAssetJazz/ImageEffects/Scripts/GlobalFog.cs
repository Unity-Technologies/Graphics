using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent (typeof(Camera))]
[AddComponentMenu ("Image Effects/Rendering/Global Fog")]
class GlobalFog : PostEffectsBase {
	
	private float CAMERA_NEAR = 0.5f;
	private float CAMERA_FAR = 50.0f;
	private float CAMERA_FOV = 60.0f;	
	private float CAMERA_ASPECT_RATIO = 1.333333f;

	public bool  distanceFog = true;
	public bool  heightFog = true;
	public float height = 1.0f;
	[Range(0.001f,10.0f)]
	public float heightDensity = 2.0f;
	public float startDistance = 0.0f;

	public Shader fogShader = null;
	private Material fogMaterial = null;


    public override bool CheckResources (){	
		 CheckSupport (true);
	
		fogMaterial = CheckShaderAndCreateMaterial (fogShader, fogMaterial);
		
		if(!isSupported)
			ReportAutoDisable ();
		return isSupported;				
	}

	[ImageEffectOpaque]
	void OnRenderImage ( RenderTexture source ,   RenderTexture destination  ){	
		if (CheckResources()==false || (!distanceFog && !heightFog))
		{
			Graphics.Blit (source, destination);
			return;
		}
			
		CAMERA_NEAR = GetComponent<Camera>().nearClipPlane;
		CAMERA_FAR = GetComponent<Camera>().farClipPlane;
		CAMERA_FOV = GetComponent<Camera>().fieldOfView;
		CAMERA_ASPECT_RATIO = GetComponent<Camera>().aspect;
	
		Matrix4x4 frustumCorners = Matrix4x4.identity;

	    float fovWHalf = CAMERA_FOV * 0.5f;
		
		Vector3 toRight = GetComponent<Camera>().transform.right * CAMERA_NEAR * Mathf.Tan (fovWHalf * Mathf.Deg2Rad) * CAMERA_ASPECT_RATIO;
		Vector3 toTop = GetComponent<Camera>().transform.up * CAMERA_NEAR * Mathf.Tan (fovWHalf * Mathf.Deg2Rad);
	
		Vector3 topLeft = (GetComponent<Camera>().transform.forward * CAMERA_NEAR - toRight + toTop);
		float CAMERA_SCALE = topLeft.magnitude * CAMERA_FAR/CAMERA_NEAR;	
			
		topLeft.Normalize();
		topLeft *= CAMERA_SCALE;
	
		Vector3 topRight = (GetComponent<Camera>().transform.forward * CAMERA_NEAR + toRight + toTop);
		topRight.Normalize();
		topRight *= CAMERA_SCALE;
		
		Vector3 bottomRight = (GetComponent<Camera>().transform.forward * CAMERA_NEAR + toRight - toTop);
		bottomRight.Normalize();
		bottomRight *= CAMERA_SCALE;
		
		Vector3 bottomLeft = (GetComponent<Camera>().transform.forward * CAMERA_NEAR - toRight - toTop);
		bottomLeft.Normalize();
		bottomLeft *= CAMERA_SCALE;
				
		frustumCorners.SetRow (0, topLeft); 
		frustumCorners.SetRow (1, topRight);		
		frustumCorners.SetRow (2, bottomRight);
		frustumCorners.SetRow (3, bottomLeft);		
		
		var camPos= GetComponent<Camera>().transform.position;
		float FdotC = camPos.y-height;
		float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
		fogMaterial.SetMatrix ("_FrustumCornersWS", frustumCorners);
		fogMaterial.SetVector ("_CameraWS", camPos);
		fogMaterial.SetVector ("_HeightParams", new Vector4 (height, FdotC, paramK, heightDensity*0.5f));
		fogMaterial.SetVector ("_DistanceParams", new Vector4 (-Mathf.Max(startDistance,0.0f), 0, 0, 0));
		
		var sceneMode= RenderSettings.fogMode;
		var sceneDensity= RenderSettings.fogDensity;
		var sceneStart= RenderSettings.fogStartDistance;
		var sceneEnd= RenderSettings.fogEndDistance;
		Vector4 sceneParams;
		bool  linear = (sceneMode == UnityEngine.FogMode.Linear);
		float diff = linear ? sceneEnd - sceneStart : 0.0f;
		float invDiff = Mathf.Abs(diff) > 0.0001f ? 1.0f / diff : 0.0f;
		sceneParams.x = sceneDensity * 1.2011224087f; // density / sqrt(ln(2)), used by Exp2 fog mode
		sceneParams.y = sceneDensity * 1.4426950408f; // density / ln(2), used by Exp fog mode
		sceneParams.z = linear ? -invDiff : 0.0f;
		sceneParams.w = linear ? sceneEnd * invDiff : 0.0f;
		fogMaterial.SetVector ("_SceneFogParams", sceneParams);
		fogMaterial.SetInt ("_SceneFogMode", (int)sceneMode);
		
		int pass = 0;
		if (distanceFog && heightFog)
			pass = 0; // distance + height
		else if (distanceFog)
			pass = 1; // distance only
		else
			pass = 2; // height only
		CustomGraphicsBlit (source, destination, fogMaterial, pass);
	}
	
	static void CustomGraphicsBlit ( RenderTexture source ,   RenderTexture dest ,   Material fxMaterial ,   int passNr  ){
		RenderTexture.active = dest;
		       
		fxMaterial.SetTexture ("_MainTex", source);	        
	        	        
		GL.PushMatrix ();
		GL.LoadOrtho ();	
	    	
		fxMaterial.SetPass (passNr);	
		
	    GL.Begin (GL.QUADS);
							
		GL.MultiTexCoord2 (0, 0.0f, 0.0f); 
		GL.Vertex3 (0.0f, 0.0f, 3.0f); // BL
		
		GL.MultiTexCoord2 (0, 1.0f, 0.0f); 
		GL.Vertex3 (1.0f, 0.0f, 2.0f); // BR
		
		GL.MultiTexCoord2 (0, 1.0f, 1.0f); 
		GL.Vertex3 (1.0f, 1.0f, 1.0f); // TR
		
		GL.MultiTexCoord2 (0, 0.0f, 1.0f); 
		GL.Vertex3 (0.0f, 1.0f, 0.0f); // TL
		
		GL.End ();
	    GL.PopMatrix ();
	}		
}