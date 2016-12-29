using UnityEngine.Experimental.Rendering;

namespace UnityEngine.ScriptableRenderPipeline
{ 
	public class RenderingDataStore : IScriptableRenderDataStore
	{
		private bool m_NeedsBuild = true;

		public RenderingDataStore(IRenderPipeline owner)
		{
			this.owner = owner;
		}

		public void Build()
		{
			if (m_NeedsBuild)
			{
				InternalBuild();
				m_NeedsBuild = false;
			}
		}

		protected virtual void InternalBuild()
		{}

		public void Cleanup()
		{
			if (!m_NeedsBuild)
			{
				InternalCleanup();
				m_NeedsBuild = true;
			}
		}

		protected virtual void InternalCleanup()
		{}

		public IRenderPipeline owner { get; private set; }

		public T GetRealOwner<T>() where T : IRenderPipeline
		{
			return (T)owner;
		}
	}
}
