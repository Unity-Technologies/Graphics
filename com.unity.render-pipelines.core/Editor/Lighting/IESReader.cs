using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Class to Parse IES File
    /// </summary>
    [System.Serializable]
    public class IESReader
    {
        string m_FileFormatVersion;
        /// <summary>
        /// Version of the IES File
        /// </summary>
        public string FileFormatVersion
        {
            get { return m_FileFormatVersion; }
        }

        float m_TotalLumens;
        /// <summary>
        /// Total light intensity (in Lumens) stored on the file, usage of it is optional (through the prefab subasset inside the IESObject)
        /// </summary>
        public float TotalLumens
        {
            get { return m_TotalLumens; }
        }

        float m_MaxCandelas;
        /// <summary>
        /// Maximum of Candela in the IES File
        /// </summary>
        public float MaxCandelas
        {
            get { return m_MaxCandelas; }
        }

        int m_PhotometricType;

        /// <summary>
        /// Type of Photometric light in the IES file, varying per IES-Type and version
        /// </summary>
        public int PhotometricType
        {
            get { return m_PhotometricType; }
        }

        Dictionary<string, string> m_KeywordDictionary = new Dictionary<string, string>();

        int m_VerticalAngleCount;
        int m_HorizontalAngleCount;
        float[] m_VerticalAngles;
        float[] m_HorizontalAngles;
        float[] m_CandelaValues;

        float m_MinDeltaVerticalAngle;
        float m_MinDeltaHorizontalAngle;
        float m_FirstHorizontalAngle;
        float m_LastHorizontalAngle;

        // File format references:
        // https://www.ies.org/product/standard-file-format-for-electronic-transfer-of-photometric-data/
        // http://lumen.iee.put.poznan.pl/kw/iesna.txt
        // https://seblagarde.wordpress.com/2014/11/05/ies-light-format-specification-and-reader/
        /// <summary>
        /// Main function to read the file
        /// </summary>
        /// <param name="iesFilePath">The path to the IES File on disk.</param>
        /// <returns>Return the error during the import otherwise null if no error</returns>
        public string ReadFile(string iesFilePath)
        {
            using (var iesReader = File.OpenText(iesFilePath))
            {
                string versionLine = iesReader.ReadLine();

                if (versionLine == null)
                {
                    return "Premature end of file (empty file).";
                }

                switch (versionLine.Trim())
                {
                    case "IESNA91":
                        m_FileFormatVersion = "LM-63-1991";
                        break;
                    case "IESNA:LM-63-1995":
                        m_FileFormatVersion = "LM-63-1995";
                        break;
                    case "IESNA:LM-63-2002":
                        m_FileFormatVersion = "LM-63-2002";
                        break;
                    case "IES:LM-63-2019":
                        m_FileFormatVersion = "LM-63-2019";
                        break;
                    default:
                        m_FileFormatVersion = "LM-63-1986";
                        break;
                }

                var keywordRegex = new Regex(@"\s*\[(?<keyword>\w+)\]\s*(?<data>.*)", RegexOptions.Compiled);
                var tiltRegex = new Regex(@"TILT=(?<data>.*)", RegexOptions.Compiled);

                string currentKeyword = string.Empty;

                for (string keywordLine = (m_FileFormatVersion == "LM-63-1986") ? versionLine : iesReader.ReadLine(); true; keywordLine = iesReader.ReadLine())
                {
                    if (keywordLine == null)
                    {
                        return "Premature end of file (missing TILT=NONE).";
                    }

                    if (string.IsNullOrWhiteSpace(keywordLine))
                    {
                        continue;
                    }

                    Match keywordMatch = keywordRegex.Match(keywordLine);

                    if (keywordMatch.Success)
                    {
                        string keyword = keywordMatch.Groups["keyword"].Value;
                        string data = keywordMatch.Groups["data"].Value.Trim();

                        if (keyword == currentKeyword || keyword == "MORE")
                        {
                            m_KeywordDictionary[currentKeyword] += $" {data}";
                        }
                        else
                        {
                            // Many separate occurrences of keyword OTHER will need to be handled properly once exposed in the inspector.
                            currentKeyword = keyword;
                            m_KeywordDictionary[currentKeyword] = data;
                        }

                        continue;
                    }

                    Match tiltMatch = tiltRegex.Match(keywordLine);

                    if (tiltMatch.Success)
                    {
                        string data = tiltMatch.Groups["data"].Value.Trim();

                        if (data == "NONE")
                        {
                            break;
                        }

                        return $"TILT format not supported: TILT={data}";
                    }
                }

                string[] iesDataTokens = Regex.Split(iesReader.ReadToEnd().Trim(), @"[\s,]+");
                var iesDataTokenEnumerator = iesDataTokens.GetEnumerator();
                string iesDataToken;


                if (iesDataTokens.Length == 1 && string.IsNullOrWhiteSpace(iesDataTokens[0]))
                {
                    return "Premature end of file (missing IES data).";
                }

                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing lamp count value).";
                }

                int lampCount;
                iesDataToken = iesDataTokenEnumerator.Current.ToString();
                if (!int.TryParse(iesDataToken, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out lampCount))
                {
                    return $"Invalid lamp count value: {iesDataToken}";
                }
                if (lampCount < 1) lampCount = 1;

                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing lumens per lamp value).";
                }

                float lumensPerLamp;
                iesDataToken = iesDataTokenEnumerator.Current.ToString();
                if (!float.TryParse(iesDataToken, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out lumensPerLamp))
                {
                    return $"Invalid lumens per lamp value: {iesDataToken}";
                }
                m_TotalLumens = (lumensPerLamp < 0f) ? -1f : lampCount * lumensPerLamp;

                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing candela multiplier value).";
                }

                float candelaMultiplier;
                iesDataToken = iesDataTokenEnumerator.Current.ToString();
                if (!float.TryParse(iesDataToken, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out candelaMultiplier))
                {
                    return $"Invalid candela multiplier value: {iesDataToken}";
                }
                if (candelaMultiplier < 0f) candelaMultiplier = 0f;

                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing vertical angle count value).";
                }

                iesDataToken = iesDataTokenEnumerator.Current.ToString();
                if (!int.TryParse(iesDataToken, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out m_VerticalAngleCount))
                {
                    return $"Invalid vertical angle count value: {iesDataToken}";
                }
                if (m_VerticalAngleCount < 1)
                {
                    return $"Invalid number of vertical angles: {m_VerticalAngleCount}";
                }

                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing horizontal angle count value).";
                }

                iesDataToken = iesDataTokenEnumerator.Current.ToString();
                if (!int.TryParse(iesDataToken, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out m_HorizontalAngleCount))
                {
                    return $"Invalid horizontal angle count value: {iesDataToken}";
                }
                if (m_HorizontalAngleCount < 1)
                {
                    return $"Invalid number of horizontal angles: {m_HorizontalAngleCount}";
                }

                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing photometric type value).";
                }

                iesDataToken = iesDataTokenEnumerator.Current.ToString();
                if (!int.TryParse(iesDataToken, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out m_PhotometricType))
                {
                    return $"Invalid photometric type value: {iesDataToken}";
                }
                if (m_PhotometricType < 1 || m_PhotometricType > 3)
                {
                    return $"Invalid photometric type: {m_PhotometricType}";
                }

                // Skip luminous dimension unit type.
                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing luminous dimension unit type value).";
                }

                // Skip luminous dimension width.
                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing luminous dimension width value).";
                }

                // Skip luminous dimension length.
                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing luminous dimension length value).";
                }

                // Skip luminous dimension height.
                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing luminous dimension height value).";
                }

                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing ballast factor value).";
                }

                float ballastFactor;
                iesDataToken = iesDataTokenEnumerator.Current.ToString();
                if (!float.TryParse(iesDataToken, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out ballastFactor))
                {
                    return $"Invalid ballast factor value: {iesDataToken}";
                }
                if (ballastFactor < 0f) ballastFactor = 0f;

                // Skip future use.
                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing future use value).";
                }

                // Skip input watts.
                if (!iesDataTokenEnumerator.MoveNext())
                {
                    return "Premature end of file (missing input watts value).";
                }

                m_VerticalAngles = new float[m_VerticalAngleCount];
                float previousVerticalAngle = float.MinValue;

                m_MinDeltaVerticalAngle = 180f;

                for (int v = 0; v < m_VerticalAngleCount; ++v)
                {
                    if (!iesDataTokenEnumerator.MoveNext())
                    {
                        return "Premature end of file (missing vertical angle values).";
                    }

                    float angle;
                    iesDataToken = iesDataTokenEnumerator.Current.ToString();
                    if (!float.TryParse(iesDataToken, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out angle))
                    {
                        return $"Invalid vertical angle value: {iesDataToken}";
                    }

                    if (angle <= previousVerticalAngle)
                    {
                        return $"Vertical angles are not in ascending order near: {angle}";
                    }

                    float deltaVerticalAngle = angle - previousVerticalAngle;
                    if (deltaVerticalAngle < m_MinDeltaVerticalAngle)
                    {
                        m_MinDeltaVerticalAngle = deltaVerticalAngle;
                    }

                    m_VerticalAngles[v] = previousVerticalAngle = angle;
                }

                m_HorizontalAngles = new float[m_HorizontalAngleCount];
                float previousHorizontalAngle = float.MinValue;

                m_MinDeltaHorizontalAngle = 360f;

                for (int h = 0; h < m_HorizontalAngleCount; ++h)
                {
                    if (!iesDataTokenEnumerator.MoveNext())
                    {
                        return "Premature end of file (missing horizontal angle values).";
                    }

                    float angle;
                    iesDataToken = iesDataTokenEnumerator.Current.ToString();
                    if (!float.TryParse(iesDataToken, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out angle))
                    {
                        return $"Invalid horizontal angle value: {iesDataToken}";
                    }

                    if (angle <= previousHorizontalAngle)
                    {
                        return $"Horizontal angles are not in ascending order near: {angle}";
                    }

                    float deltaHorizontalAngle = angle - previousHorizontalAngle;
                    if (deltaHorizontalAngle < m_MinDeltaHorizontalAngle)
                    {
                        m_MinDeltaHorizontalAngle = deltaHorizontalAngle;
                    }

                    m_HorizontalAngles[h] = previousHorizontalAngle = angle;
                }

                m_FirstHorizontalAngle = m_HorizontalAngles[0];
                m_LastHorizontalAngle = m_HorizontalAngles[m_HorizontalAngleCount - 1];

                m_CandelaValues = new float[m_HorizontalAngleCount * m_VerticalAngleCount];
                m_MaxCandelas = 0f;

                for (int h = 0; h < m_HorizontalAngleCount; ++h)
                {
                    for (int v = 0; v < m_VerticalAngleCount; ++v)
                    {
                        if (!iesDataTokenEnumerator.MoveNext())
                        {
                            return "Premature end of file (missing candela values).";
                        }

                        float value;
                        iesDataToken = iesDataTokenEnumerator.Current.ToString();
                        if (!float.TryParse(iesDataToken, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
                        {
                            return $"Invalid candela value: {iesDataToken}";
                        }
                        value *= candelaMultiplier * ballastFactor;

                        m_CandelaValues[h * m_VerticalAngleCount + v] = value;

                        if (value > m_MaxCandelas)
                        {
                            m_MaxCandelas = value;
                        }
                    }
                }
            }

            return null;
        }

        internal string GetKeywordValue(string keyword)
        {
            return m_KeywordDictionary.ContainsKey(keyword) ? m_KeywordDictionary[keyword] : string.Empty;
        }

        internal int GetMinVerticalSampleCount()
        {
            if (m_PhotometricType == 2) // type B
            {
                // Factor in the 90 degree rotation that will be done when building the cylindrical texture.
                return 1 + (int)Mathf.Ceil(360 / m_MinDeltaHorizontalAngle); // 360 is 2 * 180 degrees
            }
            else // type A or C
            {
                return 1 + (int)Mathf.Ceil(360 / m_MinDeltaVerticalAngle); // 360 is 2 * 180 degrees
            }
        }

        internal int GetMinHorizontalSampleCount()
        {
            switch (m_PhotometricType)
            {
                case 3: // type A
                    return 1 + (int)Mathf.Ceil(720 / m_MinDeltaHorizontalAngle); // 720 is 2 * 360 degrees
                case 2: // type B
                    // Factor in the 90 degree rotation that will be done when building the cylindrical texture.
                    return 1 + (int)Mathf.Ceil(720 / m_MinDeltaVerticalAngle); // 720 is 2 * 360 degrees
                default: // type C
                    // Factor in the 90 degree rotation that will be done when building the cylindrical texture.
                    return 1 + (int)Mathf.Ceil(720 / Mathf.Min(m_MinDeltaHorizontalAngle, m_MinDeltaVerticalAngle)); // 720 is 2 * 360 degrees
            }
        }

        internal float ComputeVerticalAnglePosition(float angle)
        {
            return ComputeAnglePosition(angle, m_VerticalAngles);
        }

        internal float ComputeTypeAorBHorizontalAnglePosition(float angle) // angle in range [-180..+180] degrees
        {
            return ComputeAnglePosition(((m_FirstHorizontalAngle == 0f) ? Mathf.Abs(angle) : angle), m_HorizontalAngles);
        }

        internal float ComputeTypeCHorizontalAnglePosition(float angle) // angle in range [0..360] degrees
        {
            switch (m_LastHorizontalAngle)
            {
                case 0f: // the luminaire is assumed to be laterally symmetric in all planes
                    angle = 0f;
                    break;
                case 90f: // the luminaire is assumed to be symmetric in each quadrant
                    angle = 90f - Mathf.Abs(Mathf.Abs(angle - 180f) - 90f);
                    break;
                case 180f: // the luminaire is assumed to be symmetric about the 0 to 180 degree plane
                    angle = 180f - Mathf.Abs(angle - 180f);
                    break;
                default: // the luminaire is assumed to exhibit no lateral symmetry
                    break;
            }

            return ComputeAnglePosition(angle, m_HorizontalAngles);
        }

        internal float ComputeAnglePosition(float value, float[] angles)
        {
            int start = 0;
            int end = angles.Length - 1;

            if (value < angles[start])
            {
                return start;
            }

            if (value > angles[end])
            {
                return end;
            }

            while (start < end)
            {
                int index = (start + end + 1) / 2;

                float angle = angles[index];

                if (value >= angle)
                {
                    start = index;
                }
                else
                {
                    end = index - 1;
                }
            }

            float leftValue = angles[start];
            float fraction = 0f;

            if (start + 1 < angles.Length)
            {
                float rightValue = angles[start + 1];
                float deltaValue = rightValue - leftValue;

                if (deltaValue > 0.0001f)
                {
                    fraction = (value - leftValue) / deltaValue;
                }
            }

            return start + fraction;
        }

        internal float InterpolateBilinear(float x, float y)
        {
            int ix = (int)Mathf.Floor(x);
            int iy = (int)Mathf.Floor(y);

            float fractionX = x - ix;
            float fractionY = y - iy;

            float p00 = InterpolatePoint(ix + 0, iy + 0);
            float p10 = InterpolatePoint(ix + 1, iy + 0);
            float p01 = InterpolatePoint(ix + 0, iy + 1);
            float p11 = InterpolatePoint(ix + 1, iy + 1);

            float p0 = Mathf.Lerp(p00, p01, fractionY);
            float p1 = Mathf.Lerp(p10, p11, fractionY);

            return Mathf.Lerp(p0, p1, fractionX);
        }

        internal float InterpolatePoint(int x, int y)
        {
            x %= m_HorizontalAngles.Length;
            y %= m_VerticalAngles.Length;

            return m_CandelaValues[y + x * m_VerticalAngles.Length];
        }
    }
}
