using System;
using System.IO;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    internal class LTCTableGenerator
    {
        internal enum LTCTableParametrization
        {
            CosTheta,
            Theta
        }

        // Minimal roughness to avoid singularities
        const float k_MinRoughness = 0.001f;
        const int k_MaxIterations = 100;
        const float k_FitExploreDelta = 0.05f;
        const float k_Tolerance = 1e-5f;

        // Holds all the information required to achieve a LTC table generation
        internal class BRDFGenerator
        {
            public Type type;
            public IBRDF brdf;
            public LTCTableParametrization parametrization;
            public bool shouldGenerate;
            public int tableResolution;
            public int sampleCount;
            public string outputDir;

            public BRDFGenerator(Type targetType, int tableResolution, int sampleCount, LTCTableParametrization parametrization, string outputDir)
            {
                this.type = targetType;
                this.brdf = (IBRDF)Activator.CreateInstance(targetType);
                this.shouldGenerate = true;
                this.tableResolution = tableResolution;
                this.sampleCount = sampleCount;
                this.outputDir = outputDir;
                this.parametrization = parametrization;
            }
        };

        struct BRDFGeneratorJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<LTCData> ltcData;
            public int tableResolution;
            public int sampleCount;
            public LTCLightingModel lightingModel;
            public LTCTableParametrization parametrization;

            public void Fit(int roughnessIndex, int thetaIndex, NelderMead fitter, IBRDF brdf)
            {
                // Compute the roughness and cosTheta for this sample
                float roughness, cosTheta;
                GetRoughnessAndAngle(roughnessIndex, thetaIndex, tableResolution, parametrization, out roughness, out cosTheta);

                // Compute the matching view vector
                Vector3 tsView = new Vector3(Mathf.Sqrt(1 - cosTheta * cosTheta), 0, cosTheta);

                // Compute BRDF's magnitude and average direction
                LTCData currentLTCData;
                LTCDataUtilities.Initialize(out currentLTCData);
                LTCDataUtilities.ComputeAverageTerms(brdf, ref tsView, roughness, sampleCount, ref currentLTCData);

                // Otherwise use average direction as Z vector
                int previousLTCDataIndex = (thetaIndex - 1) * tableResolution + roughnessIndex;
                LTCData previousLTC = ltcData[previousLTCDataIndex];
                currentLTCData.m11 = previousLTC.m11;
                currentLTCData.m22 = previousLTC.m22;
                currentLTCData.m13 = previousLTC.m13;

                LTCDataUtilities.Update(ref currentLTCData);

                // Find best-fit LTC lobe (scale, alphax, alphay)
                if (currentLTCData.magnitude > 1e-6)
                {
                    double[] startFit = LTCDataUtilities.GetFittingParms(in currentLTCData);
                    double[] resultFit = new double[startFit.Length];

                    int localSampleCount = sampleCount;
                    currentLTCData.error = (float)fitter.FindFit(resultFit, startFit, (double)k_FitExploreDelta, (double)k_Tolerance, k_MaxIterations, (double[] parameters) =>
                    {
                        LTCDataUtilities.SetFittingParms(ref currentLTCData, parameters, false);
                        return ComputeError(currentLTCData, brdf, localSampleCount, ref tsView, roughness);
                    });
                    currentLTCData.iterationsCount = fitter.m_lastIterationsCount;

                    // Update LTC with final best fitting values
                    LTCDataUtilities.SetFittingParms(ref currentLTCData, resultFit, false);
                }

                // Store new valid result
                int currentLTCDataIndex = thetaIndex * tableResolution + roughnessIndex;
                ltcData[currentLTCDataIndex] = currentLTCData;
            }

            public void Execute(int roughnessIndex)
            {
                // Create the fitter
                NelderMead fitter = new NelderMead(3);
                IBRDF brdf = LTCAreaLight.GetBRDFInterface(lightingModel);
                // Compute all the missing LTCData (0 of the first line is already done)
                for (int thetaIndex = 1; thetaIndex < tableResolution; thetaIndex++)
                {
                    Fit(roughnessIndex, thetaIndex, fitter, brdf);
                }
            }
        }

        static void GetRoughnessAndAngle(int roughnessIndex, int thetaIndex, int tableResolution, LTCTableParametrization parametrization, out float alpha, out float cosTheta)
        {
            float perceptualRoughness = (float)roughnessIndex / (tableResolution - 1);
            alpha = Mathf.Max(k_MinRoughness, perceptualRoughness * perceptualRoughness);

            if (parametrization == LTCTableParametrization.CosTheta)
            {
                // Parameterised by sqrt(1 - cos(theta))
                float x = (float)thetaIndex / (tableResolution - 1);
                cosTheta = 1.0f - x * x;
                // Clamp to cos(1.57)
                cosTheta = Mathf.Max(3.7540224885647058065387021283285e-4f, cosTheta);
            }
            else
            {
                float theta = Mathf.Min(1.57f, thetaIndex / (float)(tableResolution - 1) * 1.57079f);
                cosTheta = Mathf.Cos(theta);
            }
        }

        static public void FitInitial(BRDFGenerator brdfGenerator, NelderMead fitter, NativeArray<LTCData> ltcData, int roughnessIndex, int thetaIndex)
        {
            // Compute the roughness and cosTheta for this sample
            float roughness, cosTheta;
            GetRoughnessAndAngle(roughnessIndex, thetaIndex, brdfGenerator.tableResolution, brdfGenerator.parametrization, out roughness, out cosTheta);

            // Compute the matching view vector
            Vector3 tsView = new Vector3(Mathf.Sqrt(1 - cosTheta * cosTheta), 0, cosTheta);

            // Compute BRDF's magnitude and average direction
            LTCData currentLTCData;
            LTCDataUtilities.Initialize(out currentLTCData);
            LTCDataUtilities.ComputeAverageTerms(brdfGenerator.brdf, ref tsView, roughness, brdfGenerator.sampleCount, ref currentLTCData);

            // if theta == 0 the lobe is rotationally symmetric and aligned with Z = (0 0 1)
            currentLTCData.X.x = 1;
            currentLTCData.X.y = 0;
            currentLTCData.X.z = 0;

            currentLTCData.Y.x = 0;
            currentLTCData.Y.y = 1;
            currentLTCData.Y.z = 0;

            currentLTCData.Z.x = 0;
            currentLTCData.Z.y = 0;
            currentLTCData.Z.z = 1;

            if (roughnessIndex == (brdfGenerator.tableResolution - 1))
            {
                // roughness = 1 or no available result
                currentLTCData.m11 = 1.0f;
                currentLTCData.m22 = 1.0f;
            }
            else
            {
                // init with roughness of previous fit
                LTCData previousLTC = ltcData[roughnessIndex + 1];
                currentLTCData.m11 = previousLTC.m11;
                currentLTCData.m22 = previousLTC.m22;
            }
            currentLTCData.m13 = 0;

            LTCDataUtilities.Update(ref currentLTCData);

            // Find best-fit LTC lobe (scale, alphax, alphay)
            if (currentLTCData.magnitude > 1e-6)
            {
                double[] startFit = LTCDataUtilities.GetFittingParms(in currentLTCData);
                double[] resultFit = new double[startFit.Length];

                currentLTCData.error = (float)fitter.FindFit(resultFit, startFit, k_FitExploreDelta, k_Tolerance, k_MaxIterations, (double[] parameters) =>
                {
                    LTCDataUtilities.SetFittingParms(ref currentLTCData, parameters, true);
                    return ComputeError(currentLTCData, brdfGenerator.brdf, brdfGenerator.sampleCount, ref tsView, roughness);
                });
                currentLTCData.iterationsCount = fitter.m_lastIterationsCount;

                // Update LTC with final best fitting values
                LTCDataUtilities.SetFittingParms(ref currentLTCData, resultFit, true);
            }

            // Store new valid result
            ltcData[roughnessIndex] = currentLTCData;
        }

        // Compute the error between the BRDF and the LTC using Multiple Importance Sampling
        static float ComputeError(LTCData ltcData, IBRDF brdf, int sampleCount, ref Vector3 _tsView, float _alpha)
        {
            Vector3 tsLight = Vector3.zero;

            double pdf_BRDF, eval_BRDF;
            double pdf_LTC, eval_LTC;

            float sumError = 0.0f;
            for (int j = 0; j < sampleCount; ++j)
            {
                for (int i = 0; i < sampleCount; ++i)
                {
                    float U1 = (i + 0.5f) / sampleCount;
                    float U2 = (j + 0.5f) / sampleCount;

                    // importance sample LTC
                    {
                        // sample
                        LTCDataUtilities.GetSamplingDirection(ltcData, U1, U2, ref tsLight);

                        eval_BRDF = brdf.Eval(ref _tsView, ref tsLight, _alpha, out pdf_BRDF);
                        eval_LTC = (float)LTCDataUtilities.Eval(ltcData, ref tsLight);
                        pdf_LTC = eval_LTC / ltcData.magnitude;

                        // error with MIS weight
                        float error = Mathf.Abs((float)(eval_BRDF - eval_LTC));
                        error = error * error * error;        // Use L3 norm to favor large values over smaller ones
                        if (error != 0.0f)
                            error /= (float)pdf_LTC + (float)pdf_BRDF;

                        if (double.IsNaN(error))
                        {
                            // SHOULD NEVER HAPPEN
                        }
                        sumError += error;
                    }

                    // importance sample BRDF
                    {
                        // sample
                        brdf.GetSamplingDirection(ref _tsView, _alpha, U1, U2, ref tsLight);

                        // error with MIS weight
                        eval_BRDF = brdf.Eval(ref _tsView, ref tsLight, _alpha, out pdf_BRDF);
                        eval_LTC = LTCDataUtilities.Eval(ltcData, ref tsLight);
                        pdf_LTC = eval_LTC / ltcData.magnitude;
                        float error = Mathf.Abs((float)(eval_BRDF - eval_LTC));
                        error = error * error * error;        // Use L3 norm to favor large values over smaller ones

                        if (error != 0.0f)
                            error /= (float)pdf_LTC + (float)pdf_BRDF;

                        if (double.IsNaN(error))
                        {
                            // SHOULD NEVER HAPPEN
                        }
                        sumError += error;
                    }
                }
            }

            return sumError / ((float)sampleCount * sampleCount);
        }

        static public void ExecuteFittingJob(BRDFGenerator brdfGenerator, bool parallel)
        {
            // When dispatching the table on the two dimensions (X, Y) a set of constrains apply:
            // - Every element (Xi, Yi) has a dependency on the previous one on the same column.
            // - The first element of a column has a dependency on the first element of the previous column.
            // - The element (0,0) doesn't have a dependency on any element.
            // To be able to dispatch this as a job, we need to compute the first line linearly and then dispatch every column starting from the second element.

            using (var ltcData = new NativeArray<LTCData>(brdfGenerator.tableResolution * brdfGenerator.tableResolution, Allocator.TempJob))
            {
                // Create the fitter
                NelderMead fitter = new NelderMead(3);

                Debug.Log("Running fitting job on the " + brdfGenerator.type.Name + " BRDF.");

                // Fill the first line
                for (int roughnessIndex = brdfGenerator.tableResolution - 1; roughnessIndex >= 0; roughnessIndex--)
                    FitInitial(brdfGenerator, fitter, ltcData, roughnessIndex, 0);

                BRDFGeneratorJob brdfJob = new BRDFGeneratorJob
                {
                    ltcData = ltcData,
                    tableResolution = brdfGenerator.tableResolution,
                    sampleCount = brdfGenerator.sampleCount,
                    lightingModel = brdfGenerator.brdf.GetLightingModel(),
                    parametrization = brdfGenerator.parametrization,
                };

                if (parallel)
                {
                    // Create, run the job and wait for its completion.
                    JobHandle fittingJob = brdfJob.Schedule(brdfGenerator.tableResolution, 1);
                    fittingJob.Complete();
                }
                else
                {
                    for (int i = 0; i < brdfGenerator.tableResolution; ++i)
                    {
                        brdfJob.Execute(i);
                    }
                }

                Debug.Log("Fitting done. Exporting the file");

                // Export the table to disk
                string BRDFName = brdfGenerator.type.Name;
                FileInfo CSharpFileName = new FileInfo(Path.Combine(brdfGenerator.outputDir, "LtcData." + BRDFName + ".cs"));
                ExportToCSharp(ltcData, brdfGenerator.tableResolution, brdfGenerator.parametrization, CSharpFileName, BRDFName);
            }
        }

        static void ExportToCSharp(NativeArray<LTCData> ltcDataArray, int tableResolution, LTCTableParametrization parametrization, FileInfo _CSharpFileName, string brdfName)
        {
            string sourceCode = "";

            LTCData ltcData;
            ltcData.magnitude = 0.0f;

            string tableName = "s_LtcMatrixData_" + brdfName;

            sourceCode += "using UnityEngine;\n"
                + "using System;\n"
                + "\n"
                + "namespace UnityEngine.Rendering.HighDefinition\n"
                + "{\n"
                + "    internal partial class LTCAreaLight\n"
                + "    {\n"
                + "        // [GENERATED CONTENT " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]\n"
                + "        // Table contains 3x3 matrix coefficients of M^-1 for the fitting of the " + brdfName + " BRDF using the LTC technique\n"
                + "        // From \"Real-Time Polygonal-Light Shading with Linearly Transformed Cosines\" 2016 (https://eheitzresearch.wordpress.com/415-2/)\n"
                + "        //\n"
                + "        // The table is accessed via LTCAreaLight." + tableName + "[<roughnessIndex> + 64 * <thetaIndex>]    // Theta values are along the Y axis, Roughness values are along the X axis\n"
                + "        //    • roughness = ( <roughnessIndex> / " + (tableResolution - 1) + " )^2  (the table is indexed by perceptual roughness)\n";
            if (parametrization == LTCTableParametrization.CosTheta)
                sourceCode += "        //    • cosTheta = 1 - ( <thetaIndex> / " + (tableResolution - 1) + " )^2\n";
            else
                sourceCode += "        //    • theta = ( <thetaIndex> / " + (tableResolution - 1) + " )\n";
            sourceCode += "        //\n"
                //                        + "        public static double[,]    " + tableName + " = new double[k_LtcLUTResolution * k_LtcLUTResolution, k_LtcLUTMatrixDim * k_LtcLUTMatrixDim] {";
                + "        public static double[,]    " + tableName + " = new double[" + tableResolution + " * " + tableResolution + ", 3 * 3]\n"
                + "        {";

            string lotsOfSpaces = "                                                                                                                            ";
            float alpha, cosTheta;
            for (int thetaIndex = 0; thetaIndex < tableResolution; thetaIndex++)
            {
                GetRoughnessAndAngle(0, thetaIndex, tableResolution, parametrization, out alpha, out cosTheta);
                sourceCode += "\n";
                if (parametrization == LTCTableParametrization.CosTheta)
                    sourceCode += "            // Cos (theta) = " + cosTheta + "\n";
                else
                    sourceCode += "            // Theta = " + Mathf.Acos(cosTheta) + "\n";

                for (int roughnessIndex = 0; roughnessIndex < tableResolution; roughnessIndex++)
                {
                    // Compute the current ltc data index
                    int currentIndexData = roughnessIndex + thetaIndex * tableResolution;
                    ltcData = ltcDataArray[currentIndexData];

                    GetRoughnessAndAngle(roughnessIndex, thetaIndex, tableResolution, parametrization, out alpha, out cosTheta);

                    // Export the matrix as a list of 3x3 doubles, columns first
                    double factor = 1.0 / ltcData.invM.m22;

                    string matrixString = (factor * ltcData.invM.m00) + ", " + (factor * ltcData.invM.m10) + ", " + (factor * ltcData.invM.m20) + ", ";
                    matrixString += (factor * ltcData.invM.m01) + ", " + (factor * ltcData.invM.m11) + ", " + (factor * ltcData.invM.m21) + ", ";
                    matrixString += (factor * ltcData.invM.m02) + ", " + (factor * ltcData.invM.m12) + ", " + "1.0";

                    string line = "            { " + matrixString + " },";
                    if (line.Length < 132)
                        line += lotsOfSpaces.Substring(lotsOfSpaces.Length - (132 - line.Length));    // Pad with spaces
                    sourceCode += line;
                    sourceCode += "// alpha = " + alpha + "\n";
                }
            }

            sourceCode += "        };\n";

            // End comment
            sourceCode += "\n";
            sourceCode += "        // NOTE: Formerly, we needed to also export and create a table for the BRDF's amplitude factor + fresnel coefficient\n";
            sourceCode += "        //    but it turns out these 2 factors are actually already precomputed and available in the FGD table corresponding\n";
            sourceCode += "        //    to the " + brdfName + " BRDF, therefore they are no longer exported...\n";

            // Close class and namespace
            sourceCode += "    }\n";
            sourceCode += "}\n";

            // Write content
            using (StreamWriter W = _CSharpFileName.CreateText())
                W.Write(sourceCode);
        }
    }
}
