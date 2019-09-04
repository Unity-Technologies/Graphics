using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using System.Net;

namespace Tests
{
    public class NewTestScript
    {
        //TestRailGraphics trg = new TestRailGraphics();
        // A Test behaves as an ordinary method
        [Test]
        public void NewTestScriptSimplePasses()
        {
            // Use the Assert class to test conditions
            //TestRailGraphics
            APIClient client = TestRailGraphics.ConnectToTestrail();
            string run_id = TestRailGraphics.CreateTestRun(client);
            TestRailGraphics.AddResult(client, run_id, "498297", "5");
        }

    }

}
