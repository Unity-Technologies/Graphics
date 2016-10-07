using System;
using System.Collections.Generic;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	class NodeData : SimpleElementData
	{
		[SerializeField]
		protected List<NodeAnchorData> m_Anchors;
		public List<NodeAnchorData> anchors
 		{
 			get { return m_Anchors ?? (m_Anchors = new List<NodeAnchorData>()); }
 		}

		// NOTE: This is a demo node. We could have any number of output anchors if we wanted.
		public NodeAnchorData outputAnchor;

		// TODO make a simple creation function
		protected new void OnEnable()
		{
			base.OnEnable();
			if (m_Anchors==null)
				m_Anchors = new List<NodeAnchorData>();

			// This is a demo version. We could have a ctor that takes in input and output types, etc.
			var nodeAnchorData = CreateInstance<NodeAnchorData>();
			nodeAnchorData.type = typeof (int);
			nodeAnchorData.direction = Direction.Input;
			m_Anchors.Add(nodeAnchorData);

			nodeAnchorData = CreateInstance<NodeAnchorData>();
			nodeAnchorData.type = typeof (float);
			nodeAnchorData.direction = Direction.Input;
			m_Anchors.Add(nodeAnchorData);

			nodeAnchorData = CreateInstance<NodeAnchorData>();
			nodeAnchorData.type = typeof (Vector3);
			nodeAnchorData.direction = Direction.Input;
			m_Anchors.Add(nodeAnchorData);

			nodeAnchorData = CreateInstance<NodeAnchorData>();
			nodeAnchorData.type = typeof (Texture2D);
			nodeAnchorData.direction = Direction.Input;
			m_Anchors.Add(nodeAnchorData);

			nodeAnchorData = CreateInstance<NodeAnchorData>();
			nodeAnchorData.type = typeof (Color);
			nodeAnchorData.direction = Direction.Input;
			m_Anchors.Add(nodeAnchorData);

			outputAnchor = CreateInstance<NodeAnchorData>();
			outputAnchor.type = typeof(int);
			outputAnchor.direction = Direction.Output; // get rid of direction use styles
		}
	}
}
