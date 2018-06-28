using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
	public class IesScriptedImporterEditor : ScriptedImporterEditor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
		}
	}
}