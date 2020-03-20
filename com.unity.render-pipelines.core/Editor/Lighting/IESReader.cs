using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public class IesReader
    {
        Dictionary<string, string> m_KeywordDictionary = new Dictionary<string, string>();
        public Dictionary<string, string> KeywordDictionary
        {
            get { return m_KeywordDictionary; }
        }

        float m_TotalLumens;
        public float TotalLumens
        {
            get { return m_TotalLumens; }
        }

        float m_MaxCandelas;
        public float MaxCandelas
        {
            get { return m_MaxCandelas; }
        }

        int m_VerticalAngleCount;
        public int VerticalAngleCount
        {
            get { return m_VerticalAngleCount; }
        }

        int m_HorizontalAngleCount;
        public int HorizontalAngleCount
        {
            get { return m_HorizontalAngleCount; }
        }

        int m_PhotometricType;

        float[] m_VerticalAngles;
        float[] m_HorizontalAngles;
        float[] m_CandelaValues;

        // File format references:
        // https://www.ies.org/product/standard-file-format-for-electronic-transfer-of-photometric-data/
        // http://lumen.iee.put.poznan.pl/kw/iesna.txt
        // https://seblagarde.wordpress.com/2014/11/05/ies-light-format-specification-and-reader/
        public string ReadFile(string iesFilePath)
        {
            using (var iesReader = new StreamReader(iesFilePath))
            {
                string versionLine = iesReader.ReadLine().Trim();
                if (versionLine != "IESNA91" && versionLine != "IESNA:LM-63-1995" && versionLine != "IESNA:LM-63-2002")
                {
                    return $"IES file version not supported: {versionLine}";
                }

                var keywordRegex = new Regex(@"\s*\[(?<keyword>\w+)\]\s*(?<data>.*)", RegexOptions.Compiled);
                var tiltRegex = new Regex(@"TILT=(?<data>.*)", RegexOptions.Compiled);

                string currentKeyword = string.Empty;

                while (true)
                {
                    string keywordLine = iesReader.ReadLine();

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

                string iesDataLines = iesReader.ReadToEnd();

                int lampCount = GetInt(iesDataLines, out iesDataLines);
                if (lampCount < 1) lampCount = 1;

                float lumensPerLamp = GetFloat(iesDataLines, out iesDataLines);
                m_TotalLumens = (lumensPerLamp < 0f) ? -1f : lampCount * lumensPerLamp;

                float candelaMultiplier = GetFloat(iesDataLines, out iesDataLines);
                if (candelaMultiplier < 0f) candelaMultiplier = 0f;

                m_VerticalAngleCount = GetInt(iesDataLines, out iesDataLines);
                if (m_VerticalAngleCount < 1)
                {
                    return $"Invalid number of vertical angles: {m_VerticalAngleCount}";
                }

                m_HorizontalAngleCount = GetInt(iesDataLines, out iesDataLines);
                if (m_HorizontalAngleCount < 1)
                {
                    return $"Invalid number of horizontal angles: {m_HorizontalAngleCount}";
                }

                m_PhotometricType = GetInt(iesDataLines, out iesDataLines);
                if (m_PhotometricType < 1 || m_PhotometricType > 3)
                {
                    return $"Invalid photometric type: {m_PhotometricType}";
                }

                int luminousDimensionUnitType = GetInt(iesDataLines, out iesDataLines);
                float luminousDimensionWidth = GetFloat(iesDataLines, out iesDataLines);
                float luminousDimensionLength = GetFloat(iesDataLines, out iesDataLines);
                float luminousDimensionHeight = GetFloat(iesDataLines, out iesDataLines);

                float ballastFactor = GetFloat(iesDataLines, out iesDataLines);
                if (ballastFactor < 0f) ballastFactor = 0f;

                float futureUse = GetFloat(iesDataLines, out iesDataLines);
                float inputWatts = GetFloat(iesDataLines, out iesDataLines);

                m_VerticalAngles = new float[m_VerticalAngleCount];
                float currentMininumVertivalAngle = float.MinValue;

                for (int v = 0; v < m_VerticalAngleCount; ++v)
                {
                    // Usually the angles are separated by whitespaces, but sometimes by commas without whitespaces.
                    float angle = GetFloat(iesDataLines, out iesDataLines, true);
                    if (angle < currentMininumVertivalAngle)
                    {
                        return $"Vertical angles are not in ascending order near: {angle}";
                    }

                    m_VerticalAngles[v] = currentMininumVertivalAngle = angle;
                }

                m_HorizontalAngles = new float[m_HorizontalAngleCount];
                float currentMininumHorizontalAngle = float.MinValue;

                for (int h = 0; h < m_HorizontalAngleCount; ++h)
                {
                    // Usually the angles are separated by whitespaces, but sometimes by commas without whitespaces.
                    float angle = GetFloat(iesDataLines, out iesDataLines, true);
                    if (angle < currentMininumHorizontalAngle)
                    {
                        return $"Horizontal angles are not in ascending order near: {angle}";
                    }

                    m_HorizontalAngles[h] = currentMininumHorizontalAngle = angle;
                }

                m_CandelaValues = new float[m_HorizontalAngleCount * m_VerticalAngleCount];
                m_MaxCandelas = 0f;

                for (int h = 0; h < m_HorizontalAngleCount; ++h)
                {
                    for (int v = 0; v < m_VerticalAngleCount; ++v)
                    {
                        float value = candelaMultiplier * ballastFactor * GetFloat(iesDataLines, out iesDataLines, true);

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

        string GetToken(string line, out string remainder, bool stopOnComma)
        {
            SkipOnWhitespaceAndLineEnd(line, out line, stopOnComma);

            int lineIndex = 0;
            foreach (var c in line)
            {
                if (c == '\r' || c == '\n' || c <= ' ' || (c == ',' && stopOnComma))
                {
                    break;
                }
                ++lineIndex;
            }

            remainder = line.Substring(lineIndex, line.Length - lineIndex);

            return line.Substring(0, lineIndex);
        }

        void SkipOnWhitespaceAndLineEnd(string line, out string remainder, bool stopOnComma)
        {
            int begin = 0;
            int end = line.Length;

            while (begin < end)
            {
                if (line[begin] != '\r' && line[begin] != '\n' && line[begin] > ' ')
                {
                    break;
                }
                ++begin;
            }

            if (stopOnComma)
            {
                while (begin < end)
                {
                    if (line[begin] != ',')
                    {
                        break;
                    }
                    ++begin;
                }
            }

            remainder = line.Substring(begin, line.Length - begin);
        }

        float GetFloat(string line, out string remainder, bool in_StopOnComma = false)
        {
            string token = GetToken(line, out remainder, in_StopOnComma);

            float value;
            if (!float.TryParse(token, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
            {
                value = 1f;
            }

            return value;
        }

        int GetInt(string line, out string remainder)
        {
            string token = GetToken(line, out remainder, false);

            int value;
            if (!int.TryParse(token, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
            {
                value = 1;
            }

            return value;
        }

        public float RemapVerticalAngle(float angle)
        {
            // Input angle in range [0..180]
            if (m_PhotometricType == 1) // type C: angle in range [0..180]
            {
                return angle;
            }
            else // types A and B: angle in range [-90..90]
            {
                return angle - 90f;
            }
        }

        public int GetRemappedHorizontalAngleCount()
        {
            if (m_PhotometricType == 1) // type C
            {
                switch (m_HorizontalAngles[m_HorizontalAngles.Length - 1])
                {
                    case 0f:
                        return 1;
                    case 90f:
                        return 4 * m_HorizontalAngleCount;
                    case 180f:
                        return 2 * m_HorizontalAngleCount;
                    default:
                        return m_HorizontalAngleCount;
                }
            }
            else // types A and B
            {
                if (m_HorizontalAngles[0] == 0f)
                {
                    return 2 * m_HorizontalAngleCount;
                }
                else
                {
                    return m_HorizontalAngleCount;
                }
            }
        }

        public float RemapHorizontalAngle(float angle)
        {
            // Input angle in range [0..360]
            if (m_PhotometricType == 1) // type C: angle in range [0..360]
            {
                switch (m_HorizontalAngles[m_HorizontalAngles.Length - 1])
                {
                    case 0f:
                        return 0f;

                    case 90f:
                        angle %= 180f;
                        return (angle > 90f) ? 180f - angle : angle;

                    case 180f:
                        return (angle > 180f) ? 360f - angle : angle;

                    default:
                        return angle;
                }
            }
            else // types A and B: angle in range [-90..90]
            {
                if (m_HorizontalAngles[0] == 0f)
                {
                    return (angle > 180f) ? 360f - angle : angle;
                }
                else
                {
                    return (angle > 180f) ? -(360f - angle) : angle;
                }
            }
        }

        public float ComputeVerticalAnglePosition(float angle)
        {
            return ComputeAnglePosition(angle, m_VerticalAngles);
        }

        public float ComputeHorizontalAnglePosition(float angle)
        {
            return ComputeAnglePosition(angle, m_HorizontalAngles);
        }

        float ComputeAnglePosition(float value, float[] angles)
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

        public float InterpolateBilinear(float x, float y)
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

        float InterpolatePoint(int x, int y)
        {
            x %= m_HorizontalAngles.Length;
            y %= m_VerticalAngles.Length;

            return m_CandelaValues[y + x * m_VerticalAngles.Length];
        }
    }
}
