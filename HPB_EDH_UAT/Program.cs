using ApiUtilLib;
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
    class Program
    {
        // application realm
        static string realm = "http://apex_l2_eg"; //according to "EDH - APEX (Internet) REST L2 to L2-EG Guide_v1.2.docx", 2.1

        // authorization prefix (i.e 'Apex_l2_eg' )
        static string authPrefix = "apex_l2_eg"; //according to "APEX_Interface_Specification_v1_3.pdf" page 8 section e

        // app id i.e 'Apex_l2_eg' assign to the application
        static string appId = "edh-5iLlRCGWvZ94ysTfulcy4H1W";

        // api signing gateway name and path (for Intranet i.e <tenant>-pvt.i.api.gov.sg)
        static string signingGateway = "edh.e.api.gov.sg"; //accoridng to "EDH - APEX (Internet) REST L2 to L2-EG Guide_v1.2.docx" section 4.1, should replace edh.api with edh.e.api

        static string apiPath = "test/l2-eg/v1/entities"; //according to "EDH_BIDWH_HPB_UAT_REST_Scenarios_Conditions_v1.0.xlsx", tab "WebService"

        // private cert file and password
        static string privateCertName = GetLocalPath("Certificates/healthier-choice_hpb_gov_sg.pfx");
        static string password = "setCorrectPasswordHere";
        static RSACryptoServiceProvider privateKey = ApiAuthorization.PrivateKeyFromP12(privateCertName, password);

        // no need append .e on the target gateway name (for Intranet i.e <tenant>-pvt.api.gov.sg)
        static string targetGatewayName = "edh.api.gov.sg";
        //string targetBaseUrl = string.Format("https://{0}/{1}?{2}", targetGatewayName, apiPath, queryString);
        static string targetBaseUrl = $"https://{targetGatewayName}/{apiPath}";

        static void Main(string[] args)
        {
            LoggerManager.Logger = new FileLogger(LogLevel.Debug);
            DT001();
        }

        private static void DT001()
        {
            LoggerManager.Logger.LogInformation("Test Case DT001 started");
            // base URL
            string baseUrl = $"https://{signingGateway}/{apiPath}";

            // authorization header
            var authorizationHeader = ApiAuthorization.Token(realm, authPrefix, HttpMethod.GET, new Uri(baseUrl), appId, null, null, privateKey);

            var result = ApiAuthorization.HttpRequest(new Uri(targetBaseUrl), authorizationHeader);
            //using default values for HTTP method, null for form data and 'false' for ignoreServerCert

            LoggerManager.Logger.LogInformation("Test Case DT001 ended");
        }

        private static string GetLocalPath(string relativeFileName)
        {
            var localPath = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), relativeFileName.Replace('/', Path.DirectorySeparatorChar));

            return localPath;
        }
    }
}
