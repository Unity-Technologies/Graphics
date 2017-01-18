namespace RMGUI.GraphView
{
	public class GraphViewTypeMapper : BaseTypeMapper<GraphElementPresenter, GraphElement>
	{
		public GraphViewTypeMapper() : base(typeof(FallbackGraphElement))
		{
		}

		public override GraphElement Create(GraphElementPresenter key)
		{
			GraphElement elem = base.Create(key);
			if (elem != null)
			{
				elem.presenter = key;
			}
			return elem;
		}
	}
}
