using System;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public abstract class DataWatchContainer : VisualContainer
	{
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
				// TODO: consider a disposable handle?
				DataWatchService.AddDataSpy(this, watch, OnDataChanged);
		}

		protected void RemoveWatch()
		{
			var watch = toWatch as UnityEngine.Object;
			try
			{
				if (watch != null && panel != null)
					DataWatchService.RemoveDataSpy(watch, OnDataChanged);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				if (watch != null)
				{
					Debug.LogWarningFormat("No datawatch is registered to: {0} it is type {1}", watch.name, watch.GetType());
				}
			}
		}
	}
}
