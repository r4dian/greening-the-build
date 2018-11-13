using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Gurock.TestRail;
using System.Text;

namespace greeningthebuild
{
    class MainClass
    {

		public struct Case
        {
            public string SuiteID;
            public string SuiteName;
            public string CreatedOn;
            public string UpdatedOn;
            public int CaseID;
            public string CaseName;
            public string MilestoneName;
			public string MostRecentTestID;
			public string EditorVersion;
			public string Result;
        }


        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
			APIClient client = ConnectToTestrail();

			JArray suitesArray = GetSuitesInProject(client, projectID);

			for (int i = 0; i < suitesArray.Count; i++)
			{
				JObject arrayObject = suitesArray[i].ToObject<JObject>();
				string suiteId = arrayObject.Property("id").Value.ToString();
				string suiteName = arrayObject.Property("name").Value.ToString();

				JArray casesArray = GetCasesInSuite(client, projectID, suiteId);

				List<Case> cases = new List<Case>();

				cases = CreateListOfCases(client, casesArray, cases, suiteId, suiteName, milestoneName);
			}
        }

		private static APIClient ConnectToTestrail()
        {
            APIClient client = new APIClient("http://qatestrail.hq.unity3d.com");
			client.User = "";
			client.Password = ""; //API key
            return client;
        }

		public static JArray GetCasesInSuite(APIClient client, string projectID, string suiteID)
        {
            return (JArray)client.SendGet("get_cases/" + projectID + "&suite_id=" + suiteID);
        }

        public static JArray GetSuitesInProject(APIClient client, string projectID)
        {
            return (JArray)client.SendGet("get_suites/" + projectID);
        }


		public static List<Case> CreateListOfCases(APIClient client, JArray casesArray, List<Case> listOfCases, string suiteID, string suiteName, string milestoneName)
        {
            for (int i = 0; i < casesArray.Count; i++)
            {
                JObject arrayObject = casesArray[i].ToObject<JObject>();

                //allCaseIDs.Add(Int32.Parse(arrayObject.Property("id").Value.ToString()));

                string caseID = arrayObject.Property("id").Value.ToString();
                string caseName = arrayObject.Property("title").Value.ToString();
                string caseType = arrayObject.Property("type_id").Value.ToString();
                string sectionID = arrayObject.Property("section_id").Value.ToString();

                string createdOn = arrayObject.Property("created_on").Value.ToString();
				string updatedOn = arrayObject.Property("updated_on").Value.ToString();        

                Case newCase;
                newCase.SuiteID = suiteID;
                newCase.SuiteName = suiteName;
                newCase.CreatedOn = createdOn;
                newCase.UpdatedOn = updatedOn;
                newCase.CaseID = Int32.Parse(caseID);
                newCase.CaseName = caseName;
                newCase.MilestoneName = milestoneName;

                //.Add(newCase);
            }
            return listOfCases;
        }
    }
}
