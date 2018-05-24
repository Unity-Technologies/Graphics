using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
	[CreateAssetMenu(fileName = "AdditionalTestSceneInfos", menuName = "Render Pipeline/Tests/AdditionalTestSceneInfos", order = 20)]
	public class AdditionalTestSceneInfos : ScriptableObject
	{
		public AdditionalTestSceneInfo[] additionalInfos;

		[System.Serializable]
		public struct AdditionalTestSceneInfo
		{
			public string name;
			public string comment;
		}
	}
}