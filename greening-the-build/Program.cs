using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Gurock.TestRail;
using System.Text;
using System.IO;

namespace greeningthebuild
{
    class MainClass
    {

		public struct Case
        {
            public string SuiteID;
            public string SuiteName;
            public string CaseID;
            public string CaseName;
            public string MilestoneName;
			//public string MostRecentTestID;
			//public string EditorVersion;
			//public string Result;
        }


        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
			APIClient client = ConnectToTestrail();

			JArray suitesArray = GetSuitesInProject(client, args[0]);

			List<Case> cases = new List<Case>();

			for (int i = 0; i < suitesArray.Count; i++)
			{
				JObject arrayObject = suitesArray[i].ToObject<JObject>();
				string suiteId = arrayObject.Property("id").Value.ToString();
				string suiteName = arrayObject.Property("name").Value.ToString();

				JArray casesArray = GetCasesInSuite(client, args[0], suiteId);



				JObject projectObject = GetProject(client, args[0]);

				string projectName = projectObject.Property("name").Value.ToString();

				string milestoneString = "";
                if (projectName.Contains("Unity "))
                {
                    milestoneString = projectName.Remove(0, 6);
                }

				cases = CreateListOfCases(client, casesArray, cases, suiteId, suiteName, milestoneString);
			}

			string csv = CreateCsvOfCases(cases);

			File.WriteAllText("Cases.csv", csv.ToString());
        }

		private static APIClient ConnectToTestrail()
        {
            APIClient client = new APIClient("https://qatestrail.hq.unity3d.com");
			client.User = "";
			client.Password = ""; //API key
            return client;
        }

		private static string CreateCsvOfCases(List<Case> listOfCases)
        {
            StringBuilder csv = new StringBuilder();

			string header = string.Format("{0},{1},{2},{3},{4},{5}", "Suite ID", "Suite Name", "Case ID", "Title", "Milestone Name", "\n");
            csv.Append(header);

			for (int i = 0; i < listOfCases.Count; i++)
            {
				Case caseObject = listOfCases[i];
                
				string newLine = string.Format("{0},{1},{2},{3},{4},{5}", caseObject.SuiteID, caseObject.SuiteName, caseObject.CaseID, caseObject.CaseName, caseObject.MilestoneName, "\n");
                csv.Append(newLine);
            }

            return csv.ToString();
        }
        
		public static JArray GetCasesInSuite(APIClient client, string projectID, string suiteID)
        {
            return (JArray)client.SendGet("get_cases/" + projectID + "&suite_id=" + suiteID);
        }

        public static JArray GetSuitesInProject(APIClient client, string projectID)
        {
            return (JArray)client.SendGet("get_suites/" + projectID);
        }

		public static JArray GetRuns(APIClient client, string projectID)
        {
            return (JArray)client.SendGet("get_runs/" + projectID);
        }

		public static JArray GetTestsInRun(APIClient client, string runID)
        {
            return (JArray)client.SendGet("get_tests/" + runID);
        }

		public static JArray GetLatestResultsOfTest(APIClient client, string testID, string amountOfResultsToShow)
        {
            return (JArray)client.SendGet("get_results/" + testID + "&limit=" + amountOfResultsToShow);
        }

		public static JArray GetLatestResultsForCase(APIClient client, string runID, string caseID, string amountOfResultsToShow)
        {
            return (JArray)client.SendGet("get_results_for_case/" + runID + "/" + caseID + "&limit=" + amountOfResultsToShow);
        }

		public static JObject GetProject(APIClient client, string projectID)
        {
            return (JObject)client.SendGet("get_project/" + projectID);
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
				newCase.CaseID = caseID;
                newCase.CaseName = caseName;
                newCase.MilestoneName = milestoneName;

				listOfCases.Add(newCase);
            }
            return listOfCases;
        }
    }
}
