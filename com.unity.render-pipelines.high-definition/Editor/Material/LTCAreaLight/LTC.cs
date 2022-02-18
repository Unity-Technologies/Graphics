/*
Copyright(c) 2017, Eric Heitz, Jonathan Dupuy, Stephen Hill and David Neubelt.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* If you use(or adapt) the source code in your own work, please include a
 reference to the paper:

  Real-Time Polygonal-Light Shading with Linearly Transformed Cosines.
  Eric Heitz, Jonathan Dupuy, Stephen Hill and David Neubelt.
  ACM Transactions on Graphics (Proceedings of ACM SIGGRAPH 2016) 35(4), 2016.
  Project page: https://eheitzresearch.wordpress.com/415-2/

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

// Content adapted from https://github.com/selfshadow/ltc_code

using System;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    internal struct Vec3
    {
        public double x;
        public double y;
        public double z;
    };

    internal class Vec3Utilities
    {
        internal static double Length(Vec3 vec3)
        {
            return Math.Sqrt(vec3.x * vec3.x + vec3.y * vec3.y + vec3.z * vec3.z);
        }
    }

    internal struct Matrix
    {
        public double m00;
        public double m01;
        public double m02;
        public double m10;
        public double m11;
        public double m12;
        public double m20;
        public double m21;
        public double m22;
    };

    internal class MatrixUtilities
    {
        internal static void Initialize(out Matrix m)
        {
            m.m00 = 0;
            m.m01 = 0;
            m.m02 = 0;

            m.m10 = 0;
            m.m11 = 0;
            m.m12 = 0;

            m.m20 = 0;
            m.m21 = 0;
            m.m22 = 0;
        }
    }

    internal struct LTCData
    {
        // Lobe magnitude
        public double magnitude;
        // Average Schlick Fresnel term
        public double fresnel;
        // Parametric representation (used by the fitter only!)
        public Vec3 X;
        public Vec3 Y;
        public Vec3 Z;
        public double m11;
        public double m22;
        public double m13;
        public Matrix M;
        // Last fitting error
        public double error;
        // Last amount of iterations
        public int iterationsCount;
        // Runtime matrix representation
        public Matrix invM;
        // Determinant of the matrix
        public double detM;
    }

    internal class LTCDataUtilities
    {
        static public void Initialize(out LTCData ltcData)
        {
            ltcData.magnitude = 1;
            ltcData.fresnel = 1;

            ltcData.X.x = 1;
            ltcData.X.y = 0;
            ltcData.X.z = 0;

            ltcData.Y.x = 0;
            ltcData.Y.y = 1;
            ltcData.Y.z = 0;

            ltcData.Z.x = 0;
            ltcData.Z.y = 0;
            ltcData.Z.z = 1;

            ltcData.m11 = 1;
            ltcData.m22 = 1;
            ltcData.m13 = 0;

            ltcData.error = 0;
            ltcData.iterationsCount = 0;
            ltcData.detM = 0;

            MatrixUtilities.Initialize(out ltcData.M);
            MatrixUtilities.Initialize(out ltcData.invM);
        }

        static public double[] GetFittingParms(in LTCData ltcData)
        {
            double[] tempParams = new double[]
            {
                ltcData.m11,
                ltcData.m22,
                ltcData.m13,
            };
            return tempParams;
        }

        static public void SetFittingParms(ref LTCData ltcData, double[] parameters, bool isotropic)
        {
            float tempM11 = Mathf.Max((float)parameters[0], 1e-7f);
            float tempM22 = Mathf.Max((float)parameters[1], 1e-7f);
            float tempM13 = (float)parameters[2];

            if (isotropic)
            {
                ltcData.m11 = tempM11;
                ltcData.m22 = tempM11;
                ltcData.m13 = 0.0f;
            }
            else
            {
                ltcData.m11 = tempM11;
                ltcData.m22 = tempM22;
                ltcData.m13 = tempM13;
            }

            // Update the matrices
            Update(ref ltcData);
        }

        static public void ComputeAverageTerms(IBRDF brdf, ref Vector3 tsView, float roughness, int sampleCount, ref LTCData ltcData)
        {
            // Initialize the values for the accumulation
            ltcData.magnitude = 0.0f;
            ltcData.fresnel = 0.0f;
            ltcData.Z.x = 0.0f;
            ltcData.Z.y = 0.0f;
            ltcData.Z.z = 0.0f;
            ltcData.error = 0.0f;

            for (int j = 0; j < sampleCount; ++j)
            {
                for (int i = 0; i < sampleCount; ++i)
                {
                    float U1 = (i + 0.5f) / sampleCount;
                    float U2 = (j + 0.5f) / sampleCount;

                    // sample
                    Vector3 tsLight = Vector3.zero;
                    brdf.GetSamplingDirection(ref tsView, roughness, U1, U2, ref tsLight);

                    // eval
                    double pdf;
                    double eval = brdf.Eval(ref tsView, ref tsLight, roughness, out pdf);
                    if (pdf == 0.0)
                        continue;

                    Vector3 H = Vector3.Normalize(tsView + tsLight);

                    // accumulate
                    double weight = eval / pdf;
                    if (double.IsNaN(weight))
                    {
                        // Should not happen
                    }

                    ltcData.magnitude += weight;
                    ltcData.fresnel += weight * Mathf.Pow(1 - Mathf.Max(0.0f, Vector3.Dot(tsView, H)), 5.0f);
                    ltcData.Z.x += weight * tsLight.x;
                    ltcData.Z.y += weight * tsLight.y;
                    ltcData.Z.z += weight * tsLight.z;
                }
            }
            ltcData.magnitude /= (float)(sampleCount * sampleCount);
            ltcData.fresnel /= (float)(sampleCount * sampleCount);

            // Finish building the average TBN orthogonal basis
            // clear y component, which should be zero with isotropic BRDFs
            ltcData.Z.y = 0.0f;
            double length = Vec3Utilities.Length(ltcData.Z);
            if (length > 0.0)
            {
                ltcData.Z.x /= length;
                ltcData.Z.y /= length;
                ltcData.Z.z /= length;
            }
            else
            {
                ltcData.Z.x = 0;
                ltcData.Z.y = 0;
                ltcData.Z.z = 1;
            }

            ltcData.X.x = ltcData.Z.z;
            ltcData.X.y = 0;
            ltcData.X.z = -ltcData.Z.x;

            ltcData.Y.x = 0;
            ltcData.Y.y = 1;
            ltcData.Y.z = 0;
        }

        // Heitz & Hill Method => Fit M, inverse to obtain target matrix
        static public void Update(ref LTCData ltcData)
        {
            // Build the source matrix M for which we're exploring the parameter space
            ltcData.M.m00 = ltcData.m11 * ltcData.X.x;
            ltcData.M.m01 = ltcData.m22 * ltcData.Y.x;
            ltcData.M.m02 = ltcData.m13 * ltcData.X.x + ltcData.Z.x;

            ltcData.M.m10 = ltcData.m11 * ltcData.X.y;
            ltcData.M.m11 = ltcData.m22 * ltcData.Y.y;
            ltcData.M.m12 = ltcData.m13 * ltcData.X.y + ltcData.Z.y;

            ltcData.M.m20 = ltcData.m11 * ltcData.X.z;
            ltcData.M.m21 = ltcData.m22 * ltcData.Y.z;
            ltcData.M.m22 = ltcData.m13 * ltcData.X.z + ltcData.Z.z;

            // Build the final matrix required at runtime for LTC evaluation
            ltcData.detM = Invert(in ltcData.M, ref ltcData.invM);
            if (ltcData.detM < 0.0)
            {
                // SHOULD NEVER HAPPEN
            }

            // Kill useless coeffs in matrix
            ltcData.invM.m01 = 0;  // Row 0 - Col 1
            ltcData.invM.m10 = 0;  // Row 1 - Col 0
            ltcData.invM.m12 = 0;  // Row 1 - Col 2
            ltcData.invM.m21 = 0;  // Row 2 - Col 1
        }

        static double Invert(in Matrix _A, ref Matrix _B)
        {
            double det = (_A.m00 * _A.m11 * _A.m22 + _A.m01 * _A.m12 * _A.m20 + _A.m02 * _A.m10 * _A.m21)
                - (_A.m20 * _A.m11 * _A.m02 + _A.m21 * _A.m12 * _A.m00 + _A.m22 * _A.m10 * _A.m01);
            if (Math.Abs(det) < double.Epsilon)
            {
                // SHOULD NEVER HAPPEN
            }

            double invDet = 1.0 / det;

            _B.m00 = +(_A.m11 * _A.m22 - _A.m21 * _A.m12) * invDet;
            _B.m10 = -(_A.m10 * _A.m22 - _A.m20 * _A.m12) * invDet;
            _B.m20 = +(_A.m10 * _A.m21 - _A.m20 * _A.m11) * invDet;

            _B.m01 = -(_A.m01 * _A.m22 - _A.m21 * _A.m02) * invDet;
            _B.m11 = +(_A.m00 * _A.m22 - _A.m20 * _A.m02) * invDet;
            _B.m21 = -(_A.m00 * _A.m21 - _A.m20 * _A.m01) * invDet;

            _B.m02 = +(_A.m01 * _A.m12 - _A.m11 * _A.m02) * invDet;
            _B.m12 = -(_A.m00 * _A.m12 - _A.m10 * _A.m02) * invDet;
            _B.m22 = +(_A.m00 * _A.m11 - _A.m10 * _A.m01) * invDet;

            return det;
        }

        public static void GetSamplingDirection(LTCData ltcData, float _U1, float _U2, ref Vector3 _direction)
        {
            // float theta = Mathf.Asin(Mathf.Sqrt(_U1));
            float theta = Mathf.Acos(Mathf.Sqrt(_U1));
            float phi = 2.0f * Mathf.PI * _U2;
            Vector3 D = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));

            Transform(ltcData.M, D, ref _direction);

            _direction.Normalize();
        }

        public static double Eval(LTCData ltcData, ref Vector3 _tsLight)
        {
            // Transform into original distribution space
            Vector3 Loriginal = Vector3.zero;
            Transform(ltcData.invM, _tsLight, ref Loriginal);
            float l = Loriginal.magnitude;
            Loriginal /= l;

            // Estimate original distribution (a clamped cosine lobe)
            double D = Math.Max(0.0, Loriginal.z) / Math.PI;

            // Compute the Jacobian, roundDwo / roundDw
            double jacobian = 1.0 / (ltcData.detM * l * l * l);

            // Scale distribution
            return ltcData.magnitude * D * jacobian;
        }

        public static void Transform(Matrix a, Vector3 b, ref Vector3 c)
        {
            // Annoying GLM library details:
            // return vec3(
            //     m[0][0] * v.x + m[1][0] * v.y + m[2][0] * v.z,
            //     m[0][1] * v.x + m[1][1] * v.y + m[2][1] * v.z,      (thank God, they didn't change the math!)
            //     m[0][2] * v.x + m[1][2] * v.y + m[2][2] * v.z);


            c.x = (float)(b.x * a.m00 + b.y * a.m01 + b.z * a.m02);
            c.y = (float)(b.x * a.m10 + b.y * a.m11 + b.z * a.m12);
            c.z = (float)(b.x * a.m20 + b.y * a.m21 + b.z * a.m22);
        }
    }
}
