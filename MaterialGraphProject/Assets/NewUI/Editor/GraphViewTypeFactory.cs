namespace RMGUI.GraphView
{
	public class GraphViewTypeFactory : BaseTypeFactory<GraphElementPresenter, GraphElement>
	{
		public GraphViewTypeFactory() : base(typeof(FallbackGraphElement))
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
