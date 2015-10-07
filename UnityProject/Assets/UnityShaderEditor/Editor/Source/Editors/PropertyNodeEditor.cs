using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Graphs.Material
{
	[CustomEditor(typeof(PropertyNode), true)]
	class PropertyNodeEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var propertyNode = target as PropertyNode;
			if (propertyNode == null)
				return;

			// find available properties
			var allowedBindings = propertyNode.FindValidPropertyBindings ().ToList ();

			var names = new List<string> {"none"};
			names.AddRange (allowedBindings.Select (x => x.name));
			var currentIndex = names.IndexOf (propertyNode.boundProperty == null ? "none" : propertyNode.boundProperty.name);

			EditorGUI.BeginChangeCheck ();
			currentIndex = EditorGUILayout.Popup ("Bound Property", currentIndex, names.ToArray ());
			if (EditorGUI.EndChangeCheck())
			{
				ShaderProperty selected = null;
				if (currentIndex > 0)
					selected = allowedBindings[currentIndex - 1];

				propertyNode.BindProperty (selected, true);
			}
		}
	}
}
