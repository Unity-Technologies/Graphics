using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace TestRailGraphics.Configuration
{
    public class ConfigReader : IConfigReader
    {
        private JObject _currentJObject;
        private readonly string _configLocation;
        private readonly object _lock = new object();
        private const string DEFAULT_LOCATION = @"configuration/config.json";
        private const string TESTRAILUSER = "testrail:user";
        private const string TESTRAILPASS = "testrail:password";

        public ConfigReader() : this(DEFAULT_LOCATION) { }
        public ConfigReader(string configurationFile)
        {
            _configLocation = configurationFile;
        }

        public bool TestrailEnabled { get; set; } = true;

        public string TestRailUser => GetConfigEntry<string>(TESTRAILUSER);
        public string TestRailPass => GetConfigEntry<string>(TESTRAILPASS);

        public T GetConfigEntry<T>(string entryName)
        {
            return GetJObject().Value<T>(entryName);
        }

        private JObject GetJObject()
        {
            lock (_lock)
            {
                if (_currentJObject == null)
                {
                    string assemblyLocation = AssemblyLocation();
                    string fileName = Path.Combine(assemblyLocation, _configLocation);
                    string json = File.ReadAllText(fileName);
                    _currentJObject = JObject.Parse(json);
                }
            }

            return _currentJObject;
        }

        private string AssemblyLocation()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var codebase = new Uri(assembly.CodeBase);
            var path = Path.GetDirectoryName(codebase.LocalPath);
            return path;
        }
    }
}