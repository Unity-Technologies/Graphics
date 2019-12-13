#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{

	[CreateAssetMenu(fileName = "MaterialRoot", menuName = "QualityTracker/MaterialRoot", order = 1)]
	public class MaterialRoot : ScriptableObject
	{
		public string GetAssetPath()
		{
	#if UNITY_EDITOR
			return AssetDatabase.GetAssetPath(this);
	#else
			return "";
	#endif
		}
	}
}
