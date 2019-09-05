using System;
using Gurock.TestRail;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace TestRailGraphics
{
    public struct Case
    {
        public string SuiteID;
        public string SuiteName;
        public string CreatedOn;
        public string UpdatedOn;
        public string Section;
        public int CaseID;
        public string CaseName;
        public string Status;
        public string Type;
        public string TemplateStatus;
        public double Estimate;
        public double EstimateForecast;
        public string MilestoneName;
        public string UniqueCaseIdentifier; //this is to identify the case when it is moved between projects and the ID changes
    }
    public class TestRailGraphics
    {
        public static void Main(string[] args)
        {
            APIClient client = ConnectToTestrail();
            string run_id = CreateTestRun(client);
            AddResult(client, run_id, "498297", "5");
        }
        
        public static APIClient ConnectToTestrail()
        {
            APIClient client = new APIClient("https://qatestrail.hq.unity3d.com");
            client.User = "sophia@unity3d.com";
            client.Password = <APIKEY>;
            return client;
            //test
        }
        
        public static string CreateTestRun(APIClient client)
        {
            var data = new Dictionary<string, object>
            {
                { "suite_id", 5123 },
                { "name", "new run" }
            };

            JObject runObject = (JObject)client.SendPost("add_run/100", data);
            string run_id = runObject.Property("id").Value.ToString();
            return run_id;
        }

        public static string AddResult(APIClient client, string run_id, string case_id, string status_id)
        {
            var data = new Dictionary<string, object>
            {
                { "status_id", status_id },
                { "elapsed", "1s" } //get this info from unity test
            };
            
            JObject resultObject = (JObject)client.SendPost(String.Format("add_result_for_case/{0}/{1}", run_id, case_id) , data);
            string result_id = resultObject.Property("id").Value.ToString();
            return result_id;
        }
        
        public static void CreateCase(APIClient client, string caseTitle, string section_id)
        {
            var data = new Dictionary<string, object>
            {
                { "title", caseTitle }
            };

            JObject caseObject = (JObject)client.SendPost(String.Format("add_case/{0}", section_id), data);
        }
    }
}
