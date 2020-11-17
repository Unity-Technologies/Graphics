//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Reflection;

namespace EVP
{

public static class CommonTools
	{
	static Dictionary<string, Color> m_colors = new Dictionary<string, Color>();


	static CommonTools ()
		{
		m_colors["red"]     = Color.red;
		m_colors["green"]   = Color.green;
		m_colors["blue"]    = Color.blue;
		m_colors["white"]   = Color.white;
		m_colors["black"]   = Color.black;
		m_colors["yellow"]  = Color.yellow;
		m_colors["cyan"]    = Color.cyan;
		m_colors["magenta"] = Color.magenta;
		m_colors["gray"]    = Color.gray;
		m_colors["grey"]    = Color.grey;
		m_colors["clear"]   = Color.clear;
		}


	public static int HexToDecimal (char ch)
		{
		switch (ch)
			{
			case '0': return 0x0;
			case '1': return 0x1;
			case '2': return 0x2;
			case '3': return 0x3;
			case '4': return 0x4;
			case '5': return 0x5;
			case '6': return 0x6;
			case '7': return 0x7;
			case '8': return 0x8;
			case '9': return 0x9;
			case 'a':
			case 'A': return 0xA;
			case 'b':
			case 'B': return 0xB;
			case 'c':
			case 'C': return 0xC;
			case 'd':
			case 'D': return 0xD;
			case 'e':
			case 'E': return 0xE;
			case 'f':
			case 'F': return 0xF;
			}

		return 0x0;
		}


	public static Color ParseColor (string col)
		{
		// Colores básicos por nombre

		if (m_colors.ContainsKey(col))
			return m_colors[col];

		// Colores en formato #FFF/#FFFA ó #FFFFFF/#FFFFFFAA

		Color result = Color.black;
		int l = col.Length;
		float f;

		if (l > 0 && col[0] == "#"[0])
			{
			if (l == 4 || l == 5)
				{
				f = 1.0f / 15.0f;

				result.r = HexToDecimal(col[1]) * f;
				result.g = HexToDecimal(col[2]) * f;
				result.b = HexToDecimal(col[3]) * f;

				if (l == 5)
					result.a = HexToDecimal(col[4]) * f;
				}
			else
			if (l == 7 || l == 9)
				{
				f = 1.0f / 255.0f;

				result.r = ((HexToDecimal(col[1]) << 4) | HexToDecimal(col[2])) * f;
				result.g = ((HexToDecimal(col[3]) << 4) | HexToDecimal(col[4])) * f;
				result.b = ((HexToDecimal(col[5]) << 4) | HexToDecimal(col[6])) * f;

				if (l == 9)
					result.a = ((HexToDecimal(col[7]) << 4) | HexToDecimal(col[8])) * f;
				}
			}

		return result;
		}


	// Returns an angle valued clamped as [-180 .. +180]

	public static float ClampAngle (float angle)
		{
		angle = angle % 360.0f;
		if (angle > 180.0f) angle -= 360.0f;
		return angle;
		}


	// Returns an angle valued clamped as [0 .. +360] suitable for Mathf.LerpAngle

	public static float ClampAngle360 (float angle)
		{
		angle = angle % 360.0f;
		if (angle < 0.0f) angle += 360.0f;
		return angle;
		}


	// Draws a debug crossmark at the given position using the given transform for orientation

	public static void DrawCrossMark (Vector3 pos, Transform trans, Color col, float length = 0.1f)
		{
		length *= 0.5f;

		Vector3 F = trans.forward * length;
		Vector3 U = trans.up * length;
		Vector3 R = trans.right * length;

		Debug.DrawLine(pos - F, pos + F, col);
		Debug.DrawLine(pos - U, pos + U, col);
		Debug.DrawLine(pos - R, pos + R, col);
		}


	// Converting lineal to logaritmic values, useful for debug lines

	public static float Lin2Log(float val)
		{
		return Mathf.Log(Mathf.Abs(val)+1) * Mathf.Sign(val);
		}

	public static Vector3 Lin2Log(Vector3 val)
		{
		return Vector3.ClampMagnitude(val, Lin2Log(val.magnitude));
		}


	// Method for cloning serializable classes
	// Usage: someClass = CommonTools.CloneObject(classToBeCloned);
	//
	// Source: http://stackoverflow.com/questions/78536/deep-cloning-objects
	//
	// Edy: Modified for using XmlSerializer instead of BinaryFormatter, which
	// seems to support basic types only.

 public static T CloneObject<T>(T source)
		{
		#if NETFX_CORE
        if (!typeof(T).GetTypeInfo().IsSerializable)
		#else
        if (!typeof(T).IsSerializable)
		#endif
           throw new System.ArgumentException("The type must be serializable.", "source");

        // Don't serialize a null object, simply return the default for that object
        if (Object.ReferenceEquals(source, null))
            return default(T);

		XmlSerializer serializer = new XmlSerializer(typeof(T));
        Stream stream = new MemoryStream();
        using (stream)
			{
            serializer.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)serializer.Deserialize(stream);
			}
		}


	// Unclamped Lerp methods


	public static float FastLerp (float from, float to, float t)
		{
		return from + (to-from) * t;
		}


	public static float LinearLerp (float x0, float y0, float x1, float y1, float x)
		{
		return y0 + (x-x0) * (y1-y0) / (x1-x0);
		}


	public static float LinearLerp (Vector2 from, Vector2 to, float t)
		{
		return LinearLerp(from.x, from.y, to.x, to.y, t);
		}


	public static float CubicLerp (float x0, float y0, float x1, float y1, float x)
		{
		// Hermite-based cubic polinomial function (spline) with horizontal tangents (0)
		//
		// h1(t) =  2*t3 - 3*t2 + 1;	-> start point
		// h2(t) = -2*t3 + 3*t2;		-> end point

		float t = (x - x0) / (x1 - x0);
		float t2 = t*t;
		float t3 = t*t2;

		return y0 * (2*t3 - 3*t2 + 1) + y1 * (-2*t3 + 3*t2);
		}


	public static float CubicLerp (Vector2 from, Vector2 to, float t)
		{
		return CubicLerp(from.x, from.y, to.x, to.y, t);
		}


	// Smooth interpolation with simplified tangent adjustment


	public static float TangentLerp (float x0, float y0, float x1, float y1, float a, float b, float x)
		{
		float h = y1 - y0;
		float tg0 = 3.0f * h * a;
		float tg1 = 3.0f * h * b;

		// Hermite-based cubic polinomial function (spline)
		//
		// h1(t) =  2*t3 - 3*t2 + 1;	-> start point
		// h2(t) = -2*t3 + 3*t2;		-> end point
		// h3(t) =    t3 - 2*t2 + t;	-> start tangent
		// h4(t) =    t3 - t2;			-> end tangent

		float t = (x - x0) / (x1 - x0);
		float t2 = t*t;
		float t3 = t*t2;

		return y0 * (2*t3 - 3*t2 + 1) + y1 * (-2*t3 + 3*t2) + tg0 * (t3 - 2*t2 + t) + tg1 * (t3 - t2);
		}


	public static float TangentLerp (Vector2 from, Vector2 to, float a, float b, float t)
		{
		return TangentLerp(from.x, from.y, to.x, to.y, a, b, t);
		}


	// Hermite interpolation with full control on tangents


	public static float HermiteLerp (float x0, float y0, float x1, float y1, float outTangent, float inTangent, float x)
		{
		// Hermite-based cubic polinomial function (spline)
		//
		// h1(t) =  2*t3 - 3*t2 + 1;	-> start point
		// h2(t) = -2*t3 + 3*t2;		-> end point
		// h3(t) =    t3 - 2*t2 + t;	-> start tangent
		// h4(t) =    t3 - t2;			-> end tangent

		float t = (x - x0) / (x1 - x0);
		float t2 = t*t;
		float t3 = t*t2;

		return y0 * (2*t3 - 3*t2 + 1) + y1 * (-2*t3 + 3*t2) + outTangent * (t3 - 2*t2 + t) + inTangent * (t3 - t2);
		}


	// Generic biased lerp with optional context optimization:
	//
	// 	BiasedLerp(x, bias)				generic unoptimized
	//	BiasedLerp(x, bias, context)	optimized for bias which changes unfrequently


	public class BiasLerpContext
		{
		public float lastBias = -1.0f;
		public float lastExponent = 0.0f;
		}


	static float BiasWithContext (float x, float bias, BiasLerpContext context)
		{
		if (x <= 0.0f) return 0.0f;
		if (x >= 1.0f) return 1.0f;

		if (bias != context.lastBias)
			{
			if (bias <= 0.0f) return x >= 1.0f? 1.0f : 0.0f;
			else if (bias >= 1.0f) return x > 0.0f? 1.0f : 0.0f;
			else if (bias == 0.5f) return x;

			context.lastExponent = Mathf.Log(bias) * -1.4427f;
			context.lastBias = bias;
			}

		return Mathf.Pow(x, context.lastExponent);
		}


	static float BiasRaw (float x, float bias)
		{
		if (x <= 0.0f) return 0.0f;
		if (x >= 1.0f) return 1.0f;

		if (bias <= 0.0f) return x >= 1.0f? 1.0f : 0.0f;
		else if (bias >= 1.0f) return x > 0.0f? 1.0f : 0.0f;
		else if (bias == 0.5f) return x;

		float exponent = Mathf.Log(bias) * -1.4427f;
		return Mathf.Pow(x, exponent);
		}


	public static float BiasedLerp (float x, float bias)
		{
		float result = bias <= 0.5f? BiasRaw(Mathf.Abs(x), bias) :
			1.0f - BiasRaw(1.0f - Mathf.Abs(x), 1.0f - bias);

		return x < 0.0f? -result : result;
		}


	public static float BiasedLerp (float x, float bias, BiasLerpContext context)
		{
		float result = bias <= 0.5f? BiasWithContext(Mathf.Abs(x), bias, context) :
			1.0f - BiasWithContext(1.0f - Mathf.Abs(x), 1.0f - bias, context);

		return x < 0.0f? -result : result;
		}
	}
}