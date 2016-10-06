using UnityEngine.RMGUI;
using Object = UnityEngine.Object;

namespace RMGUI.GraphView
{
	public class DataContainer : VisualContainer
	{
		IDataSource m_DataProvider;

		public IDataSource dataProvider
		{
			get { return m_DataProvider; }
			set
			{
				if (m_DataProvider == value)
					return;
				RemoveWatch();
				m_DataProvider = value;
				OnDataChanged();
				AddWatch();
			}
		}

		public T GetData<T>() where T : GraphElementData
		{
			return dataProvider as T;
		}

		void AddWatch()
		{
			if (m_DataProvider != null && panel != null && m_DataProvider is Object)
				// TODO: consider a disposable handle?
				DataWatchService.AddDataSpy(this, (Object) m_DataProvider, OnDataChanged);
		}

		void RemoveWatch()
		{
			if (m_DataProvider != null && panel != null && m_DataProvider is Object)
				DataWatchService.RemoveDataSpy((Object) m_DataProvider, OnDataChanged);
		}

		public DataContainer()
		{
			// trigger data source reset when entering leaving panel
			onEnter += AddWatch;
			onLeave += RemoveWatch;
		}

		// called when Serialized object has changed
		// only works while widget is in a panel
		public virtual void OnDataChanged()
		{}
	}
}
