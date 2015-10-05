using System;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	public abstract class BaseLightFunction
	{
		public virtual string GetName () { return ""; }
		public virtual void GenerateBody (ShaderGenerator visitor) {}
	}

	class LambertLightFunction : BaseLightFunction
	{
		public override string GetName () { return "Lambert"; }
	}
	
	class BlinnPhongLightFunction : BaseLightFunction
	{
		public override string GetName () { return "BlinnPhong"; }
	}
	class PBRMetalicLightFunction : BaseLightFunction
	{
		public override string GetName () { return "Standard"; }
	}
}
