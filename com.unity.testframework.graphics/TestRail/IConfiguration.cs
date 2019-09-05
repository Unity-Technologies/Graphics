using System;

namespace TestRailGraphics.Configuration
{
	/// <summary>
	/// This dependency defines all the middleware/plugins you want to be executed in your application
	/// </summary>
	public interface IConfiguration
	{
		/// <summary>
		/// Priority order pipeline for middleware to be executed
		/// </summary>
		Type[] ListMiddlewareTypes();

		/// <summary>
		/// A list of plugins to use within the application
		/// </summary>
		Type[] ListPluginTypes();
	}
}