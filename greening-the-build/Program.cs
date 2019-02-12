using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Gurock.TestRail;
using System.Text;
using System.IO;
using System.Globalization;
using System.Linq;

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
			public string MostRecentRunID;
			public string MostRecentTestID;
			public string EditorVersion;
			public int RawEditorVersion;
			public string Result;
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
			public string Result;
			public int CompletedOn;
			public string EditorVersion;
			public int RawEditorVersion;
		}

		public static List<Run> runs = new List<Run>();


        public static void Main(string[] args)
        {
			Console.WriteLine("HOW GREEN IS YOUR LOVE");
			APIClient client = ConnectToTestrail();

			JArray suitesArray = GetSuitesInProject(client, args[0]);

            Console.WriteLine("Project ID: " + args[0]);

            List<Case> cases = new List<Case>();

			JArray runsArray = GetRuns(client, args[0]);

			JArray plansArray = GetPlans(client, args[0]);

			CreateListOfRuns(runsArray, runs);

			GetRunsInPlan(plansArray, client, runs);

			JObject projectObject = GetProject(client, args[0]);

            string projectName = projectObject.Property("name").Value.ToString();

			List<Test> tests = new List<Test>();
			int count = 0;
			foreach (Run run in runs)
			{
				count++;
				List<Test> currentTests = new List<Test>();

				JArray testsArray = GetTestsInRun(client, run.RunID);
				currentTests = CreateListOfTests(client, testsArray, args[0]);

				tests.AddRange(currentTests);
			}

			for (int i = 0; i < suitesArray.Count; i++)
            {
                JObject arrayObject = suitesArray[i].ToObject<JObject>();
                string suiteId = arrayObject.Property("id").Value.ToString();
                string suiteName = arrayObject.Property("name").Value.ToString();

                JArray casesArray = GetCasesInSuite(client, args[0], suiteId);            

                string milestoneString = "";
                if (projectName.Contains("Unity "))
                {
                    milestoneString = projectName.Remove(0, 6);
                }

				List<Case> casesInSuite = CreateListOfCases(casesArray, suiteId, suiteName, milestoneString, tests);
				cases.AddRange(casesInSuite);
			}

			Case highestCase = cases.Aggregate((i1, i2) => i1.RawEditorVersion > i2.RawEditorVersion ? i1 : i2);
			string maxEditorVersion = highestCase.EditorVersion;

			string csv = CreateCsvOfCases(cases, maxEditorVersion, args[0]);

			File.WriteAllText("Cases"+ args[0] + ".csv", csv.ToString());
            Console.WriteLine("Goodbye World!");
        }

		private static APIClient ConnectToTestrail()
        {
            APIClient client = new APIClient("https://qatestrail.hq.unity3d.com");
			client.User = "";
			client.Password = ""; //API key
            return client;
        }

		private static string CreateCsvOfCases(List<Case> listOfCases, string maxEditorVersion, string projectID)
        {
            StringBuilder csv = new StringBuilder();

			string header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", "Identifier", "Suite ID", "Suite Name", "Case ID", "Title", "Milestone Name", "Run ID", "Test ID", "Editor Version", "Editor Version Num", "Result", "Max Editor Version", "Max Editor Version Num", "\n");
            csv.Append(header);
            
			for (int i = 0; i < listOfCases.Count; i++)
            {
				Case caseObject = listOfCases[i];

                if (caseObject.Result == "")
                {
                    caseObject.Result = "In Progress";
                }
                
				string newLine = caseObject.CaseID + "-" + projectID + "-" + maxEditorVersion +","+ caseObject.SuiteID + "," + "\""+ caseObject.SuiteName +"\"" + ","+ caseObject.CaseID + ","+"\"" + caseObject.CaseName + "\"" + "," + caseObject.MilestoneName + "," + caseObject.MostRecentRunID + "," + caseObject.MostRecentTestID + "," + caseObject.EditorVersion + "," + GetVersionInt(caseObject.EditorVersion) +"," + caseObject.Result +","+ maxEditorVersion + "," + GetMaxVersionInt(maxEditorVersion) + ",\n";
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

		public static JArray GetResultsFields(APIClient client)
        {
            return (JArray)client.SendGet("get_result_fields");
        }

		public static JArray GetStatuses(APIClient client)
        {
            return (JArray)client.SendGet("get_statuses");
        }


		public static List<Case> CreateListOfCases(JArray casesArray, string suiteID, string suiteName, string milestoneName, List<Test> tests)
        {
			List<Case> listOfCases = new List<Case>();
            for (int i = 0; i < casesArray.Count; i++)
            {
                JObject arrayObject = casesArray[i].ToObject<JObject>();

                string caseID = arrayObject.Property("id").Value.ToString();
                string caseName = arrayObject.Property("title").Value.ToString();
                string caseType = arrayObject.Property("type_id").Value.ToString();
                string sectionID = arrayObject.Property("section_id").Value.ToString();

                string createdOn = arrayObject.Property("created_on").Value.ToString();
				string updatedOn = arrayObject.Property("updated_on").Value.ToString();

                
				List<Test> testsWithCaseID = tests.FindAll(x => x.CaseID == caseID);

				Test recentTest;
				if (testsWithCaseID.Count == 0)
				{
					Case untestedCase;
					untestedCase.SuiteID = suiteID;
					untestedCase.SuiteName = suiteName;
					untestedCase.CaseID = caseID;
					untestedCase.CaseName = caseName;
					untestedCase.MilestoneName = milestoneName;
					untestedCase.MostRecentTestID = "0000";
					untestedCase.MostRecentRunID = "0000";
					untestedCase.Result = "Untested";
					untestedCase.EditorVersion = "None";
					untestedCase.RawEditorVersion = 00;

					listOfCases.Add(untestedCase);
					continue;
				}
				else if (testsWithCaseID.Count == 1)
				{
					recentTest = testsWithCaseID[0];
				}
				else
				{
					// Find the test with the most recent date
					recentTest = testsWithCaseID.Aggregate((i1, i2) => i1.CompletedOn > i2.CompletedOn ? i1 : i2);
				}

                Case newCase;
                newCase.SuiteID = suiteID;
                newCase.SuiteName = suiteName;
				newCase.CaseID = caseID;
                newCase.CaseName = caseName;
                newCase.MilestoneName = milestoneName;
				newCase.MostRecentTestID = recentTest.TestID;
				newCase.MostRecentRunID = recentTest.RunID;
				newCase.Result = recentTest.Result;
				newCase.EditorVersion = recentTest.EditorVersion;

				if (recentTest.EditorVersion.Contains("None"))
					newCase.RawEditorVersion = 00;
				else
					newCase.RawEditorVersion = int.Parse(GetRawEditorVersionInt(recentTest.EditorVersion));

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

		public static List<Test> CreateListOfTests(APIClient client, JArray testsArray, string projectID)
		{
			JArray statusArray = GetStatuses(client);

			List<Test> tests = new List<Test>();

			for (int i = 0; i < testsArray.Count; i++)
			{
				JObject arrayObject = testsArray[i].ToObject<JObject>();
                
				string testID = arrayObject.Property("id").Value.ToString();
				string caseID = arrayObject.Property("case_id").Value.ToString();
				string runID = arrayObject.Property("run_id").Value.ToString();

				string editorVersion = "None";
				int completedDate = 0;
				string result = "In Progress";
				int rawEditorVersion = 0;

				JArray resultsOfLatestTest = GetLatestResultsOfTest(client, testID, "1");

				if (resultsOfLatestTest.Count > 0)
				{
					for (int k = 0; k < resultsOfLatestTest.Count; k++)
					{
						JObject resultObject = resultsOfLatestTest[k].ToObject<JObject>();

						//if ((resultObject.Property("custom_editorversion").Value != null) || (resultObject.Property("custom_editorversion") != null) || (resultObject.Property("custom_editorversion").Value.ToString() == ""))
						if (resultObject.Property("custom_editorversion").Value.ToString() != "")
						{
							rawEditorVersion = Int32.Parse(resultObject.Property("custom_editorversion").Value.ToString());
                            editorVersion = GetEditorVersion(client, projectID, rawEditorVersion.ToString());
						}
						completedDate = Int32.Parse(resultObject.Property("created_on").Value.ToString());
						string resultID = resultObject.Property("status_id").Value.ToString();
						result = GetStatus(statusArray, resultID);
					}
				}

				Test test;
				test.TestID = testID;
				test.CaseID = caseID;
				test.RunID = runID;
				test.Result = result;
				test.EditorVersion = editorVersion;
				test.CompletedOn = completedDate;
				test.RawEditorVersion = rawEditorVersion;

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

        public static string GetVersionInt(string editorVersion)
		{
			string versionInt = "";
			if (editorVersion.Contains("Alpha"))
			{
				versionInt = editorVersion.Replace("Alpha ", "1_");
			}
			else if (editorVersion.Contains("Beta"))
			{
				versionInt = editorVersion.Replace("Beta ", "2_");
			}
			else if (editorVersion.Contains("f"))
			{
				versionInt = editorVersion.Replace("f", "3_");
			}

			return versionInt;
		}

		public static string GetRawEditorVersionInt(string editorVersion)
		{
			string versionInt = "";
			
			if (editorVersion.Contains("Alpha"))
			{
				string[] versionStr = editorVersion.Split(' ');
				versionInt = versionStr[1].Length > 1 ? "1" + versionStr[1] : "10" + versionStr[1];
			}
			else if (editorVersion.Contains("Beta"))
			{
				string[] versionStr = editorVersion.Split(' ');
				versionInt = versionStr[1].Length > 1 ? "2" + versionStr[1] : "20" + versionStr[1];
			}
			else if (editorVersion.Contains("f"))
			{
				versionInt = editorVersion.Replace("f", "");
				versionInt = versionInt.Length > 1 ? "3" + versionInt : "30" + versionInt;
			}
			else if (editorVersion.Contains("RC"))
			{
				versionInt = editorVersion.Replace("RC", "");
				versionInt = versionInt.Length > 1 ? "3" + versionInt : "30" + versionInt;
			}

			return versionInt;
		}

		public static string GetMaxVersionInt(string maxEditorVersion)
        {
            string versionInt = "";
			if (maxEditorVersion.Contains("Alpha"))
            {
				versionInt = maxEditorVersion.Replace("Alpha ", "1_");
            }
			else if (maxEditorVersion.Contains("Beta"))
            {
				versionInt = maxEditorVersion.Replace("Beta ", "2_");
            }
			else if (maxEditorVersion.Contains("f"))
            {
				versionInt = maxEditorVersion.Replace("f", "3_");
            }

            return versionInt;
        }

		public static string GetStatus(JArray statusArray, string rawValue)
        {
            string statusName = "";

            for (int i = 0; i < statusArray.Count; i++)
            {
                JObject caseType = statusArray[i].ToObject<JObject>();

                if (caseType.Property("id").Value.ToString() == rawValue)
                {
                    statusName = caseType.Property("name").Value.ToString();

                    if (statusName == "untested")
                    {
                        statusName = "In Progress";
                    }
                    break;
                }
            }

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            statusName = textInfo.ToTitleCase(statusName);

            return statusName;
        }
    }
}
