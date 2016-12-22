using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public abstract class DataWatchContainer : VisualContainer
	{
		IDataWatchHandle handle;

		protected DataWatchContainer()
		{
			// trigger data source reset when entering leaving panel
			onEnter += AddWatch;
			onLeave += RemoveWatch;
		}

		// called when Serialized object has changed
		// only works while widget is in a panel
		public virtual void OnDataChanged()
		{ }

		protected abstract object toWatch { get; }

		protected void AddWatch()
		{
			var watch = toWatch as UnityEngine.Object;
			if (watch != null && panel != null)
			{
				handle = panel.dataWatch.AddWatch(this, watch, OnDataChanged);
			}
		}

		protected void RemoveWatch()
		{
			if (handle != null)
			{
				handle.Dispose();
				handle = null;
			}
		}
	}
}
