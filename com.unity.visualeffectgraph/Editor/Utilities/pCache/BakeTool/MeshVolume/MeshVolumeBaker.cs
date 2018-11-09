#define DEBUG_SLICES
#undef DEBUG_SLICES

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2018_2_OR_NEWER
using UnityEngine.Rendering;
#else
using UnityEngine.Experimental.Rendering;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEditor.VFX.Utils
{
	public class MeshVolumePCacheBaker
	{
		public Mesh mesh;
		public float voxelsSize = 0.5f ;
		public bool keepInside = true;

		bool m_processing = false;
		public bool processing {get{return m_processing;}}

		List<Vector3> m_voxels;

		public ComputeShader computeShader;

		public List<Vector3> voxels
		{
			get{ return m_voxels; }
		}

		public UnityEngine.Events.UnityAction finishedCallback;

		public IEnumerator Bake()
		{
			var previousRP = GraphicsSettings.renderPipelineAsset;
			GraphicsSettings.renderPipelineAsset = null;
		
			string csPath = "Packages/com.unity.visualeffectgraph/Editor/Utilities/pCache/BakeTool/MeshVolume/GetVoxelsFromSlice.compute";

			if (computeShader == null)
				computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>( csPath );

			if ( m_voxels == null) m_voxels = new List<Vector3>();
			m_voxels.Clear();
			
			float time = Time.realtimeSinceStartup;
			
			if (mesh == null) yield break;

			m_processing = true;
			
			Bounds bounds = mesh.bounds;
			
			// Number of voxels on each axis.
			Vector3Int m_voxelsCount = new Vector3Int(
				Mathf.CeilToInt( bounds.extents.x * 2f / voxelsSize ),
				Mathf.CeilToInt( bounds.extents.y * 2f / voxelsSize ),
				Mathf.CeilToInt( bounds.extents.z * 2f / voxelsSize )
			);
			
			Vector3 m_voxelsCountF = m_voxelsCount;
			
			// Resize the bounds to match the border of voxels.
			bounds.extents = m_voxelsCountF * 0.5f * voxelsSize;
			
			// The bounds of the center of the voxels.
			var voxelBounds = bounds;
			voxelBounds.extents = voxelBounds.extents - 0.5f * Vector3.one * voxelsSize;

			// Material to render the slices.
			Material material = new Material(Shader.Find("Hidden/VoxelizeShader"));
			material.SetInt("_ColorMask", 8 );
			Material[] materials = new Material[mesh.subMeshCount];
			for (int i = 0; i < mesh.subMeshCount; ++i) materials[i] = material;

			// Create the camera to render the object slices
			Camera camera = new GameObject("VoxelizeCamera").AddComponent<Camera>();
			camera.gameObject.layer = 31;
			camera.cullingMask = LayerMask.GetMask( LayerMask.LayerToName(31) );
			camera.transform.position = bounds.center + Vector3.back * ( bounds.extents.z + 1f );
			camera.clearFlags = CameraClearFlags.Depth;
			camera.backgroundColor = Color.black;
			camera.orthographic = true;
			camera.orthographicSize = Mathf.Max( bounds.extents.x, bounds.extents.y );
			camera.allowMSAA = false;

			camera.nearClipPlane = 0.9f;
			camera.farClipPlane = bounds.extents.z * 2f + 3f;

			// Quad for the background.
			Mesh quad = new Mesh();
			quad.vertices = new Vector3[]{
				new Vector3( bounds.min.x - 1f, bounds.min.y - 1f, bounds.max.z +1),
				new Vector3( bounds.max.x + 1f, bounds.min.y - 1f, bounds.max.z +1),
				new Vector3( bounds.max.x + 1f, bounds.max.y + 1f, bounds.max.z +1),
				new Vector3( bounds.min.x - 1f, bounds.max.y + 1f, bounds.max.z +1)
			};
			quad.triangles = new int[]{
				0,2,1,
				2,0,3
			};
			quad.RecalculateBounds();
			quad.RecalculateNormals();

			// Render texture to render the slices.
			RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
			rtDesc.width = m_voxelsCount.x;
			rtDesc.height = m_voxelsCount.y;
			rtDesc.depthBufferBits = 32;
			rtDesc.colorFormat = RenderTextureFormat.R8;
			rtDesc.volumeDepth = 1;
			rtDesc.msaaSamples = 1;
			rtDesc.dimension = TextureDimension.Tex2D;
			RenderTexture renderTexture = new RenderTexture(rtDesc);
			renderTexture.antiAliasing = 1;

			camera.targetTexture = renderTexture;

			int kernelIndex = computeShader.FindKernel("FilterVoxels");
			var filteredm_voxelsBuffer = new ComputeBuffer(m_voxelsCount.x * m_voxelsCount.y, sizeof(float)*3, ComputeBufferType.Counter );
			computeShader.SetBuffer(kernelIndex, "filteredVoxelsBuffer", filteredm_voxelsBuffer);
			
			// Buffer to store count in.
			var countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
			int[] counter = new int[1] { 0 };
			
			computeShader.SetVector("minP", voxelBounds.min);
			computeShader.SetVector("maxP", voxelBounds.max);
			
			computeShader.SetVector( "textureSize", new Vector2(m_voxelsCount.x, m_voxelsCount.y) );
					
			computeShader.SetTexture(kernelIndex, "slices", renderTexture);
			
			Vector3[] filteredm_voxels = new Vector3[m_voxelsCount.x * m_voxelsCount.y];
			Vector3[] zeroData = new Vector3[m_voxelsCount.x * m_voxelsCount.y];
			for (int i=0 ; i<zeroData.Length ; ++i) zeroData[i] = Vector3.one * 20;
			
			
			Vector3Int threadGroups = new Vector3Int( Mathf.CeilToInt( m_voxelsCountF.x / 8f ), Mathf.CeilToInt( m_voxelsCountF.y / 8f ), 1 );

			CommandBuffer cmd = new CommandBuffer();
			cmd.name = "Draw Slices";

			for (int z = 0; z <= m_voxelsCount.z; ++z)
			{	
				EditorUtility.DisplayProgressBar("Voxelization", "", (float)z / (m_voxelsCount.z+2f) );
				
				camera.nearClipPlane = 1f + (z + 0.5f) * voxelsSize;

				camera.RemoveAllCommandBuffers();

				cmd.Clear();
				cmd.ClearRenderTarget(true, false, Color.black, 1.0f);
				cmd.DrawMesh(quad, Matrix4x4.identity, material, 0, 1);
				//cmd.DrawMesh(mesh, Matrix4x4.identity, material);
				
				for(var i=0 ; i<mesh.subMeshCount ; ++i)
					cmd.DrawMesh(mesh, Matrix4x4.identity, material, i, 0);
				for(var i=0 ; i<mesh.subMeshCount ; ++i)
					cmd.DrawMesh(mesh, Matrix4x4.identity, material, i, 1);

				camera.AddCommandBuffer( CameraEvent.AfterEverything, cmd);
				
				camera.Render();

#if DEBUG_SLICES
				string textureOutput = System.IO.Path.Combine(Application.dataPath, "Debug/debug_"+z+".png");
				RenderTexture previous = RenderTexture.active;
				RenderTexture.active = renderTexture;
				camera.Render();
				Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height );
				tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
				RenderTexture.active = previous;
				tex.Apply();
				var bytes = ImageConversion.EncodeToPNG(tex);
				
				System.IO.File.WriteAllBytes(textureOutput, bytes);
#endif

				
				computeShader.SetFloat("zValue", Mathf.Lerp(voxelBounds.min.z, voxelBounds.max.z, (float)z / m_voxelsCount.z));

				filteredm_voxelsBuffer.SetData(zeroData);
				filteredm_voxelsBuffer.SetCounterValue(0);

				computeShader.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, 1);

				var readbackRequest = AsyncGPUReadback.Request(filteredm_voxelsBuffer);
				while (!readbackRequest.done)
				{
					readbackRequest.Update();
					yield return null;
				}

				// Copy the count.
				ComputeBuffer.CopyCount(filteredm_voxelsBuffer, countBuffer, 0);

				// Retrieve it into array.
				countBuffer.GetData(counter);

				filteredm_voxels = readbackRequest.GetData<Vector3>().ToArray();

				//filteredm_voxelsBuffer.GetData(filteredm_voxels);

				System.Array.Resize(ref filteredm_voxels, counter[0]);

				//Debug.Log( filteredm_voxels.Aggregate("Data ("+filteredm_voxels.Length+"): ", (s, v) => s += v.ToString() ) );

				m_voxels.AddRange(filteredm_voxels);
			}

			EditorUtility.ClearProgressBar();
			
#if DEBUG_SLICES
			AssetDatabase.Refresh();
#endif

			countBuffer.Dispose();
			filteredm_voxelsBuffer.Dispose();
			cmd.Dispose();

			EditorUtility.ClearProgressBar();
			
			Object.DestroyImmediate(camera.gameObject);
			Object.DestroyImmediate(renderTexture);
			Object.DestroyImmediate(material);

			time = Time.realtimeSinceStartup - time;
			// Debug.Log("Generated "+m_voxels.Count+" m_voxels in "+time+" seconds.");

			m_processing = false;

			GraphicsSettings.renderPipelineAsset = previousRP;
			
			if (finishedCallback != null ) finishedCallback();
		}
	}
}
