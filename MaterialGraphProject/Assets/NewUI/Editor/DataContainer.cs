using System;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class DataContainer <T> : VisualContainer where T : GraphElementData
    {
		T m_DataProvider;

		public T dataProvider
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

		public TK GetData<TK>() where TK : T
		{
			return dataProvider as TK;
		}

		void AddWatch()
		{
			if (m_DataProvider != null && panel != null)
				// TODO: consider a disposable handle?
				DataWatchService.AddDataSpy(this, m_DataProvider, OnDataChanged);
		}

		void RemoveWatch()
		{
		    try
		    {
		        if (m_DataProvider != null && panel != null)
		            DataWatchService.RemoveDataSpy(m_DataProvider, OnDataChanged);
		    }
		    catch (Exception e)
		    {
		        if (m_DataProvider != null)
		        {
		            Debug.LogWarningFormat("No datawatch is registered to: {0} it is type {1}", m_DataProvider.name, m_DataProvider.GetType());
		        }
                Debug.LogException(e, m_DataProvider); 

            }
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
