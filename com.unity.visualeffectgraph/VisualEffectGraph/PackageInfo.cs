using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("com.unity.visualeffectgraph.EditorTests")]
[assembly: InternalsVisibleTo("com.unity.visualeffectgraph.EditorTests-testable")]

namespace UnityEditor.VFX
{

public static class VisualEffectGraphPackageInfo
{
	static string m_PackagePath;
	
	public static string packagePath
	{
		get{
			if(m_PackagePath == null)
			{
				foreach(string str in UnityEditor.PackageManager.Folders.GetPackagesPaths())
				{
					m_PackagePath = str;
					break;
				}
			}
			return m_PackagePath;
		}
	}
}

}
