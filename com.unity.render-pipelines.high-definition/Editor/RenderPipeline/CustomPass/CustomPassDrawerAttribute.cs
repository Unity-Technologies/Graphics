using System;

namespace UnityEditor.Rendering.HighDefinition
{
	/// <summary>
	/// Tells a CustomPassDrawer which CustomPass class is intended for the GUI inside the CustomPassDrawer class
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class CustomPassDrawerAttribute : Attribute
	{
		internal Type targetPassType;

		public CustomPassDrawerAttribute(Type targetPassType) => this.targetPassType = targetPassType;
	}
}