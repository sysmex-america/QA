using System;
using System.IO;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Sysmex.Crm.ConsoleApplication.Model;

namespace Sysmex.Crm.ConsoleApplication
{
    class CrmConsoleConnection
    {
        private const string DefaultConnectionStringTemplate = "Url={0}";
        private const string SettingsConnectionStringTemplate = "Url={0};Username={1};Password={2};AuthType={3};RequireNewInstance=true";

        private static ApplicationSettings _settings;

        static void Main(string[] args)
        {
            // If you run the program with arguments "-s <settings file>.json", we will load the settings using those settings.
            // This allows us to connect to different organizations/connect as different users easily without having to recompile the program.
            _settings = args.Length == 2 && args[0].ToLower() == "-s"
                    ? LoadApplicationSettings(args[1])
                    : new ApplicationSettings();

            // Using the default settings, we'll just want to attempt to connect to the Url directly,
            // That will attempt to use Active Directory service for the current logged in user.
            var connectionString = _settings.IsDefaultSettings
                ? string.Format(DefaultConnectionStringTemplate, _settings.CrmUrl)
                : string.Format(SettingsConnectionStringTemplate, _settings.CrmUrl, _settings.Username, _settings.Password, _settings.AuthenticationType);

            // CrmServiceClient basically implements IOrganizationService that you would normally use in plugins.
            // This means with it you can do any Retrieve/Create/Update/etc. operations that you might need to do on the organization.
            CrmServiceClient crmService = null;

            try
            {
                crmService = new CrmServiceClient(connectionString);

                if (!crmService.IsReady)
                {
                    Error(crmService.LastCrmError);
                }
            }
            catch (Exception ex)
            {
                // Connecting may throw an Exception, we just log it to the console and exit the program
                Error(ex.Message);
            }

            // Print a success message to the console
            Success("Successfully connected to CRM Org: {0}", _settings.CrmUrl);

            // Do whatever type of work from here on, passing the CrmServiceClient object to any custom functions
            ExecuteWhoAmIRequest(crmService);

            // The end function function will just do a Console.ReadLine() so that we display the program output without immediately closing the console.
            // (It also exits the program if called elsewhere, useful for ending on errors).
            EndProgram();
        }

        /// <summary>
        /// Execute a WhoAmIRequest against the organization.
        /// </summary>
        /// <param name="orgService">A CrmServiceClient that's connected to an organization.</param>
        private static void ExecuteWhoAmIRequest(IOrganizationService orgService)
        { 
            var whoAmIRequest = new WhoAmIRequest();
            var whoAmIResponse = (WhoAmIResponse)orgService.Execute(whoAmIRequest);
            
            // Simply printing out the Ids for the org/user/business unit that's connected
            Console.WriteLine("You are connected to organization: {0}", whoAmIResponse.OrganizationId);
            Console.WriteLine("You are logged in as user: {0}", whoAmIResponse.UserId);
            Console.WriteLine("Your business unit is: {0}", whoAmIResponse.BusinessUnitId);
        }

        /// <summary>
        /// Load and deserialze a JSON file for that handles the settings for the program
        /// </summary>
        /// <param name="settingsFile">A JSON file that has dynamic settings for the program to determine org/user.</param>
        /// <returns>An instance of ApplicationSettings that will be used by the rest of the program.</returns>
        private static ApplicationSettings LoadApplicationSettings(string settingsFile)
        {
            // Create an object that points to the file to check if it exists before attempting to open it
            var filePtr = new FileInfo(settingsFile);
            if (!filePtr.Exists)
            {
                Error("Settings file {0} could not be found.", settingsFile);
            }

            try
            {
                using (var sr = new StreamReader(filePtr.OpenRead()))
                {
                    var settingsJson = sr.ReadToEnd(); // read the file from a streamreader (easier than directly from the file stream)

                    var appSettings = JsonConvert.DeserializeObject<ApplicationSettings>(settingsJson);
                    appSettings.IsDefaultSettings = false; // since we're loading the settings dynamically, we are not using defaults

                    return appSettings;
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Takes in a format string and arguments to print an error to the console and then ends the program.
        /// </summary>
        /// <param name="text">Error message format</param>
        /// <param name="args">Any arguments for the error message</param>
        private static void Error(string text, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red; // print the error in red to make it obvious that it's an error.
            Console.Error.WriteLine("Error: " + text, args);
            EndProgram();
        }

        /// <summary>
        /// Takes in a format string and arguments to print green success message to the console.
        /// </summary>
        /// <param name="text">Success message format</param>
        /// <param name="args">Any arguments for the success message</param>
        private static void Success(string text, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.WriteLine(text, args);
            Console.ResetColor();
        }

        /// <summary>
        /// Force end the program from anywhere
        /// </summary>
        private static void EndProgram()
        {
            // if not running from CMD (e.g. running from VisualStudio), we want to ReadLine to make sure the console doesn't immediately close
            Console.ReadLine(); 
            Environment.Exit(0);
        }
    }
}
