using System.Collections.Generic;

namespace RMGUI.GraphView
{
	interface IConnectorCollection
	{
		IEnumerable<IConnector> inputConnectors { get; }
		IEnumerable<IConnector> outputConnectors { get; }
	}
}
