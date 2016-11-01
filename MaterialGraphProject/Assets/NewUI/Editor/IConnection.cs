namespace RMGUI.GraphView
{
	public interface IConnection
	{
		IConnector output { get; set; }
		IConnector input { get; set; }
	}
}
