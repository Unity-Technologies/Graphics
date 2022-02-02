using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// Downhill simplex solver:
    /// http://en.wikipedia.org/wiki/Nelder%E2%80%93Mead_method#One_possible_variation_of_the_NM_algorithm
    /// Using the termination criterion from Numerical Recipes in C++ (3rd Ed.)
    /// </summary>
    internal class NelderMead
    {
        // standard coefficients from Nelder-Mead
        const double reflect = 1.0;
        const double expand = 2.0;
        const double contract = 0.5;
        const double shrink = 0.5;

        int DIM;
        int NB_POINTS;
        double[][] s;
        double[] f;

        public int m_lastIterationsCount;

        public delegate double ObjectiveFunctionDelegate(double[] _parameters);

        public NelderMead(int _dimensions)
        {
            DIM = _dimensions;
            NB_POINTS = _dimensions + 1;
            s = new double[NB_POINTS][];
            for (int i = 0; i < NB_POINTS; i++)
                s[i] = new double[_dimensions];
            f = new double[NB_POINTS];
        }

        public double FindFit(double[] _pmin, double[] _start, double _delta, double _tolerance, int _maxIterations, ObjectiveFunctionDelegate _objectiveFn)
        {
            // initialise simplex
            Mov(s[0], _start);
            for (int i = 1; i < NB_POINTS; i++)
            {
                Mov(s[i], _start);
                s[i][i - 1] += _delta;
            }

            // evaluate function at each point on simplex
            for (int i = 0; i < NB_POINTS; i++)
                f[i] = _objectiveFn(s[i]);

            double[] o = new double[DIM];    // Centroid
            double[] r = new double[DIM];    // Reflection
            double[] c = new double[DIM];    // Contraction
            double[] e = new double[DIM];    // Expansion

            int lo = 0, hi, nh;
            for (m_lastIterationsCount = 0; m_lastIterationsCount < _maxIterations; m_lastIterationsCount++)
            {
                // find lowest, highest and next highest
                lo = hi = nh = 0;
                for (int i = 1; i < NB_POINTS; i++)
                {
                    if (f[i] < f[lo])
                        lo = i;
                    if (f[i] > f[hi])
                    {
                        nh = hi;
                        hi = i;
                    }
                    else if (f[i] > f[nh])
                        nh = i;
                }

                // stop if we've reached the required tolerance level
                double a = Math.Abs(f[lo]);
                double b = Math.Abs(f[hi]);
                if (2.0 * Math.Abs(a - b) < (a + b) * _tolerance)
                    break;

                // compute centroid (excluding the worst point)
                Set(o, 0.0f);
                for (int i = 0; i < NB_POINTS; i++)
                {
                    if (i == hi)
                        continue;
                    Add(o, s[i]);
                }

                for (int i = 0; i < DIM; i++)
                    o[i] /= DIM;

                // reflection
                for (int i = 0; i < DIM; i++)
                    r[i] = o[i] + reflect * (o[i] - s[hi][i]);

                double fr = _objectiveFn(r);
                if (fr < f[nh])
                {
                    if (fr < f[lo])
                    {
                        // expansion
                        for (int i = 0; i < DIM; i++)
                            e[i] = o[i] + expand * (o[i] - s[hi][i]);

                        double fe = _objectiveFn(e);
                        if (fe < fr)
                        {
                            Mov(s[hi], e);
                            f[hi] = fe;
                            continue;
                        }
                    }

                    Mov(s[hi], r);
                    f[hi] = fr;
                    continue;
                }

                // contraction
                for (int i = 0; i < DIM; i++)
                    c[i] = o[i] - contract * (o[i] - s[hi][i]);

                double fc = _objectiveFn(c);
                if (fc < f[hi])
                {
                    Mov(s[hi], c);
                    f[hi] = fc;
                    continue;
                }

                // reduction
                for (int k = 0; k < NB_POINTS; k++)
                {
                    if (k == lo)
                        continue;
                    for (int i = 0; i < DIM; i++)
                        s[k][i] = s[lo][i] + shrink * (s[k][i] - s[lo][i]);
                    f[k] = _objectiveFn(s[k]);
                }
            }

            // return best point and its value
            Mov(_pmin, s[lo]);
            return f[lo];
        }

        void Mov(double[] r, double[] v)
        {
            for (int i = 0; i < DIM; ++i)
                r[i] = v[i];
        }

        void Set(double[] r, double v)
        {
            for (int i = 0; i < DIM; ++i)
                r[i] = v;
        }

        void Add(double[] r, double[] v)
        {
            for (int i = 0; i < DIM; ++i)
                r[i] += v[i];
        }
    }
}
