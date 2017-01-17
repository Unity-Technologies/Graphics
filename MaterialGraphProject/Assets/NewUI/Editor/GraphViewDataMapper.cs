namespace RMGUI.GraphView
{
	public class GraphViewDataMapper : BaseDataMapper<GraphElementPresenter, GraphElement>
	{
		public GraphViewDataMapper() : base(typeof(FallbackGraphElement))
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
