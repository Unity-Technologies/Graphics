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

		/// <summary>
		/// Indicates that the class is a Custom Pass drawer and that it replaces the default Custom Pass GUI.
		/// </summary>
		/// <param name="targetPassType">The Custom Pass type.</param>
		public CustomPassDrawerAttribute(Type targetPassType) => this.targetPassType = targetPassType;
	}
}