namespace RMGUI.GraphView
{
	public interface IConnectable
	{
		Direction direction { get; }
		Orientation orientation { get; }
		bool highlight { get; set; }
		object source { get; }
		bool connected { get; set; }
	}

	public static class ConnectableExtensions
	{
		public static bool IsConnectable(this IConnectable connectable)
		{
			return connectable.direction != Direction.Input || !connectable.connected;
		}
	}
}
