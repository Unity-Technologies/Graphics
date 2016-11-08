using System.Collections.Generic;

namespace RMGUI.GraphView
{
	public interface IConnector
	{
		Direction direction { get; }
		Orientation orientation { get; }
		bool highlight { get; set; }
		object source { get; }
		bool connected { get; }
		IEnumerable<IConnection> connections { get; }

		void Connect(IConnection connection);
		void Disconnect(IConnection connection);

		// TODO YAGNI?
		// void DisconnectAll();
	}

	public static class ConnectableExtensions
	{
		public static bool IsConnectable(this IConnector connector)
		{
			return connector.direction == Direction.Output || !connector.connected;
		}
	}
}
