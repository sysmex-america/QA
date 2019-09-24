using System;
using System.IO;
using System.Configuration;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Sysmex.Crm.ConsoleApp.Model;
using System.Data.SqlClient;
using SonomaPartners.Crm.Toolkit;
using log4net;

namespace Sysmex.Crm.ConsoleApplication
{
    class RecordPull
    {
        private static ILog Log { get; set; }
        private const string DefaultConnectionStringTemplate = "Url={0}";
        private const string SettingsConnectionStringTemplate = "Url={0};Username={1};Password={2};AuthType={3};RequireNewInstance=true";

        static void Main(string[] args)
        {
            //Initiate Log4Net to log to database
            log4net.Config.XmlConfigurator.Configure();
            Log = log4net.LogManager.GetLogger(typeof(RecordPull));

            //A DateTime Snapshot of when this console app was called
            DateTime lastConsoleRun = DateTime.UtcNow;

            Log.InfoFormat("Console App has started... Current Date/Time: {0}", lastConsoleRun);

            // These are the settings located in the app.config file
            // These variables can be changed by modifying their content in the app.config file
            Log.Debug("Loading Settings and Connecting String from app.config file...");
            ApplicationSettings _settings = new ApplicationSettings()
            {
                CrmUrl = ConfigurationManager.AppSettings["CrmUrl"],
                CrmUsername = ConfigurationManager.AppSettings["CrmUsername"],
                CrmPassword = ConfigurationManager.AppSettings["CrmPassword"],
                CrmAuthenticationType = ConfigurationManager.AppSettings["CrmAuthenticationType"],
                SQLDBConnectionString = ConfigurationManager.ConnectionStrings["SQLDBConnectionString"].ConnectionString,
                UseActiveDirectoryService = Convert.ToBoolean(ConfigurationManager.AppSettings["UseActiveDirectoryService"])
            };

            // Using the default settings, we'll just want to attempt to connect to the Url directly,
            // That will attempt to use Active Directory service for the current logged in user.
            Log.Debug("Formatting CRM connecting string...");
            string crmConnectionString = _settings.UseActiveDirectoryService
                ? String.Format(DefaultConnectionStringTemplate, _settings.CrmUrl)
                : String.Format(SettingsConnectionStringTemplate, _settings.CrmUrl, _settings.CrmUsername, _settings.CrmPassword, _settings.CrmAuthenticationType);

            // CrmServiceClient basically implements IOrganizationService that you would normally use in plugins.
            // This means with it you can do any Retrieve/Create/Update/etc. operations that you might need to do on the organization.
            CrmServiceClient crmService = null;
            IOrganizationService orgService = null;

            try
            {
                Log.InfoFormat("Connecting to CRM Org {0}...", _settings.CrmUrl);
                crmService = new CrmServiceClient(crmConnectionString);

                if (!crmService.IsReady)
                {
                    Log.FatalFormat("There was an error connecting to the CRM Org: {0}", crmService.LastCrmError);
                    EndProgram();
                }
                else
                {
                    orgService = (IOrganizationService)crmService.OrganizationWebProxyClient != null
                        ? (IOrganizationService)crmService.OrganizationWebProxyClient :
                        (IOrganizationService)crmService.OrganizationServiceProxy;
                }
            }
            catch (Exception ex)
            {
                // Connecting may throw an Exception, we just log it to the console and exit the program
                Log.FatalFormat("There was an error connecting to the CRM Org: {0}", ex.Message);
                EndProgram();
            }

            // Print a success message to the console
            Log.InfoFormat("Successfully connected to CRM Org: {0}", _settings.CrmUrl);

            // Connect to SQL Express DB
            Log.Info("Connecting to SQL Database...");
            SqlConnection conn = null;
            try
            {
                conn = new SqlConnection(_settings.SQLDBConnectionString);
                conn.Open();
            }
            catch (Exception ex)
            {
                // Connecting may throw an Exception, we just log it to the console and exit the program
                Log.FatalFormat("There was an error connecting to the SQL Database: {0}", ex.Message);
                EndProgram();
            }

            Log.Info("Successfully connected to DB");

            // Check if customer table exists, if not create it
            CreateTablesIfTheyDoNotExist(conn);

            // Get date & time of most recently updated records
            DateTime maxRunDate = GetMaxRunDate(conn);
            Log.InfoFormat("Last Contact record in Database was on {0}", maxRunDate);

            // query contacts modified after maxRunDate
            QueryExpression queryExpression = BuildQueryExpression(maxRunDate);

            // query contacts modified after max run date
            // insert contact into table
            Log.InfoFormat("Pulling Contact records from CRM with a modified date greater than {0}...", maxRunDate);
            EntityCollection entityCollection = orgService.RetrieveMultipleAll(queryExpression);
            Log.InfoFormat("CRM Contacts retrieved: {0}, inserting them into Database...", entityCollection.Entities.Count);

            foreach (Entity contact in entityCollection.Entities)
            {
                Log.DebugFormat("Inserting CRM contact {0} into database...", contact.Id);
                string commandString = @"INSERT INTO Customer(entity_name, run_date, recordid) VALUES(@entitytype, @modifiedon, @contactid);";
                using (SqlCommand sqlCommand = new SqlCommand(commandString, conn))
                {
                    sqlCommand.Parameters.Add("@entitytype", System.Data.SqlDbType.Char, 50).Value = "contact";
                    sqlCommand.Parameters.Add("@modifiedon", System.Data.SqlDbType.DateTime).Value = lastConsoleRun;
                    sqlCommand.Parameters.Add("@contactid", System.Data.SqlDbType.Char, 32).Value = contact.Id.ToString();
                    sqlCommand.ExecuteNonQuery();
                }
            }
            Log.Info("CRM Contacts into Database operations have completed.");

            // The end function function will just do a Console.ReadKey() so that we display the program output without immediately closing the console.
            // (It also exits the program if called elsewhere, useful for ending on errors).
            EndProgram();
        }

        private static void CreateTablesIfTheyDoNotExist(SqlConnection conn)
        {
            //Create table for Customers
            Log.Debug("Creating 'Customer' table if it does not exist in Database...");
            string commandString = "If not exists (select name from sysobjects where name = 'Customer') CREATE TABLE Customer(entity_name char(50),run_date datetime, recordid char(32))";
            using (SqlCommand sqlCommand = new SqlCommand(commandString, conn))
            {
                sqlCommand.ExecuteNonQuery();
            }

            //Create table for Logging (Log4Net)
            Log.Debug("Creating 'CustomerLog' table if it does not exist in Databse...");
            commandString = "If not exists (select name from sysobjects where name = 'CustomerLog') CREATE TABLE CustomerLog(Id int IDENTITY(1,1) NOT NULL, Date datetime NOT NULL, Thread varchar(255) NOT NULL, Level varchar(255) NOT NULL, Logger varchar(255) NOT NULL, Message varchar(max) NOT NULL, Exception varchar(max) NULL) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]";
            using (SqlCommand sqlCommand = new SqlCommand(commandString, conn))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }

        private static DateTime GetMaxRunDate(SqlConnection conn)
        {
            Log.Debug("Retrieving last record in Customer Database");
            string commandStr = "SELECT MAX(run_date) AS max_run_date FROM Customer";
            //DateTime d = new DateTime();
            using (SqlCommand command = new SqlCommand(commandStr, conn))
            {
                using (SqlDataReader rdr = command.ExecuteReader())
                {
                    // If a date is returned, set d
                    if (rdr.Depth != 0)
                    {
                        Log.Debug("Last record found, using it's DateTime");
                        return (DateTime)rdr["max_run_date"];
                    }
                    else // if no date is returned, our table is empty - set d to past and query all contacts
                    {
                        Log.Debug("No records found, using date of 1/1/2000");
                        return new DateTime(2000, 1, 1);
                    }
                }
            }
        }

        private static QueryExpression BuildQueryExpression(DateTime maxRunDate)
        {
            Log.Debug("Building Query Expression for CRM");
            QueryExpression queryExpression = new QueryExpression();
            queryExpression.EntityName = "contact";
            queryExpression.ColumnSet = new ColumnSet("contactid", "modifiedon");
            queryExpression.ColumnSet.Columns.Add("contactid");
            queryExpression.AddOrder("modifiedon", OrderType.Ascending);
            ConditionExpression conditionExpression = new ConditionExpression("modifiedon", ConditionOperator.GreaterThan, maxRunDate);
            queryExpression.Criteria.AddCondition(conditionExpression);

            return queryExpression;
        }

        /// <summary>
        /// Force end the program from anywhere
        /// </summary>
        private static void EndProgram()
        {
            Log.Info("Console App has stopped running");
            Console.WriteLine("Press any key to exit...");
            // if not running from CMD (e.g. running from VisualStudio), we want to ReadKey to make sure the console doesn't immediately close
            Console.ReadKey();
            Log.Info("Console App has been shut down");
            Environment.Exit(0);
        }
    }
}
