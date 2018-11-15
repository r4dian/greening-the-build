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
			//public string MostRecentRunID;
			public string MostRecentTestID;
			//public string EditorVersion;
			//public string Result;
        }

        public struct Run
		{
			public string RunID;
			public string IsCompleted;
			public string CompletedOn;
		}

        public struct Test
		{
			public string TestID;
			public string RunID;
			public string CaseID;
		}

		public static List<Run> runs;


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

				cases = CreateListOfCases(casesArray, suiteId, suiteName, milestoneString);
			}

			JArray runsArray = GetRuns(client, args[0]);

			JArray plansArray = GetPlans(client, args[0]);

			CreateListOfRuns(runsArray, runs);

			GetRunsInPlan(plansArray, client, runs);

			List<Test> tests = new List<Test>();
			foreach (Run run in runs)
			{
				List<Test> currentTests = new List<Test>();

				JArray testsArray = GetTestsInRun(client, run.RunID);
				currentTests = CreateListOfTests(testsArray);

				tests.AddRange(currentTests);
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

		public static JArray GetPlans(APIClient client, string projectID)
        {
            return (JArray)client.SendGet("get_plans/" + projectID);
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


		public static List<Case> CreateListOfCases(JArray casesArray, string suiteID, string suiteName, string milestoneName)
        {
			List<Case> listOfCases = new List<Case>();
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


		public static void GetRunsInPlan(JArray planArray, APIClient client, List<Run> runs)
        {
            List<JArray> ListOfRunsInPlan = new List<JArray>();
            List<string> runInPlanIds = new List<string>();

            for (int i = 0; i < planArray.Count; i++)
            {
                JObject arrayObject = planArray[i].ToObject<JObject>();

                string planID = arrayObject.Property("id").Value.ToString();
                //planIds.Add(planID);

                //string planName = arrayObject.Property("name").Value.ToString();

				JObject singularPlanObject = (JObject)client.SendGet("get_plan/" + planID);

                JProperty prop = singularPlanObject.Property("entries");
                if (prop != null && prop.Value != null)
                {
                    JArray entries = (JArray)singularPlanObject.Property("entries").First;

                    for (int k = 0; k < entries.Count; k++)
                    {
                        JObject entriesObject = entries[k].ToObject<JObject>();


                        JArray runsArray = (JArray)entriesObject.Property("runs").First;

                        for (int j = 0; j < runsArray.Count; j++)
                        {
                            JObject runObject = runsArray[j].ToObject<JObject>();


                            string runInPlanId = runObject.Property("id").Value.ToString();

                            Run run;
                            run.RunID = runInPlanId;
                            run.IsCompleted = runObject.Property("is_completed").Value.ToString();
                            run.CompletedOn = runObject.Property("completed_on").Value.ToString();
                            runs.Add(run);
                        }
                    }
                }
            }
        }

		public static void CreateListOfRuns(JArray runsArray, List<Run> runs)
		{
			for (int i = 0; i < runsArray.Count; i++)
			{
				JObject arrayObject = runsArray[i].ToObject<JObject>();

				string run_id = arrayObject.Property("id").Value.ToString();
				string isCompleted = arrayObject.Property("is_completed").Value.ToString();
				string completedOn = arrayObject.Property("completed_on").Value.ToString();

				Run run;
                run.RunID = run_id;
				run.IsCompleted = isCompleted;
				run.CompletedOn = completedOn;

				runs.Add(run);
			}
		}

		public static List<Test> CreateListOfTests(JArray testsArray)
		{
			List<Test> tests = new List<Test>();

			for (int i = 0; i < testsArray.Count; i++)
			{
				JObject arrayObject = testsArray[i].ToObject<JObject>();

				string testID = arrayObject.Property("id").Value.ToString();
				string caseID = arrayObject.Property("case_id").Value.ToString();
				string runID = arrayObject.Property("run_id").Value.ToString();

				Test test;
				test.TestID = testID;
				test.CaseID = caseID;
				test.RunID = runID;

				tests.Add(test);
			}

			return tests;
		}

		public static string GetEditorVersion(APIClient client, string projectID, string rawValue)
        {
            JArray resultFieldsArray = GetResultsFields(client);

            for (int i = 0; i < resultFieldsArray.Count; i++)
            {
                JObject resultObject = resultFieldsArray[i].ToObject<JObject>();

                if (resultObject.Property("name").Value.ToString() == "editorversion")
                {
                    JProperty configs = resultObject.Property("configs");

                    foreach (JArray child in configs.OfType<JArray>())
                    {
                        for (int k = 0; k < child.Count; k++)
                        {
                            JObject contextInner = (JObject)child[k];

                            for (int m = 0; m < contextInner.Count; m++)
                            {
                                JObject context = (JObject)contextInner["context"];
                                JArray projectIds = (JArray)context["project_ids"];


                                for (int j = 0; j < projectIds.Count; j++)
                                {
                                    var projectObject = projectIds[j];
                                    if (projectObject.ToString() == projectID)
                                    {
                                        //get list of editor versions
                                        JObject options = (JObject)contextInner["options"];

                                        string versions = options.Property("items").Value.ToString();
                                        string[] editorVersions = versions.ToString().Split('\n');
                                        foreach (string editorVersion in editorVersions)
                                        {
                                            string[] values = editorVersion.Split(',');
                                            string id = values[0];
                                            string name = values[1];
                                            if (id == rawValue)
                                            {
                                                return name;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }


                }
            }
            return "";
        }
    }
}
