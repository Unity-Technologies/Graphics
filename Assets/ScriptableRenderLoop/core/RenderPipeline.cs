using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.ScriptableRenderPipeline
{
	public abstract class RenderPipeline : BaseRenderPipeline
	{
		private readonly HashSet<IScriptableRenderDataStore> m_AssociatedDataStores = new HashSet<IScriptableRenderDataStore>();
		private ICameraProvider m_CameraProvider;

		public override ICameraProvider cameraProvider
		{
			get
			{
				if (m_CameraProvider == null)
					m_CameraProvider = ConstructCameraProvider();

				return m_CameraProvider;
			}
			set { m_CameraProvider = value; }
		}

		public override void Render(ScriptableRenderContext renderContext, IScriptableRenderDataStore dataStore)
		{
			if (dataStore == null)
				throw new ArgumentException(string.Format("Null DataStore has been passed into pipe {0}", this));

			if (dataStore.owner == null)
				throw new ArgumentException(string.Format("DataStore owner is null. It needs o be owned by loop {0}", this));

			if (dataStore.owner != null && !ReferenceEquals(dataStore.owner, this))
				throw new ArgumentException(string.Format("DataStore {0} has been passed into pipe {1}, but is owned by {2}", dataStore, this, dataStore.owner));

			m_AssociatedDataStores.Add(dataStore);
			dataStore.Build();
		}

		public override void ClearCachedData()
		{
			foreach (var store in m_AssociatedDataStores)
				store.Cleanup();

			m_AssociatedDataStores.Clear();
		}

		public override ICameraProvider ConstructCameraProvider()
		{
			return new DefaultCameraProvider();
		}

		public override IScriptableRenderDataStore ConstructDataStore()
		{
			return new RenderingDataStore(this);
		}
		
		public static void CleanCameras(IEnumerable<Camera> cameras)
		{
			foreach (var camera in cameras)
				camera.ClearIntermediateRenderers();
		}
	}
}
