using System;
using System.Configuration;
using System.Configuration.Provider;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk.Query;

namespace ConsoleApplicationTest
{
    class Program
    {
        public const string Dev = "Server=https://sysmexdev.crm.dynamics.com;AuthType=Office365;Username=martinh@sysmex.com;Password=Thisis15long!23;RequireNewInstance=true;";
        static void Main(string[] args)
        {
            Console.WriteLine("Starting connection");
            var connectionString = ConfigurationManager.ConnectionStrings["Xrm"].ConnectionString;

            //var orgService = BuildOrgService(Dev);
            var orgService = BuildOrgService(connectionString);
            Console.WriteLine($"Connection String: {connectionString}");

            var fetch = string.Format(@"<fetch top='3'>
                              <entity name='systemuser'>
                                <all-attributes />
                              </entity>
                            </fetch>");

            var result = orgService.RetrieveMultiple(new FetchExpression(fetch));
            if (result.Entities.Count > 0)
            {
                foreach (Entity e in result.Entities)
                {
                    Console.WriteLine(e.Id);
                }
            }
            Console.WriteLine("ending");
            Console.ReadKey();
        }
        public static IOrganizationService BuildOrgService(string connectionString)
        {
            if (!connectionString.EndsWith(";"))
            {
                connectionString += ";";
            }
            if (!connectionString.Contains("AuthType"))
            {
                connectionString += "AuthType=IFD;";
            }
            if (!connectionString.Contains("RequireNewInstance"))
            {
                connectionString += "RequireNewInstance=true;";
            }
            if (connectionString.Contains("AuthType=Office365") && !connectionString.Contains("SkipDiscovery"))
            {
                connectionString += "SkipDiscovery=true;";
            }

            var crmService = new CrmServiceClient(connectionString);
            if (crmService.IsReady)
            {
                Console.WriteLine("Success");
                return crmService;
            }
            else
            {
                throw new Exception(crmService.LastCrmError);
            }
        }

        public static OrganizationServiceProxy BuildOrgServiceProxy(string orgUrl, string userName, string password)
        {
            // Allows impersonation by setting CallerId after initial authentication with a sufficiently privileged user, unlike IOrganizationService
            if (!orgUrl.EndsWith("/"))
            {
                orgUrl += "/";
            }

            //orgUrl += "XRMServices/2011/Organization.svc";

            var credentials = new ClientCredentials();
            credentials.UserName.UserName = userName;
            credentials.UserName.Password = password;

            return new OrganizationServiceProxy(new Uri(orgUrl), null, credentials, null);
        }

    }
}
