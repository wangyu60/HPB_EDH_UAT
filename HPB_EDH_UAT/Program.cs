using ApiUtilLib;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HPB_EDH_UAT
{
    partial class Program
    {
        // application realm
        static string realm = "http://apex_l2_eg"; //according to "EDH - APEX (Internet) REST L2 to L2-EG Guide_v1.2.docx", 2.1

        // authorization prefix (i.e 'Apex_l2_eg' )
        static string authPrefix = "apex_l2_eg"; //according to "APEX_Interface_Specification_v1_3.pdf" page 8 section e

        // app id i.e 'Apex_l2_eg' assign to the application
        static string appId = "edh-5iLlRCGWvZ94ysTfulcy4H1W";

        // api signing gateway name and path (for Intranet i.e <tenant>-pvt.i.api.gov.sg)
        static string signingGateway = "edh.e.api.gov.sg"; //accoridng to "EDH - APEX (Internet) REST L2 to L2-EG Guide_v1.2.docx" section 4.1, should replace edh.api with edh.e.api

        static string apiPath = "test/l2-eg/v1"; //according to "EDH_BIDWH_HPB_UAT_REST_Scenarios_Conditions_v1.0.xlsx", tab "WebService"
        //v1/entities
        //v1/entity/{uen}
        //v1/entity/{uen}/appointments
        

        // private cert file and password
        static string privateCertName = GetLocalPath("Certificates/healthier-choice_hpb_gov_sg.pfx");
        
        static RSACryptoServiceProvider privateKey = ApiAuthorization.PrivateKeyFromP12(privateCertName, Constants.certPassword); // should also use embedded resource.

        // no need append .e on the target gateway name (for Intranet i.e <tenant>-pvt.api.gov.sg)
        static string targetGatewayName = "edh.api.gov.sg";

        public static string testScenarioFileName = "EDH_BIDWH_HPB_UAT_REST_Scenarios_Conditions_v1.0.xlsx";


        static void Main(string[] args)
        {
            LoggerManager.Logger = new FileLogger(LogLevel.Debug);
            
            //var testCase_DT051 = new TestCase { Id=10000, Name="DT051", TestMethod = TestMethod.UENs, UEN= "198102460H" };
            //runTest(testCase_DT051);

            var testCases = ExtractTestCases();

            foreach (var c in testCases.ToList())
            {
                TestCase testCase = c.Value;
                runTest(testCase);
            }
        }

        private static Dictionary<int, TestCase> ExtractTestCases()
        {
            var testCases = new Dictionary<int, TestCase>();

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(testScenarioFileName)); //no brainer

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (ExcelPackage xlPackage = new ExcelPackage(stream))
            {
                var testDataSheet = xlPackage.Workbook.Worksheets["Test Data"];
                var totalRows = testDataSheet.Dimension.End.Row;

                //var strBdr = new StringBuilder();
                for (int rowNum = 2; rowNum <= totalRows; rowNum++) //row 1 is the header
                {
                    var row = testDataSheet.Cells[rowNum, 1, rowNum, 3].Select(c => c.Value == null ? string.Empty : c.Value.ToString());

                    var rawData = row.ToList();
                    //[0]: Test Case Id
                    //[1]: Test Type, parameter
                    //[2]: Test Data, parameter value

                    var name = rawData[0]; //later to be used in comments

                    if (string.IsNullOrEmpty(name))
                        continue; //ignore empty lines

                    var Id = Convert.ToInt32(rawData[0].Substring(2)); //first column of the data, stripped DT and convert to integer

                    if (!testCases.ContainsKey(Id)) //id not found, this is a new test case
                    {
                        //create new test case and add it to the dict

                        var testCase = new TestCase { Name = name, Id = Id };

                        if(!string.IsNullOrEmpty(rawData[1]) && !string.IsNullOrEmpty(rawData[2]) 
                            && rawData[1]!="uen") //"uen" is not a parameter
                            testCase.AddQueryParam(rawData[1], rawData[2]);

                        //test case up to 50 is GET entities test
                        //test case 51 to 61 is GET entity by UEN test
                        //test case id 62 and above is GET entity by UEN and appointments test 

                        if (Id < 51)
                            testCase.TestMethod = TestMethod.Entities;
                        else
                        {
                            if (rawData[1] == "uen")
                                testCase.UEN = rawData[2];

                            if (Id < 62)
                                testCase.TestMethod = TestMethod.UENs;

                            else
                                testCase.TestMethod = TestMethod.Appointments;
                        }

                        testCases.Add(Id, testCase);
                    }
                    else
                    {
                        //update testCases[Id]
                        if (rawData[1] != "uen") // no such case in the data, but just in case
                            testCases[Id].AddQueryParam(rawData[1], rawData[2]);
                    }
                }               

            }

            return testCases;
        }
            
        private static void runTest(TestCase testCase)
        {
            var queryParam = new ApiList();
            foreach (var param in testCase.QueryParams)
            {
                queryParam.Add(param);
            }
            string queryString = queryParam.ToQueryString();

            LoggerManager.Logger.LogInformation($"Test Case {testCase.Name} started. Query string: {queryString}");
            Console.WriteLine($"Test Case {testCase.Name} started.");

            // base URL
            string baseUrl;
            //static string apiPath = "test/l2-eg/v1"; //according to "EDH_BIDWH_HPB_UAT_REST_Scenarios_Conditions_v1.0.xlsx", tab "WebService"
            //v1/entities
            //v1/entity/{uen}
            //v1/entity/{uen}/appointments
            string fullPath;
            switch (testCase.TestMethod)
            {
                case TestMethod.Entities:
                    fullPath = $"{apiPath}/entities";
                    break;
                case TestMethod.UENs:
                    fullPath = $"{apiPath}/entity/{testCase.UEN}";
                    break;
                case TestMethod.Appointments:
                default:
                    fullPath = $"{apiPath}/entity/{testCase.UEN}/appointments";
                    break;
            }

            if (string.IsNullOrEmpty(queryString))
                baseUrl = $"https://{signingGateway}/{fullPath}";
            else
                baseUrl = $"https://{signingGateway}/{fullPath}?{queryString}";

            // authorization header
            var authorizationHeader = ApiAuthorization.Token(realm, authPrefix, HttpMethod.GET, new Uri(baseUrl), appId, null, null, privateKey);

            //target base URL
            string targetBaseUrl;
            if (string.IsNullOrEmpty(queryString))
                targetBaseUrl = $"https://{targetGatewayName}/{fullPath}";
            else
                targetBaseUrl = $"https://{targetGatewayName}/{fullPath}?{queryString}";

            var result = ApiAuthorization.HttpRequest(new Uri(targetBaseUrl), authorizationHeader);            

            LoggerManager.Logger.LogInformation($"Test Case {testCase.Name} ended");
            Console.WriteLine($"Test Case {testCase.Name} ended");
        }
       

        private static string GetLocalPath(string relativeFileName)
        {
            var localPath = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), relativeFileName.Replace('/', Path.DirectorySeparatorChar));

            return localPath;
        }
        
    }
}
