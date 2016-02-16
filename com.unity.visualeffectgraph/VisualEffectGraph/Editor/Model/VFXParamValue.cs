using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
	public abstract class VFXParamValue
	{
		private VFXParam.Type m_Type;
		private List<VFXBlockModel> m_BoundModels = new List<VFXBlockModel>();

		public VFXParam.Type ValueType
		{
			get { return m_Type;  }
		}

		public static VFXParamValue Create(VFXParam.Type type)
		{
			switch (type)
			{
				case VFXParam.Type.kTypeFloat: return new VFXParamValueFloat();
				case VFXParam.Type.kTypeFloat2: return new VFXParamValueFloat2();
				case VFXParam.Type.kTypeFloat3: return new VFXParamValueFloat3();
				case VFXParam.Type.kTypeFloat4: return new VFXParamValueFloat4();
				case VFXParam.Type.kTypeInt: return new VFXParamValueInt();
				case VFXParam.Type.kTypeUint: return new VFXParamValueUint();
				case VFXParam.Type.kTypeTexture2D: return new VFXParamValueTexture2D();
				case VFXParam.Type.kTypeTexture3D: return new VFXParamValueTexture3D();
				default:
					throw new ArgumentException("Invalid parameter type");
			}
		}
	}

	public abstract class VFXParamValueTyped<T> : VFXParamValue
	{
		private T m_Value = default(T);
		
		public T Value
		{
			get { return m_Value; }
			set { m_Value = value; } // TODO Propagate the change ?
		}
	}

	public class VFXParamValueFloat : VFXParamValueTyped<float> {}
	public class VFXParamValueFloat2 : VFXParamValueTyped<Vector2> {}
	public class VFXParamValueFloat3 : VFXParamValueTyped<Vector3> {}
	public class VFXParamValueFloat4 : VFXParamValueTyped<Vector4> {}
	public class VFXParamValueInt : VFXParamValueTyped<int> {}
	public class VFXParamValueUint : VFXParamValueTyped<uint> {}
	public class VFXParamValueTexture2D : VFXParamValueTyped<Texture2D> {}
	public class VFXParamValueTexture3D : VFXParamValueTyped<Texture3D> {}
}