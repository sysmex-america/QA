using System;
using System.IO;
using System.Data;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using SonomaPartners.Crm.Toolkit;
using log4net;

using Sysmex.Crm.ConsoleApp.Model;
using Sysmex.Pivotal.DAAB;
using Sysmex.DAL;
using System.Runtime.InteropServices;
using System.Security;

using CdcSoftware.Pivotal.Engine;

namespace Sysmex.Crm.ConsoleApp.Model
{
    class CrmToPivotal
    {
        //private static ILog Log { get; set; }
        private const string DefaultConnectionStringTemplate = "Url={0}";
        private const string SettingsConnectionStringTemplate = "Url={0};Username={1};Password={2};AuthType={3};RequireNewInstance=true";

        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static ApplicationSettings _settings;
               

        static void Main(string[] args)
        {
            //Setup fields

            int accounts = 0;
            int labs = 0;
            int instruments = 0;
            int addresses = 0;
            int contacts = 0;
            int accountsm = 0;
            int labsm = 0;
            int instrumentsm = 0;
            int addressesm = 0;
            int contactsm = 0;
            int accountsr = 0;
            int labsr = 0;
            int instrumentsr = 0;
            int addressesr = 0;
            int contactsr = 0;

            //Initiate Log4Net to log to database
            //log4net.Config.XmlConfigurator.Configure();
            //Log = log4net.LogManager.GetLogger(typeof(RecordPull));
            string templatePath = ConfigurationManager.AppSettings["LogFilePath"];

            log4net.Config.XmlConfigurator.Configure();
            log.Info("CRM Context Setup");

            //A DateTime Snapshot of when this console app was called
            DateTime lastConsoleRun = DateTime.UtcNow;

            log.InfoFormat("Console App has started... Current Date/Time: {0}", lastConsoleRun);
            LogMessage(templatePath, "Console App has started... Current Date/Time: " + lastConsoleRun);

            // These are the settings located in the app.config file
            // These variables can be changed by modifying their content in the app.config file
            log.Debug("Loading Settings and Connecting String from app.config file...");
            log.Info("SQL Command Setup");

            //// If you run the program with arguments "-s <settings file>.json", we will load the settings using those settings.
            //// This allows us to connect to different organizations/connect as different users easily without having to recompile the program.
            //ApplicationSettings _settings = args.Length == 2 && args[0].ToLower() == "-s"
            //        ? LoadApplicationSettings(args[1])
            //        : new ApplicationSettings();

            // Using the default settings, we'll just want to attempt to connect to the Url directly,
            // That will attempt to use Active Directory service for the current logged in user.

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
            log.Debug("Formatting CRM connecting string...");
            string crmConnectionString = _settings.UseActiveDirectoryService
                ? string.Format(DefaultConnectionStringTemplate, _settings.CrmUrl)
                : String.Format(SettingsConnectionStringTemplate, _settings.CrmUrl, _settings.CrmUsername, _settings.CrmPassword, _settings.CrmAuthenticationType);


            // CrmServiceClient basically implements IOrganizationService that you would normally use in plugins.
            // This means with it you can do any Retrieve/Create/Update/etc. operations that you might need to do on the organization.
            CrmServiceClient crmService = null;
            IOrganizationService orgService = null;

            //Setup Pivotal connections to DAAB and SQL
            string businessServer = ConfigurationManager.AppSettings["BusinessServer"];
            string systemName = ConfigurationManager.AppSettings["SystemName"];

            string domain = ConfigurationManager.AppSettings["Domain"];
            string domainPassword = ConfigurationManager.AppSettings["DomainPassword"];

            DAAB daab = new DAAB();
            //daab.Connect(new Uri(businessServer), systemName);

            string sqlCommand = "";
            string sedConn = @"Data Source=" + DD.strServer + ";Integrated Security=SSPI;user id=" + DD.strUser + ";password=" + DD.strPSW + ";database=" + DD.strED;
            SqlConnection sqlConn = new SqlConnection(sedConn);
            //sqlConn.Open();

            Guid importLogRecordId = Guid.Empty;            

            //Instaniate an account object, set the entity
            Entity logrec = new Entity("smx_importlog");
            Entity eaccount = new Entity("account");
            Entity eaddress = new Entity("smx_address");
            Entity econtact = new Entity("contact");
            Entity elab = new Entity("smx_lab");
            Entity eihn = new Entity("smx_ihn");
            Entity einstrument = new Entity("smx_instrument");

            // CrmServiceClient basically implements IOrganizationService that you would normally use in plugins.
            // This means with it you can do any Retrieve/Create/Update/etc. operations that you might need to do on the organization.

            try
            {
                log.InfoFormat("Connecting to CRM Org {0}...", _settings.CrmUrl);
                crmService = new CrmServiceClient(crmConnectionString);

                if (!crmService.IsReady)
                {
                    log.FatalFormat("There was an error connecting to the CRM Org: {0}", crmService.LastCrmError);
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
                log.FatalFormat("There was an error connecting to the CRM Org: {0}", ex.Message);
                EndProgram();
            }

            // Print a success message to the console
            Success("Successfully connected to CRM Org: {0}", _settings.CrmUrl);

            // Do whatever type of work from here on, passing the CrmServiceClient object to any custom functions
            ExecuteWhoAmIRequest(crmService);

            //Start work here
            //Create a query expression specifying the link entity alias and the columns of the link entity that you want to return

            //Go fetch the last entry made in the logfile or customized entity with a datetimestamp ##
            //DateTime lastcontactfetchdate = DateTime.Today.AddDays(-5);
            DateTime lastfetchdate = GetMaxRunDate(sqlConn);
            
            //Go fetch all the records
            //List all fields from contact
            try
            {
                #region accounts
                //to be deleted                
                //lastfetchdate = Convert.ToDateTime("2017-03-15 10:40:00.000");

                ////Start Account Read Write, Query Account modified added after maxRunDate
                string pivotalaccountid = "";
                bool completedaccounts = false;
                bool accountadresschanged = false;
                int foundaLAAccount = 0;
                int accountcount = 0;
                
                //string[] stringvaluesaccount = new string[20];

                QueryExpression queryaccountExpression = BuildQueryExpression(lastfetchdate, "account");

                //log.InfoFormat("Pulling account records from CRM with a modified date greater than {0}...", lastfetchdate);
                EntityCollection accountresults = orgService.RetrieveMultipleAll(queryaccountExpression);

                log.InfoFormat("CRM accounts retrieved: {0}, inserting them into Database...", accountresults.Entities.Count);

                LogMessage(templatePath, "Retrieved, " + accountresults.Entities.Count + ", Account Records");


                if (accountresults.Entities.Count > 500)
                {
                    string subject = "Issue with CRM Account integration";
                    SendStandardEmail(accountresults.Entities.Count, contactsr, labsr, addressesr, instrumentsr, subject, foundaLAAccount);
                    return;
                }

                foreach (Entity accountent in accountresults.Entities)
                {
                    bool validBU = CheckValidBusinessUnit(accountent, orgService);
                    ////Satrt BU here
                    //Guid buid = new Guid();
                    //buid = ((EntityReference)accountent["owningbusinessunit"]).Id;

                    ////Start getting business unit here...
                    //QueryExpression buquery = new QueryExpression();
                    //buquery.EntityName = "businessunit";
                    //buquery.ColumnSet = new ColumnSet(true);

                    //buquery.Criteria.AddCondition("businessunit", "businessunitid", ConditionOperator.Equal, buid);
                    //EntityCollection buresult = orgService.RetrieveMultipleAll(buquery);

                    //if (buresult.Entities.Count == 1)
                    //{
                    //    foreach (Entity buresults in buresult.Entities)
                    //    {
                    //        var buname = buresults.GetAttributeValue<string>("name");
                    //        if (buname != "United States")
                    //        {
                    //            foundaLAAccount = true;
                    //            continue;
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    string subject = "Issue with CRM Business Unit Accounts";
                    //    SendStandardEmail(0, 0, 0, 0, buresult.Entities.Count, subject, 1);
                    //    foundaLAAccount = true;
                    //}

                    //if (foundaLAAccount)
                    //{
                    //    continue;
                    //}
                    ////End getting bu here

                    if (validBU)
                    {
                        accountsr++;
                        var pivid = accountent.GetAttributeValue<string>("smx_migrationsourceid");
                        if (pivid != null)
                        {
                            pivotalaccountid = accountent["smx_migrationsourceid"].ToString();
                        }
                        else
                        {
                            pivotalaccountid = "0x0000000000000000";
                        }

                        completedaccounts = SetupAccountFields(accountent, sqlConn, pivotalaccountid, daab, orgService);

                        accountcount++;
                    }
                    else
                    {
                        string checkbu = "";
                    }
                }

                //End account Read Write 
                #endregion                

                
                #region Contact   
                ////Start Contact Read Write, Query Contact modified added after maxRunDate

                //delete after all auto is on ##
                //lastfetchdate = Convert.ToDateTime("2017-03-13 16:48:00.000");

                string pivotalcontactid = "";
                //string[] stringvaluesContact = new string[20];

                QueryExpression queryContactExpression = BuildQueryExpression(lastfetchdate, "contact");

                //log.InfoFormat("Pulling Contact records from CRM with a modified date greater than {0}...", lastfetchdate);
                EntityCollection contactresults = orgService.RetrieveMultipleAll(queryContactExpression);
                //log.InfoFormat("CRM Contacts retrieved: {0}, inserting them into Database...", contactresults.Entities.Count);

                LogMessage(templatePath, "Retrieved, " + contactresults.Entities.Count + ",Contact Records");

                if (contactresults.Entities.Count > 500)
                {
                    string subject = "Issue with CRM Contact integration";
                    SendStandardEmail(accountsr, contactresults.Entities.Count, labsr, addressesr, instrumentsr, subject, foundaLAAccount);
                    //return;
                }

                foreach (Entity contct in contactresults.Entities)
                {
                    bool validBU = CheckValidBusinessUnit(contct, orgService);

                    if (validBU)
                    {
                        contactsr++;
                        string ln = contct.GetAttributeValue<string>("lastname");
                        string fn = contct.GetAttributeValue<string>("firstname");
                        var pivid = contct.GetAttributeValue<string>("smx_migrationsourceid");
                        if (pivid != null)
                        {
                            pivotalcontactid = contct["smx_migrationsourceid"].ToString();
                        }
                        else
                        {
                            pivotalcontactid = "0x0000000000000000";
                        }

                        bool completedContacts = SetupContactFields(orgService, contct, sqlConn, pivotalcontactid, daab);
                    }
                }

                //End Contact Read Write 
                #endregion

                
                #region Lab 
                //Start Lab Read Write, Query Labs modified added after maxRunDate
                string pivotallabid = "";
                bool labadresschanged = false;

                //to be deleted after auto is set ##
                //lastfetchdate = Convert.ToDateTime("2017-03-13 16:48:00.000");

                QueryExpression queryLabsExpression = BuildQueryExpression(lastfetchdate, "smx_lab");

                log.InfoFormat("Pulling Lab records from CRM with a modified date greater than {0}...", lastfetchdate);
                EntityCollection labresults = orgService.RetrieveMultipleAll(queryLabsExpression);
                log.InfoFormat("CRM Labs retrieved: {0}, inserting them into Database...", labresults.Entities.Count);

                LogMessage(templatePath, "Retrieved, " + labresults.Entities.Count + ", Lab Records");

                if (labresults.Entities.Count > 1000)
                {
                    string subject = "Issue with CRM Lab integration";
                    SendStandardEmail(accountsr, contactsr, labresults.Entities.Count, addressesr, instrumentsr, subject, foundaLAAccount);
                    //return;
                }

                foreach (Entity lab in labresults.Entities)
                {
                    bool validBU = CheckValidBusinessUnit(lab, orgService);

                    if (validBU)
                    {
                        labsr++;
                        var pivid = lab.GetAttributeValue<string>("smx_migrationsourceid");
                        if (pivid != null)
                        {
                            pivotallabid = lab["smx_migrationsourceid"].ToString();
                        }
                        else
                        {
                            pivotallabid = "0x0000000000000000";
                        }

                        //Check if change and setup in ctinstruments table
                        string chksqlCommand = "SELECT * FROM ctlab where ctlab_id = " + pivotallabid;
                        DataTable instrdt = GetDataSQL(chksqlCommand, sqlConn);

                        // Read data and create account and sales invoice records
                        if (instrdt.Rows.Count > 0)
                        {
                            SqlCommand lasc = new SqlCommand("update ctlab set inactive_reason = '" + pivotallabid.ToString() + "' where ctlab_id = " + pivotallabid, sqlConn);
                            lasc.ExecuteNonQuery();
                        }
                        else
                        {
                            LogMessage("", "Error - Could not find ctlabid: " + pivotallabid);
                        }
                        //END set instrumentid compare

                        bool completedLabs = SetupLabFields(lab, sqlConn, pivotallabid, daab, orgService);
                    }
                }

                //End Lab Read Write 
                #endregion
                

                #region Address
                //START ACCOUNT then ACCOUNT ADDRESS and then LAB and then LAB ADDRESS
                //

                //lastfetchdate = Convert.ToDateTime("2017-03-13 16:48:00.000");

                string pivotaladrid = "";
                string addrestousesid = "";
                var adrtouseid = "";
                //string pivotallaadrbid = "";

                QueryExpression queryAddressExpression = BuildQueryExpression(lastfetchdate, "smx_address");
                EntityCollection addressresults = orgService.RetrieveMultipleAll(queryAddressExpression);

                string addresspausehere = "";

                if (addressresults.Entities.Count > 500)
                {
                    string subject = "Issue with CRM Address integration";
                    SendStandardEmail(accountsr, contactsr, labsr, addressresults.Entities.Count, instrumentsr, subject, foundaLAAccount);
                    //return;
                }

                LogMessage(templatePath, "Retrieved, " + addressresults.Entities.Count + ",Address Records");

                foreach (Entity addresse in addressresults.Entities)
                {
                    //bool validBU = CheckValidBusinessUnit(addresse, orgService);
                    string country = "";

                    if (addresse.GetAttributeValue<OptionSetValue>("smx_country") != null)
                    {
                        country = addresse.FormattedValues["smx_country"].ToString();
                    }


                    //if (country == "US")
                    //{
                        addressesr++;
                        string pivcompanyaddressid = "";
                        string typeofaddess = "";
                        Guid _idtouse = Guid.Empty;
                        string entitytext = "";
                        string idtext = "";

                        if (addresse.GetAttributeValue<OptionSetValue>("smx_type") != null)
                        {
                            typeofaddess = addresse.FormattedValues["smx_type"].ToString();
                        }

                        if (addresse.Attributes.Contains("smx_lab"))
                        {
                            _idtouse = ((EntityReference)addresse["smx_lab"]).Id;
                            entitytext = "smx_lab";
                            idtext = "smx_labid";
                            typeofaddess = "Lab";
                        }
                        else
                        {
                            if (typeofaddess != "")
                            {
                                try
                                {
                                    _idtouse = ((EntityReference)addresse["smx_account"]).Id;
                                }
                                catch (Exception exc)
                                {
                                    continue;
                                }
                                entitytext = "account";
                                idtext = "accountid";
                                typeofaddess = "Account";
                            }
                            else
                            {
                                continue;
                            }
                        }

                        QueryExpression queryaccountsingleExp = new QueryExpression(entitytext);
                        queryaccountsingleExp.ColumnSet = new ColumnSet(true);
                        queryaccountsingleExp.Criteria.Conditions.Add(new ConditionExpression(idtext, ConditionOperator.Equal, _idtouse));

                        EntityCollection accountsinglegetresults = orgService.RetrieveMultipleAll(queryaccountsingleExp);

                        foreach (Entity addressgetacc in accountsinglegetresults.Entities)
                        {
                            addrestousesid = addressgetacc.GetAttributeValue<string>("smx_migrationsourceid");
                            adrtouseid = addressgetacc.GetAttributeValue<string>("smx_migrationsourceid");
                        }

                        if (adrtouseid != null)
                        {
                            //pivotaladrid = addresse["smx_migrationsourceid"].ToString();
                            pivotaladrid = adrtouseid.ToString();
                        }
                        else
                        {
                            pivotaladrid = "0x0000000000000000";
                        }

                        bool completedContacts = SetupAddressFields(addresse, sqlConn, pivotaladrid, typeofaddess, daab, orgService);
                    //}   
                }

                //End Address Read Write 
                #endregion
                

                
                #region Instruments 
                //Start Instruments Read Write, Query Labs modified added after maxRunDate

                string pivotalinstrumentid = "";
                //string[] stringvaluesInstruments = new string[20];

                //lastfetchdate = Convert.ToDateTime("2017-03-13 16:48:00.000");

                QueryExpression queryInstrumentsExpression = BuildQueryExpression(lastfetchdate, "smx_instrument");

                log.InfoFormat("Pulling Insturument records from CRM with a modified date greater than {0}...", lastfetchdate);
                EntityCollection instrumentresults = orgService.RetrieveMultipleAll(queryInstrumentsExpression);
                log.InfoFormat("CRM Insturuments retrieved: {0}, inserting them into Database...", instrumentresults.Entities.Count);

                LogMessage(templatePath, "Retrieved," + instrumentresults.Entities.Count + ",Insturument Records");

                if (instrumentresults.Entities.Count > 1000)
                {
                    string subject = "Issue with CRM Instrument integration";
                    SendStandardEmail(accountsr, contactsr, labsr, addressesr, instrumentresults.Entities.Count, subject, foundaLAAccount);
                    //return;
                }

                foreach (Entity instrument in instrumentresults.Entities)
                {
                    bool validBU = CheckValidBusinessUnit(instrument, orgService);

                    if (validBU)
                    {
                        instrumentsr++;
                        var pivid = instrument.GetAttributeValue<string>("smx_migrationsourceid");
                        if (pivid != null)
                        {
                            pivotalinstrumentid = instrument["smx_migrationsourceid"].ToString();
                        }
                        else
                        {
                            pivotalinstrumentid = "0x0000000000000000";
                        }

                        //Check if change and setup in ctinstruments table
                        string chksqlCommand = "SELECT * FROM ctplacement where ctplacement_id = " + pivotalinstrumentid;
                        DataTable instrdt = GetDataSQL(chksqlCommand, sqlConn);

                        // Read data and create account and sales invoice records
                        if (instrdt.Rows.Count > 0)
                        {
                            SqlCommand iasc = new SqlCommand("update ctplacement set crm_new_id = " + pivotalinstrumentid + " where ctplacement_id = " + pivotalinstrumentid, sqlConn);
                            iasc.ExecuteNonQuery();
                        }
                        else
                        {
                            LogMessage("", "Error - Could not find cplacementid: " + pivotalinstrumentid);
                        }
                        //END set instrumentid compare

                        bool completedInsturuments = SetupInstrumentFields(instrument, sqlConn, pivotalinstrumentid, daab, orgService);
                    }
                }

                //Bring ctinstruments into alignemnt for the changes made form CRM
                SqlCommand ctiasc = new SqlCommand("update ctinstruments set work_filter = 1 where ctplacement_id in (select ctplacement_id from ctplacement where crm_new_id is not null)", sqlConn);
                ctiasc.ExecuteNonQuery();                
                //End alignment to ctinstruments


                //End Instrument Read Write 

                // The end function function will just do a Console.ReadLine() so that we display the program output without immediately closing the console.
                // (It also exits the program if called elsewhere, useful for ending on errors).

                //End Instruments Read Write 
                #endregion   
             
                SqlCommand asc = new SqlCommand("update system set last_crm_update_date = '" + DateTime.Now + "'", sqlConn);
                asc.ExecuteNonQuery();


                log.Info("CRM Contacts into Database operations have completed.");


                string subjectstr = "CRM Integration completed successfully";
                SendStandardEmail(accountsr, contactsr, labsr, addressesr, instrumentsr, subjectstr, foundaLAAccount);

                sqlConn.Close();
                sqlConn.Dispose();
            }
            catch (Exception exc)
            {
                sqlConn.Close();
                sqlConn.Dispose();

                //log message here  exc
            }

            //Where are the client crm close/dispose commands?##
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
        /// Execute a WhoAmIRequest against the organization.
        /// </summary>
        /// <param name="orgService">A CrmServiceClient that's connected to an organization.</param>
        private static void SendStandardEmail(int accountsr, int contactsr, int labsr, int addressesr, int instrumentsr, string subject, int foundaLAAccount)
        {
            string to = "martinh@sysmex.com";
            string bccemail = "";

            int accountcount = accountsr + contactsr + labsr + addressesr + instrumentsr;

            DateTime dtime = DateTime.Now;

            int hrmin = dtime.Hour + dtime.Minute;

            //if (accountcount > 30 || dtime.Hour == 8 || dtime.Hour == 12 || dtime.Hour == 16 || dtime.Hour == 20)
            //if (accountcount > 30 || hrmin == 8 || hrmin == 12 || hrmin == 16 || hrmin == 20)
            if (accountsr > 0 || contactsr > 0 || labsr > 0 || addressesr > 0 || instrumentsr > 0)
            {
                //subject = "CRM - Integration completed succesfully. (" + accountcount + ")";
                subject = subject + ". (" + accountcount + ")";
                string body = "Thank You for using CRM interface, your updates have been received for " + DateTime.Now;
                body += Environment.NewLine;
                body += Environment.NewLine;
                body = body + "Accounts: " + accountsr;
                body += Environment.NewLine;
                body = body + "Contacts: " + contactsr;
                body += Environment.NewLine;
                body = body + "Labs: " + labsr;
                body += Environment.NewLine;
                body = body + "Instruments: " + instrumentsr;
                body += Environment.NewLine;
                body = body + "Addresses: " + addressesr;
                body += Environment.NewLine;
                body = body + "LA Accounts read: " + foundaLAAccount;
                body += Environment.NewLine;
                body = body + "CRM Team";
                body += Environment.NewLine;

                bool finshemailsend = SendEmail(to, subject, body, bccemail);
            }
        }

        public static bool CheckValidBusinessUnit(Entity accountent, IOrganizationService orgService)
        {
            //Satrt BU here
            Guid buid = new Guid();
            buid = ((EntityReference)accountent["owningbusinessunit"]).Id;

            //Start getting business unit here...
            QueryExpression buquery = new QueryExpression();
            buquery.EntityName = "businessunit";
            buquery.ColumnSet = new ColumnSet(true);

            buquery.Criteria.AddCondition("businessunit", "businessunitid", ConditionOperator.Equal, buid);
            EntityCollection buresult = orgService.RetrieveMultipleAll(buquery);

            if (buresult.Entities.Count == 1)
            {
                foreach (Entity buresults in buresult.Entities)
                {
                    var buname = buresults.GetAttributeValue<string>("name");
                    if (buname != "United States")
                    {
                        return false;
                    }
                }
            }
            else
            {
                string subject = "Issue with CRM Business Unit Accounts";
                SendStandardEmail(0, 0, 0, 0, buresult.Entities.Count, subject, 1);
                return false;
            }

            return true;
        }

        ///// <summary>
        ///// Execute a WhoAmIRequest against the organization.
        ///// </summary>
        ///// <param name="orgService">A CrmServiceClient that's connected to an organization.</param>
        //private static void CompareandUpdateContact(IOrganizationService orgService)
        //{
        //    var whoAmIRequest = new WhoAmIRequest();
        //    var whoAmIResponse = (WhoAmIResponse)orgService.Execute(whoAmIRequest);

        //    // Simply printing out the Ids for the org/user/business unit that's connected
        //    Console.WriteLine("You are connected to organization: {0}", whoAmIResponse.OrganizationId);
        //    Console.WriteLine("You are logged in as user: {0}", whoAmIResponse.UserId);
        //    Console.WriteLine("Your business unit is: {0}", whoAmIResponse.BusinessUnitId);
        //}

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
                    //appSettings.IsDefaultSettings = false; // since we're loading the settings dynamically, we are not using defaults

                    return appSettings;
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return null;
            }
        }

        ///// <summary>
        ///// SetupContactFields get the fields from contact and crm and compare and then create or update record
        ///// </summary>
        ///// <param> the entity from crm, contact id from pivotal and field values</param>
        private static bool SetupAccountFields(Entity account, SqlConnection sqlConn, string pivotalaccountid, DAAB daab, IOrganizationService orgService)
        {
            try
            {
                string[] stringvalues = new string[10];

                string sqlCommand = "SELECT * FROM company where company_id = " + pivotalaccountid;
                DataTable accountsdt = GetDataSQL(sqlCommand, sqlConn);

                // Read data and create account and sales invoice records
                if (accountsdt.Rows.Count > 0)
                {
                    if (accountsdt.Rows.Count == 1)
                    {
                        //ID get guids from crm
                        Guid _altmanager = new Guid();
                        Guid _gpo = new Guid();
                        Guid _ihn = new Guid();
                        Guid _ihnsec = new Guid();
                        Guid _pardacc = new Guid();

                        string gpoid = "";
                        string ihnid = "";
                        string ihnsecid = "";
                        string pardid = "";

                        if (account.GetAttributeValue<string>("name") != null)
                        {
                            stringvalues[0] = account["name"].ToString().Trim();
                        }

                        //if (((EntityReference)account["smx_altterritorymanager"]).Id != null)   ////##  this does not work if null
                        if (account.Attributes.Contains("smx_altterritorymanager"))
                        {
                            _altmanager = ((EntityReference)account["smx_altterritorymanager"]).Id;
                            Entity accounte = GetCRMId(_altmanager, "systemuser", "systemuserid", orgService);  //find the table here for employee
                            if (accounte == null)
                            {
                                return false;
                            }
                            string mdsstradusername = accounte.GetAttributeValue<string>("domainname");  //ad username here
                            string[] adname = mdsstradusername.Split('@');
                            if (adname[0] != "")
                            {
                                //var tispivid = accounte.GetAttributeValue<string>("ADUSERNAME");  //ad username here
                                sqlCommand = "SELECT a.employee_id FROM employee a, users b where a.rn_employee_user_id = b.users_id and login_name = '" + adname[0].Trim() + "'";
                                DataTable userdt = GetDataSQL(sqlCommand, sqlConn);

                                // Read data and create account and sales invoice records
                                if (userdt.Rows.Count > 0)
                                {
                                    if (userdt.Rows.Count == 1)
                                    {
                                        stringvalues[1] = Id.Create(userdt.Rows[0]["Employee_Id"]).ToString();
                                    }
                                    else
                                    {
                                        throw new Exception("issue on smx_altterritorymanager user record");
                                    }
                                }
                                else
                                {
                                    throw new Exception("smx_altterritorymanager issue");
                                }
                            }
                        }

                        //Partner account
                        if (account.Attributes.Contains("smx_partneraccount") && account.GetAttributeValue<EntityReference>("smx_partneraccount") != null)

                        //if (account.GetAttributeValue<Id>("smx_partneraccount") != null)  //##use this for checking id's
                        {
                            _pardacc = ((EntityReference)account["smx_partneraccount"]).Id;
                            Entity accounte = GetCRMId(_pardacc, "account", "accountid", orgService);  //find the table here for employee
                            if (accounte == null)
                            {
                                return false;
                            }
                            pardid = accounte.GetAttributeValue<string>("smx_migrationsourceid");   //string or var
                            stringvalues[2] = pardid;
                        }
                        else
                        {
                            stringvalues[2] = Id.Create("0x0000000000000001").ToString();
                        }

                        if (account.Attributes.Contains("smx_gpo"))
                        {
                            _gpo = ((EntityReference)account["smx_gpo"]).Id;
                            Entity accounte = GetCRMId(_gpo, "smx_gpo", "smx_gpoid", orgService);  //find the table here for employee
                            if (accounte == null)
                            {
                                return false;
                            }
                            gpoid = accounte.GetAttributeValue<string>("smx_migrationsourceid");   //string or var
                            stringvalues[3] = gpoid;
                        }
                        else
                        {
                            stringvalues[3] = "0x0000670000000001";
                        }

                        if (account.Attributes.Contains("smx_ihn"))
                        {
                            _ihn = ((EntityReference)account["smx_ihn"]).Id;
                            Entity accounte = GetCRMId(_ihn, "smx_ihn", "smx_ihnid", orgService);  //find the table here for employee
                            if (accounte == null)
                            {
                                return false;
                            }
                            ihnid = accounte.GetAttributeValue<string>("smx_migrationsourceid");   //string or var
                            stringvalues[4] = ihnid;
                        }
                        else
                        {
                            stringvalues[4] = "0x0000670000000001";
                        }

                        if (account.Attributes.Contains("smx_ihnsecondary"))
                        {
                            _ihnsec = ((EntityReference)account["smx_ihnsecondary"]).Id;
                            Entity accounte = GetCRMId(_ihnsec, "smx_ihn", "smx_ihnid", orgService);  //find the table here for employee

                            if (accounte == null)
                            {
                                return false;
                            }
                            ihnsecid = accounte.GetAttributeValue<string>("smx_migrationsourceid");   //string or var
                            stringvalues[5] = ihnsecid;
                        }
                        else
                        {
                            stringvalues[5] = "0x0000670000000001";
                        }

      
                        //DECIMALS, only taking 2 bedsize and cbc... others do not belong in pivotal
                        if (account.GetAttributeValue<decimal>("smx_BedSize") != null)
                        {
                            if (!account["smx_bedsize"].ToString().Trim().Equals(accountsdt.Rows[0]["cfBed_Size"].ToString())) stringvalues[6] = account["smx_bedsize"].ToString().Trim();
                        }
                        if (account.GetAttributeValue<decimal>("smx_NoofCBC") != null)
                        {
                            if (!account["smx_noofcbc"].ToString().Trim().Equals(accountsdt.Rows[0]["cfNo_of_CBC"].ToString())) stringvalues[8] = account["smx_noofcbc"].ToString().Trim();
                        }

                        //TEXT fields    
                        if (account.GetAttributeValue<string>("smx_phone") != null)
                        {
                            if (!account["smx_phone"].ToString().Trim().Equals(accountsdt.Rows[0]["Phone"].ToString())) stringvalues[7] = account["smx_phone"].ToString().Trim();
                        }


                        //OPTION SETS
                        //if (account.GetAttributeValue<OptionSetValue>("smx_language") != null)
                        //{
                        //    string laguage = account.FormattedValues["smx_language"].ToString();
                        //    if (!account["smx_language"].ToString().Trim().Equals(accountsdt.Rows[0]["cfLanguage"].ToString())) stringvalues[9] = laguage;
                        //}  


                        //BOOLS
                        //if (account.GetAttributeValue<bool>("smx_compass") != null)
                        //{
                        //    if (!account["smx_compass"].ToString().Trim().Equals(accountsdt.Rows[0]["cfCompass"].ToString())) stringvalues[11] = account["smx_compass"].ToString().Trim();
                        //}
                        //if (account.GetAttributeValue<bool>("smx_compass") != null)
                        //{
                        //    if (!account["smx_compass"].ToString().Trim().Equals(accountsdt.Rows[0]["cfCompass"].ToString())) stringvalues[11] = account["smx_compass"].ToString().Trim();
                        //}

                        CreateUpdateAccountRecord(pivotalaccountid, sqlConn, stringvalues, daab);
                    }
                }
   

                return true;
                
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        ///// <summary>
        ///// SetupContactFields get the fields from contact and crm and compare and then create or update record
        ///// </summary>
        ///// <param> the entity from crm, contact id from pivotal and field values</param>
        private static bool SetupContactFields(IOrganizationService orgService, Entity contct, SqlConnection sqlConn, string pivotalcontactid, DAAB daab)
        {
            try
            {
                string[] stringvalues = new string[29];
                string sqlCommand = "";

                sqlCommand = "SELECT * FROM contact where contact_id = " + pivotalcontactid;
                DataTable contactsdt = GetDataSQL(sqlCommand, sqlConn);

                // Read data and create account and sales invoice records
                if (contactsdt.Rows.Count > 0)
                {
                    if (contactsdt.Rows.Count == 1)
                    {         
                        //getid
                        Id contactidfrompivotal = Id.Create(contactsdt.Rows[0]["Contact_Id"]);
                        string fname = contactsdt.Rows[0]["First_Name"].ToString();
                        string lname = contactsdt.Rows[0]["Last_Name"].ToString();

                        var pivid = contct.GetAttributeValue<string>("smx_migrationsourceid");
                        if (pivid != null)
                        {
                            pivotalcontactid = contct["smx_migrationsourceid"].ToString();
                        }

                        if (contct.GetAttributeValue<string>("firstname") != null)
                        {
                            if (!contct["firstname"].ToString().Trim().Equals(contactsdt.Rows[0]["First_Name"].ToString())) stringvalues[0] = contct["firstname"].ToString().Trim();
                        }
                        if (contct.GetAttributeValue<string>("lastname") != null)
                        {
                            if (!contct["lastname"].ToString().Trim().Equals(contactsdt.Rows[0]["Last_Name"].ToString())) stringvalues[1] = contct["lastname"].ToString().Trim();
                        }
                        if (contct.GetAttributeValue<OptionSetValue>("smx_jobtitle") != null)
                        {
                            string jobtitle = contct.FormattedValues["smx_jobtitle"].ToString();
                            if (!contct["smx_jobtitle"].ToString().Trim().Equals(contactsdt.Rows[0]["Job_title"].ToString())) stringvalues[2] = jobtitle;
                        }
                        if (contct.GetAttributeValue<OptionSetValue>("smx_Prefix") != null)
                        {
                            string title = contct.FormattedValues["smx_prefix"].ToString();
                            if (!contct["smx_prefix"].ToString().Trim().Equals(contactsdt.Rows[0]["Title"].ToString())) stringvalues[3] = title;
                        }

                        if (contct.Attributes.Contains("smx_jobtitleother"))
                        {
                            stringvalues[4] = contct["smx_jobtitleother"].ToString().Trim();
                        }
                        //stringvalues[4] = contct["smx_jobtitleother"].ToString().Trim();

                        if (contct.GetAttributeValue<string>("address1_line1") != null)
                        {
                            if (!contct["address1_line1"].ToString().Trim().Equals(contactsdt.Rows[0]["Address_1"].ToString())) stringvalues[5] = contct["address1_line1"].ToString().Trim();
                        }
                        if (contct.GetAttributeValue<string>("address1_line2") != null)
                        {
                            if (!contct["address1_line2"].ToString().Trim().Equals(contactsdt.Rows[0]["Address_2"].ToString())) stringvalues[6] = contct["address1_line2"].ToString().Trim();
                        }
                        if (contct.GetAttributeValue<string>("address1_city") != null)
                        {
                            if (!contct["address1_city"].ToString().Trim().Equals(contactsdt.Rows[0]["City"].ToString())) stringvalues[7] = contct["address1_city"].ToString().Trim();
                        }
                        if (contct.GetAttributeValue<string>("address1_stateorprovince") != null)
                        {
                            if (!contct["address1_stateorprovince"].ToString().Trim().Equals(contactsdt.Rows[0]["State_"].ToString())) stringvalues[8] = contct["address1_stateorprovince"].ToString().Trim();
                            string state_ = contct["address1_stateorprovince"].ToString().Trim();
                            if (state_.Length > 2)
                            {
                                stringvalues[8] = "TBD";
                            }
                        }
                        if (contct.GetAttributeValue<string>("address1_postalcode") != null)
                        {
                            if (!contct["address1_postalcode"].ToString().Trim().Equals(contactsdt.Rows[0]["Zip"].ToString())) stringvalues[9] = contct["address1_postalcode"].ToString().Trim();
                        }
                        if (contct.GetAttributeValue<string>("telephone1") != null)
                        {
                            if (!contct["telephone1"].ToString().Trim().Equals(contactsdt.Rows[0]["Phone"].ToString())) stringvalues[10] = contct["telephone1"].ToString().Trim(); //#
                        }

                        if (contct.GetAttributeValue<string>("Extension") != null)
                        {
                            stringvalues[11] = contct["smx_extension"].ToString().Trim();
                        }

                        //stringvalues[11] = contct["smx_extension"].ToString().Trim(); //#
                        
                        if (contct.GetAttributeValue<string>("mobilephone") != null)
                        {
                            if (!contct["mobilephone"].ToString().Trim().Equals(contactsdt.Rows[0]["Cell"].ToString())) stringvalues[12] = contct["mobilephone"].ToString().Trim();
                        }
                        if (contct.GetAttributeValue<string>("emailaddress1") != null)
                        {
                            if (!contct["emailaddress1"].ToString().Trim().Equals(contactsdt.Rows[0]["Email"].ToString())) stringvalues[13] = contct["emailaddress1"].ToString().Trim();
                        }

                        //role switches
                        //if (contct.GetAttributeValue<bool>("smx_flowcyfc"))
                        if (contct.Attributes.Contains("smx_flowcyfc"))
                        {
                            if (!contct["smx_flowcyfc"].ToString().Trim().Equals(contactsdt.Rows[0]["cfODIS_FC"].ToString())) stringvalues[14] = contct["smx_flowcyfc"].ToString().Trim();
                        }
                        //if (contct.GetAttributeValue<bool>("smx_liscoordinator"))
                        if (contct.Attributes.Contains("smx_liscoordinator"))
                        {
                            if (!contct["smx_liscoordinator"].ToString().Trim().Equals(contactsdt.Rows[0]["cfODIS_LIS"].ToString())) stringvalues[15] = contct["smx_liscoordinator"].ToString().Trim();
                        }
                        if (contct.Attributes.Contains("smx_purchasingcontact"))
                        //if (contct.GetAttributeValue<bool>("smx_purchasingcontact"))
                        {
                            if (!contct["smx_purchasingcontact"].ToString().Trim().Equals(contactsdt.Rows[0]["cfODIS_PCN"].ToString())) stringvalues[16] = contct["smx_purchasingcontact"].ToString().Trim();
                        }
                        if (contct.Attributes.Contains("smx_wsar"))
                        //if (contct.GetAttributeValue<bool>("smx_wsar"))
                        {
                            if (!contct["smx_wsar"].ToString().Trim().Equals(contactsdt.Rows[0]["cfODIS_WSAR"].ToString())) stringvalues[17] = contct["smx_wsar"].ToString().Trim();
                        }
                        if (contct.Attributes.Contains("smx_trainingcontact"))
                        //if (contct.GetAttributeValue<bool>("smx_trainingcontact"))
                        {
                            if (!contct["smx_trainingcontact"].ToString().Trim().Equals(contactsdt.Rows[0]["cfODIS_TCN"].ToString())) stringvalues[18] = contct["smx_trainingcontact"].ToString().Trim();
                        }

                        if (contct.Attributes.Contains("smx_maincontact"))
                        {
                            if (!contct["smx_maincontact"].ToString().Trim().Equals(contactsdt.Rows[0]["cfODIS_MC1"].ToString())) stringvalues[19] = contct["smx_maincontact"].ToString().Trim();
                        }
                        if (contct.Attributes.Contains("smx_serviceagreementrecipient"))
                        //if (contct.GetAttributeValue<bool>("smx_serviceagreementrecipient"))
                        {
                            if (!contct["smx_serviceagreementrecipient"].ToString().Trim().Equals(contactsdt.Rows[0]["cfODIS_SAR"].ToString())) stringvalues[20] = contct["smx_serviceagreementrecipient"].ToString().Trim();
                        }
                        if (contct.Attributes.Contains("smx_cprcontact"))
                        //if (contct.GetAttributeValue<bool>("smx_cprcontact"))
                        {
                            if (!contct["smx_cprcontact"].ToString().Trim().Equals(contactsdt.Rows[0]["cfODIS_CPR"].ToString())) stringvalues[21] = contct["smx_cprcontact"].ToString().Trim();
                        }

                        if (contct.Attributes.Contains("smx_sysmexnewsletter"))
                        {
                            if (!contct["smx_sysmexnewsletter"].ToString().Trim().Equals(contactsdt.Rows[0]["cfSysmex_Newsletter"].ToString())) stringvalues[22] = contct["smx_sysmexnewsletter"].ToString().Trim();
                        }
                        if (contct.Attributes.Contains("smx_sysmexjournal"))
                        {
                            if (!contct["smx_sysmexjournal"].ToString().Trim().Equals(contactsdt.Rows[0]["cfSysmex_Journal"].ToString())) stringvalues[23] = contct["smx_sysmexjournal"].ToString().Trim();
                        }

                        if (contct.GetAttributeValue<string>("description") != null)
                        {
                            if (!contct["description"].ToString().Trim().Equals(contactsdt.Rows[0]["Comments"].ToString())) stringvalues[24] = contct["description"].ToString().Trim();
                        }

                        string contactdelete = contct.FormattedValues["statecode"];

                        if (contactdelete.ToUpper() == "INACTIVE")
                        {
                            SqlCommand asc = new SqlCommand("delete from contact where contact_id = " + contactidfrompivotal, sqlConn);
                            asc.ExecuteNonQuery();

                            //bool returnsendemail = SendEmail("akers@sysmex.com", "Check Contact detail: ",fname + " " + lname + ". Check Contact is inactive in CRM and Deleted in Pivotal" + stringvalues[0] + " " + stringvalues[1], "martinh@sysmex.com");

                            return true;
                        }

                        Id pivcontactid = CreateUpdateContactRecord(pivotalcontactid, sqlConn, stringvalues, daab);
                    }
                }
                else
                {
                    //Create new contact here...
                    //someValue = condition ? newValue : someValue;
                    pivotalcontactid = null;
                    string territoryid = "";
                    string accountmanagerid = "";
                    string companyid = "";
                    string type = "";

                    Guid _accountid = ((EntityReference)contct["parentcustomerid"]).Id;
                    Guid crmid  = contct.Id;
                    Entity accounte = GetCRMId(_accountid, "account", "accountid", orgService);

                    if (accounte == null)
                    {
                        return false;
                    }

                    var companypivid = accounte.GetAttributeValue<string>("smx_migrationsourceid");
                    if (companypivid != null)
                    {
                        //companypivid = contct["smx_migrationsourceid"].ToString();
                        companyid = companypivid.ToString();

                        string sqlCompany = "SELECT * FROM company where company_id = " + companypivid;
                        DataTable companydt = GetDataSQL(sqlCompany, sqlConn);

                        // Read data and create account and sales invoice records
                        if (companydt.Rows.Count == 1)
                        {
                            Id terrid = Id.Create(companydt.Rows[0]["Territory_Id"]);
                            territoryid = terrid.ToString();
                            Id accmanid = Id.Create(companydt.Rows[0]["Account_Manager_Id"]);
                            accountmanagerid = accmanid.ToString();
                            type = companydt.Rows[0]["Type"].ToString();
                        }
                        else
                        {
                            LogMessage("","Error on reading companyid");
                        }
                    }                    

                    if (contct.GetAttributeValue<string>("firstname") != null)
                    {
                        stringvalues[0] = contct["firstname"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<string>("lastname") != null)
                    {
                        stringvalues[1] = contct["lastname"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<OptionSetValue>("smx_jobtitle") != null)
                    {
                        string jobtitle = contct.FormattedValues["smx_jobtitle"].ToString();
                        stringvalues[2] = jobtitle;
                    }
                    if (contct.GetAttributeValue<OptionSetValue>("smx_prefix") != null)
                    {
                        string prefix = contct.FormattedValues["smx_prefix"].ToString();
                        stringvalues[3] = prefix;
                    }
                    
                    if (contct.Attributes.Contains("smx_jobtitleother"))
                    {
                        stringvalues[4] = contct["smx_jobtitleother"].ToString().Trim();
                    }
                    //stringvalues[4] = contct["smx_jobtitleother"].ToString().Trim();
                    
                    if (contct.GetAttributeValue<string>("address1_line1") != null)
                    {
                        stringvalues[5] = contct["address1_line1"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<string>("address1_line2") != null)
                    {
                        stringvalues[6] = contct["address1_line2"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<string>("address1_city") != null)
                    {
                        stringvalues[7] = contct["address1_city"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<string>("address1_stateorprovince") != null)
                    {
                        stringvalues[8] = contct["address1_stateorprovince"].ToString().Trim();
                        string state_ = contct["address1_stateorprovince"].ToString().Trim();
                        if (state_.Length > 2)
                        {
                            stringvalues[8] = "TBD";
                        }
                    }
                    if (contct.GetAttributeValue<string>("address1_postalcode") != null)
                    {
                        stringvalues[9] = contct["address1_postalcode"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<string>("telephone1") != null)
                    {
                        stringvalues[10] = contct["telephone1"].ToString().Trim();
                    }

                    //stringvalues[11] = contct["smx_Extension"].ToString().Trim();  //##
                    
                    if (contct.GetAttributeValue<string>("mobilephone") != null)
                    {
                        stringvalues[12] = contct["mobilephone"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<string>("emailaddress1") != null)
                    {
                        stringvalues[13] = contct["emailaddress1"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<string>("Extension") != null)  //##
                    {
                        stringvalues[11] = contct["smx_extension"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<bool>("smx_flowcyfc"))
                    {
                        stringvalues[14] = contct["smx_flowcyfc"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<bool>("smx_liscoordinator"))
                    {
                        stringvalues[15] = contct["smx_liscoordinator"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<bool>("smx_purchasingcontact"))
                    {
                        stringvalues[16] = contct["smx_purchasingcontact"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<bool>("smx_wsar"))
                    {
                        stringvalues[17] = contct["smx_wsar"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<bool>("smx_trainingcontact"))
                    {
                        stringvalues[18] = contct["smx_trainingcontact"].ToString().Trim();
                    }

                    if (contct.GetAttributeValue<bool>("smx_MainContact"))
                    {
                        stringvalues[19] = contct["smx_maincontact"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<bool>("smx_serviceagreementrecipient"))
                    {
                        stringvalues[20] = contct["smx_serviceagreementrecipient"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<bool>("smx_cprcontact"))
                    {
                        stringvalues[21] = contct["smx_cprcontact"].ToString().Trim();
                    }

                    if (contct.GetAttributeValue<bool>("smx_sysmexnewsletter"))
                    {
                        stringvalues[22] = contct["smx_sysmexnewsletter"].ToString().Trim();
                    }
                    if (contct.GetAttributeValue<bool>("smx_sysmexjournal"))
                    {
                        stringvalues[23] = contct["smx_sysmexjournal"].ToString().Trim();
                    }

                    if (contct.GetAttributeValue<string>("description") != null)
                    {
                        stringvalues[24] = contct["description"].ToString().Trim();
                    }

                    stringvalues[25] = companyid;
                    stringvalues[26] = accountmanagerid;
                    stringvalues[27] = territoryid;
                    stringvalues[28] = type;

                    Id pivcontactid = CreateUpdateContactRecord(pivotalcontactid, sqlConn, stringvalues, daab);
                    string MSID = pivcontactid.ToString();

                    bool success = WriteRecordToCRM(crmid, "contact", "contactid", MSID, orgService);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        ///// <summary>
        ///// SetupContactFields get the fields from contact and crm and compare and then create or update record
        ///// </summary>
        ///// <param> the entity from crm, contact id from pivotal and field values</param>
        private static bool SetupAddressFields(Entity eaddress, SqlConnection sqlConn, string pivotalid, string type, DAAB daab, IOrganizationService orgService)
        {
            try
            {
                string[] stringvalues = new string[7];
                DataTable companydt = null;
                DataTable labdt = null;
                bool runaccount = false;
                bool runlab = false;

                Guid _accountid = new Guid();
                Guid _labid = new Guid();
                Guid crmid = new Guid();

                if (type == "Account")
                {
                    if (pivotalid != "" || pivotalid != "0x0000000000000000")
                    {
                        string sqlCommand = "SELECT * FROM company where company_id = " + pivotalid;
                        companydt = GetDataSQL(sqlCommand, sqlConn);
                        runaccount = true;

                        //##
                        //_accountid = ((EntityReference)eaddress["parentcustomerid"]).Id;
                        _accountid = ((EntityReference)eaddress["smx_account"]).Id;
                        crmid = eaddress.Id;
                        Entity accountf = GetCRMId(crmid, "account", "accountid", orgService);
                        Entity accounte = GetCRMId(_accountid, "account", "accountid", orgService);

                        bool validBU = CheckValidBusinessUnit(accounte, orgService);
                        if (!validBU)
                        {
                            return false;
                        }
                    }
                }

                if (type == "Lab")
                {
                    if (pivotalid != "" || pivotalid == "0x0000000000000000")
                    {
                        string sqlCommand = "SELECT * FROM ctLab where ctlab_id = " + pivotalid;
                        labdt = GetDataSQL(sqlCommand, sqlConn);
                        runlab = true;

                        _labid = ((EntityReference)eaddress["smx_lab"]).Id;
                        crmid = eaddress.Id;
                        Entity accounte = GetCRMId(crmid, "smx_lab", "smx_labid", orgService);
                    }
                }

                // Read data and create account adress records
                if (runaccount)
                {
                    if (companydt.Rows.Count > 0)
                    {
                        if (companydt.Rows.Count == 1)
                        {
                            if (eaddress.GetAttributeValue<OptionSetValue>("smx_country") != null)
                            {
                                string cntry = eaddress.FormattedValues["smx_country"].ToString();
                                if (!eaddress["smx_country"].ToString().Trim().Equals(companydt.Rows[0]["Country"].ToString())) stringvalues[0] = cntry;
                            }

                            if (eaddress.GetAttributeValue<string>("smx_addressstreet1") != null)
                            {
                                if (!eaddress["smx_addressstreet1"].ToString().Trim().Equals(companydt.Rows[0]["Address_1"].ToString())) stringvalues[1] = eaddress["smx_addressstreet1"].ToString().Trim();
                            }
                            if (eaddress.GetAttributeValue<string>("smx_addressstreet2") != null)
                            {
                                if (!eaddress["smx_addressstreet2"].ToString().Trim().Equals(companydt.Rows[0]["Address_2"].ToString())) stringvalues[2] = eaddress["smx_addressstreet2"].ToString().Trim();
                            }
                            if (eaddress.GetAttributeValue<string>("smx_addressstreet3") != null)
                            {
                                if (!eaddress["smx_addressstreet3"].ToString().Trim().Equals(companydt.Rows[0]["Address_3"].ToString())) stringvalues[3] = eaddress["smx_addressstreet3"].ToString().Trim();
                            }
                            if (eaddress.GetAttributeValue<string>("smx_city") != null)
                            {
                                stringvalues[4] = eaddress["smx_city"].ToString().Trim();
                            }

                            if (eaddress.GetAttributeValue<string>("smx_zippostalcode") != null)
                            {
                                stringvalues[6] = eaddress["smx_zippostalcode"].ToString().Trim();
                            }

                            //if (eaddress.GetAttributeValue<OptionSetValue>("smx_stateprovinceid") != null)
                            //{
                            //    if (!eaddress["smx_stateprovince"].ToString().Trim().Equals(companydt.Rows[0]["State_"].ToString())) stringvalues[5] = eaddress["smx_stateprovince"].ToString().Trim();
                            //}

                            //set the bloody state here form geography as the CRM does know what the crap it is doing !!
                            string sqlCommand = "SELECT cfstate FROM geography_definition where beginning_zip_code = '" + stringvalues[6].ToString().Trim() + "'";
                            DataTable geogdt = GetDataSQL(sqlCommand, sqlConn);
                            stringvalues[5] = geogdt.Rows[0]["cfState"].ToString();
                            

                            Id pivcontactid = CreateUpdateAddressRecord(pivotalid, "CU", sqlConn, stringvalues, daab);

                            string MSID = pivotalid.ToString();

                            if (runaccount)
                            {
                                bool success = WriteRecordToCRM(crmid, "account", "accountid", MSID, orgService);
                            }
                            else
                            {
                                bool success = WriteRecordToCRM(crmid, "smx_lab", "smx_labid", MSID, orgService);
                            }
                        }
                    }
                    else
                    {
                        //issue here should never create a new address as acc or lab should exists with piv id ##
                        //Create new address here...
                        //pivotalaccountid = null;

                        //if (eaddress.GetAttributeValue<OptionSetValue>("smx_country") != null)
                        //{
                        //    string cntry = eaddress.FormattedValues["smx_country"].ToString();
                        //    stringvalues[0] = cntry;
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_addressstreet1") != null)
                        //{
                        //    stringvalues[1] = eaddress["smx_addressstreet1"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_addressstreet2") != null)
                        //{
                        //    stringvalues[2] = eaddress["smx_addressstreet2"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_addressstreet3") != null)
                        //{
                        //    stringvalues[3] = eaddress["smx_addressstreet3"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_city") != null)
                        //{
                        //    stringvalues[4] = eaddress["smx_city"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_stateprovince") != null)
                        //{
                        //    stringvalues[5] = eaddress["smx_stateprovince"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_zippostalcode") != null)
                        //{
                        //    stringvalues[6] = eaddress["smx_zippostalcode"].ToString().Trim();
                        //}

                        //CreateUpdateAddressRecord(pivotalid, "CA", sqlConn, stringvalues, daab);
                    }
                }

                //
                // Read data and create Lab adress records
                if (runlab)
                {
                    if (labdt.Rows.Count > 0)
                    {
                        if (labdt.Rows.Count == 1)
                        {
                            if (eaddress.GetAttributeValue<OptionSetValue>("smx_country") != null)
                            {
                                string cntry = eaddress.FormattedValues["smx_country"].ToString();
                                if (!eaddress["smx_country"].ToString().Trim().Equals(labdt.Rows[0]["Country"].ToString())) stringvalues[0] = cntry;
                            }
                            if (eaddress.GetAttributeValue<string>("smx_addressstreet1") != null)
                            {
                                if (!eaddress["smx_addressstreet1"].ToString().Trim().Equals(labdt.Rows[0]["Address_1"].ToString())) stringvalues[1] = eaddress["smx_addressstreet1"].ToString().Trim();
                            }
                            if (eaddress.GetAttributeValue<string>("smx_addressstreet2") != null)
                            {
                                if (!eaddress["smx_addressstreet2"].ToString().Trim().Equals(labdt.Rows[0]["Address_2"].ToString())) stringvalues[2] = eaddress["smx_addressstreet2"].ToString().Trim();
                            }
                            if (eaddress.GetAttributeValue<string>("smx_addressstreet3") != null)
                            {
                                if (!eaddress["smx_addressstreet3"].ToString().Trim().Equals(labdt.Rows[0]["Address_3"].ToString())) stringvalues[3] = eaddress["smx_addressstreet3"].ToString().Trim();
                            }
                            if (eaddress.GetAttributeValue<string>("smx_city") != null)
                            {
                                if (!eaddress["smx_city"].ToString().Trim().Equals(labdt.Rows[0]["City"].ToString())) stringvalues[4] = eaddress["smx_city"].ToString().Trim();
                            }
                            if (eaddress.GetAttributeValue<string>("smx_stateprovince") != null)
                            {
                                if (!eaddress["smx_stateprovince"].ToString().Trim().Equals(labdt.Rows[0]["State_"].ToString())) stringvalues[5] = eaddress["smx_stateprovince"].ToString().Trim();
                            }
                            if (eaddress.GetAttributeValue<string>("smx_zippostalcode") != null)
                            {
                                if (!eaddress["smx_zippostalcode"].ToString().Trim().Equals(labdt.Rows[0]["Zip"].ToString())) stringvalues[6] = eaddress["smx_zippostalcode"].ToString().Trim();
                            }

                            Id pivcontactid = CreateUpdateAddressRecord(pivotalid, "LU", sqlConn, stringvalues, daab);
                            string MSID = pivotalid.ToString();

                            if (runaccount)
                            {
                                bool success = WriteRecordToCRM(crmid, "account", "accountid", MSID, orgService);
                            }
                            else
                            {
                                bool success = WriteRecordToCRM(crmid, "smx_lab", "smx_labid", MSID, orgService);
                            };
                        }
                    }
                    else
                    {
                        //issue here should never create a new address as acc or lab should exists with piv id ##
                        //Create new address here...
                        //Create new lab here...
                        //pivotallabid = null;

                        //if (eaddress.GetAttributeValue<OptionSetValue>("smx_country") != null)
                        //{
                        //    string cntry = eaddress.FormattedValues["smx_country"].ToString();
                        //    stringvalues[0] = cntry;
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_addressstreet1") != null)
                        //{
                        //    stringvalues[1] = eaddress["smx_addressstreet1"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_addressstreet2") != null)
                        //{
                        //    stringvalues[2] = eaddress["smx_addressstreet2"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_addressstreet3") != null)
                        //{
                        //    stringvalues[3] = eaddress["smx_addressstreet3"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_city") != null)
                        //{
                        //    stringvalues[4] = eaddress["smx_city"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_stateprovince") != null)
                        //{
                        //    stringvalues[5] = eaddress["smx_stateprovince"].ToString().Trim();
                        //}
                        //if (eaddress.GetAttributeValue<string>("smx_zippostalcode") != null)
                        //{
                        //    stringvalues[6] = eaddress["smx_zippostalcode"].ToString().Trim();
                        //}

                        //CreateUpdateAddressRecord(pivotalid, "LA", sqlConn, stringvalues, daab);
                    }
                }


                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        ///// <summary>
        ///// SetupContactFields get the fields from contact and crm and compare and then create or update record
        ///// </summary>
        ///// <param> the entity from crm, contact id from pivotal and field values</param>
        private static bool SetupLabFields(Entity crmlab, SqlConnection sqlConn, string pivotallabid, DAAB daab, IOrganizationService orgService)
        {
            try
            {
                string[] stringvalues = new string[10];
                Guid crmid = new Guid();

                string sqlCommand = "SELECT * FROM ctlab where ctlab_id = " + pivotallabid;
                DataTable contactsdt = GetDataSQL(sqlCommand, sqlConn);

                // Read data and create account and sales invoice records
                if (contactsdt.Rows.Count > 0)
                {
                    if (contactsdt.Rows.Count == 1)
                    {
                        Guid _accountid = ((EntityReference)crmlab["smx_account"]).Id;
                        Entity accounte = GetCRMId(_accountid, "account", "accountid", orgService);
                        if (accounte == null)
                        {
                            return false;
                        }

                        var companypivid = accounte.GetAttributeValue<string>("smx_migrationsourceid");
                        if (companypivid != null)
                        {
                            //companypivid = accounte["smx_migrationsourceid"].ToString();
                            Id companyid = Id.Create(companypivid.ToString());
                            string companystrid = companypivid.ToString();
                        }

                        //var doo = crmlab.GetAttributeValue<OptionSetValue>("smx_daysofoperation");
                        //var tla = crmlab.GetAttributeValue<OptionSetValue>("smx_tla");

                        if (crmlab.GetAttributeValue<string>("smx_name") != null)
                        {
                            if (!crmlab["smx_name"].ToString().Trim().Equals(contactsdt.Rows[0]["Lab_Name"].ToString())) stringvalues[0] = crmlab["smx_name"].ToString().Trim();
                        }
                        //if (crmlab.GetAttributeValue<string>("smx_Account") != null)
                        if (((EntityReference)crmlab["smx_account"]).Id != null)
                        {
                            stringvalues[1] = companypivid;   ///got to add the pivid here
                        }
                        else
                        {
                            throw new Exception("Issue on account/lab");
                        }
                        if (crmlab.GetAttributeValue<OptionSetValue>("smx_daysofoperation") != null)
                        {
                            string daysop = crmlab.FormattedValues["smx_daysofoperation"].ToString();
                            if (!crmlab["smx_daysofoperation"].ToString().Trim().Equals(contactsdt.Rows[0]["Days_of_Operation"].ToString())) stringvalues[2] = daysop;
                        }
                        if (crmlab.GetAttributeValue<string>("smx_respondent") != null) //##
                        {
                            if (!crmlab["smx_respondent"].ToString().Trim().Equals(contactsdt.Rows[0]["Respondent"].ToString())) stringvalues[3] = crmlab["smx_respondent"].ToString().Trim();
                        }

                        if (crmlab.Attributes.Contains("smx_noofcbc"))
                        {
                            if (!crmlab["smx_noofcbc"].ToString().Trim().Equals(contactsdt.Rows[0]["No_of_CBC"].ToString())) stringvalues[4] = crmlab["smx_noofcbc"].ToString().Trim();
                        }

                        if (crmlab.Attributes.Contains("smx_sapid"))
                        {
                            if (!crmlab["smx_sapid"].ToString().Trim().Equals(contactsdt.Rows[0]["SAP_Number"].ToString())) stringvalues[5] = crmlab["smx_sapid"].ToString().Trim();
                        }

                        //Inactivate the Lab
                        string labdelete = crmlab.FormattedValues["statecode"];
                        if (labdelete.ToUpper() == "INACTIVE")
                        {
                            SqlCommand asc = new SqlCommand("update ctlab set inactive = 1 where ctlab_id = " + pivotallabid, sqlConn);
                            asc.ExecuteNonQuery();
                            return true;
                        }

                        CreateUpdateLabRecord(pivotallabid, sqlConn, stringvalues, daab);
                    }
                }
                else
                {
                    //Create new contact here...
                    //someValue = condition ? newValue : someValue;
                    pivotallabid = null;

                    if (crmlab.GetAttributeValue<string>("smx_name") != null)
                    {
                        stringvalues[0] = crmlab["smx_name"].ToString().Trim();
                    }

                    if (((EntityReference)crmlab["smx_account"]).Id != null)   
                    //if (crmlab.GetAttributeValue<string>("smx_account") != null)
                    {
                        Guid _accountid = ((EntityReference)crmlab["smx_account"]).Id;   //crm has account id but no source id...##
                        crmid = crmlab.Id;
                        Entity accounte = GetCRMId(_accountid, "account", "accountid", orgService);
                        if (accounte == null)
                        {
                            return false;
                        }

                        var companypivid = accounte.GetAttributeValue<string>("smx_migrationsourceid");
                        if (companypivid != null)
                        {
                            //companypivid = crmlab["smx_migrationsourceid"].ToString();
                            Id companyid = Id.Create(companypivid.ToString());
                            string companystrid = companypivid.ToString();
                        }

                        stringvalues[1] = companypivid.ToString();
                    }
                    if (crmlab.GetAttributeValue<OptionSetValue>("smx_daysofoperation") != null)
                    {
                        string daysop = crmlab.FormattedValues["smx_daysofoperation"].ToString();
                        stringvalues[2] = daysop;
                    }
                    if (crmlab.GetAttributeValue<string>("smx_respondent") != null) //#
                    {
                        stringvalues[3] = crmlab["smx_respondent"].ToString().Trim();
                    }
                    if (crmlab.GetAttributeValue<Int32>("smx_noofcbc") != 0) //#
                    {
                        stringvalues[4] = crmlab["smx_noofcbc"].ToString().Trim();
                    }

                    if (crmlab.GetAttributeValue<string>("smx_respondent") != null) //#
                    {
                        stringvalues[3] = crmlab["smx_respondent"].ToString().Trim();
                    }

                    //if (crmlab.Attributes.Contains("smx_sapid"))
                    if (crmlab.GetAttributeValue<string>("smx_sapid") != null) //#
                    {
                        stringvalues[5] = crmlab["smx_sapid"].ToString().Trim();
                    }

                    Id pivcontactid = CreateUpdateLabRecord(pivotallabid, sqlConn, stringvalues, daab);
                    string MSID = pivcontactid.ToString();

                    bool success = WriteRecordToCRM(crmid, "smx_lab", "smx_labid", MSID, orgService);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        ///// <summary>
        ///// SetupContactFields get the fields from contact and crm and compare and then create or update record
        ///// </summary>
        ///// <param> the entity from crm, contact id from pivotal and field values</param>
        private static bool SetupInstrumentFields(Entity crminstr, SqlConnection sqlConn, string pivotalinstrumentid, DAAB daab, IOrganizationService orgService)
        {
            try
            {
                string[] stringvalues = new string[11];
                string prodline = "Heme";
                Guid _compacc = new Guid();
                Guid _labacc = new Guid();
                Guid _modelacc = new Guid();

                Guid _prodline = new Guid();
                Guid _model = new Guid();
                Guid _manuf = new Guid();

                string compstrid = "";
                string labstrid = "";
                string modelstrid = "";

                string metrixinstrument = crminstr["smx_metrixinstrument"].ToString().Trim();
                if (metrixinstrument.ToUpper() == "NO")
                {
                    return true;
                }



                string sqlCommand = "SELECT * FROM ctPlacement where ctPlacement_Id = " + pivotalinstrumentid;
                DataTable contactsdt = GetDataSQL(sqlCommand, sqlConn);

                // Read data and create account and sales invoice records
                if (contactsdt.Rows.Count > 0)
                {
                    if (contactsdt.Rows.Count == 1)
                    {
                        //var doo = crminstr.GetAttributeValue<OptionSetValue>("smx_daysofoperation");
                        //var tla = crminstr.GetAttributeValue<OptionSetValue>("smx_tla");

                        if (crminstr.GetAttributeValue<string>("smx_name") != null)
                        {
                            stringvalues[0] = crminstr["smx_name"].ToString().Trim();
                        }

                        //get crmid here and add
                        if (crminstr.Attributes.Contains("smx_lab"))
                        {
                            _labacc = ((EntityReference)crminstr["smx_lab"]).Id;
                            Entity accounte = GetCRMId(_labacc, "smx_lab", "smx_labid", orgService);  //find the table here for employee
                            if (accounte == null)
                            {
                                return false;
                            }
                            labstrid = accounte.GetAttributeValue<string>("smx_migrationsourceid");   //string or var
                            stringvalues[1] = labstrid;
                        }
                        //if (crminstr.GetAttributeValue<string>("smx_lab") != null)
                        //{
                        //    if (!crminstr["smx_lab"].ToString().Trim().Equals(contactsdt.Rows[0]["ctLab_Id"].ToString())) stringvalues[1] = crminstr["smx_lab"].ToString().Trim();
                        //}

                        if (crminstr.Attributes.Contains("smx_account"))
                        {
                            _compacc = ((EntityReference)crminstr["smx_account"]).Id;
                            Entity accounte = GetCRMId(_compacc, "account", "accountid", orgService);  //find the table here for employee
                            if (accounte == null)
                            {
                                return false;
                            }
                            compstrid = accounte.GetAttributeValue<string>("smx_migrationsourceid");   //string or var
                            stringvalues[2] = compstrid;
                        }

                        if (crminstr.Attributes.Contains("smx_productline"))
                        {
                            _prodline = ((EntityReference)crminstr["smx_productline"]).Id;
                            Entity elab = GetCRMId(_prodline, "smx_productline", "smx_productlineid", orgService);
                            if (elab == null)
                            {
                                return false;
                            }
                            var labgid = elab.GetAttributeValue<string>("smx_name");
                            stringvalues[3] = labgid.ToString();
                        }
                        if (crminstr.Attributes.Contains("smx_model"))
                        {
                            _model = ((EntityReference)crminstr["smx_model"]).Id;
                            Entity elab = GetCRMId(_model, "smx_model", "smx_modelid", orgService);
                            if (elab == null)
                            {
                                return false;
                            }
                            var labgid = elab.GetAttributeValue<string>("smx_name");
                            stringvalues[4] = labgid.ToString();
                        }
                        if (crminstr.Attributes.Contains("smx_manufacturer"))
                        {
                            _manuf = ((EntityReference)crminstr["smx_manufacturer"]).Id;
                            Entity elab = GetCRMId(_manuf, "smx_manufacturer", "smx_manufacturerid", orgService);
                            if (elab == null)
                            {
                                return false;
                            }
                            var labgid = elab.GetAttributeValue<string>("smx_name");
                            stringvalues[5] = labgid.ToString();
                        }

                        //rest of fields
                        if (crminstr.GetAttributeValue<string>("smx_yearofacquisition") != null)
                        {
                            stringvalues[6] = crminstr["smx_yearofacquisition"].ToString().Trim();
                        }
                        if (crminstr.GetAttributeValue<OptionSetValue>("smx_modeofacquisition") != null)
                        {
                            string modeaq = crminstr.FormattedValues["smx_modeofacquisition"].ToString();
                            stringvalues[7] = modeaq;
                        }

                        if (crminstr.GetAttributeValue<OptionSetValue>("smx_areaoffocus") != null)   ///fix issue and test area of focus
                        {
                            if (crminstr.FormattedValues["smx_areaoffocus"] == null)
                            {
                            }
                            else
                            {
                                string areafocus = crminstr.FormattedValues["smx_areaoffocus"].ToString();
                                //if (!crminstr["smx_areaoffocus"].ToString().Trim().Equals(contactsdt.Rows[0]["Flow_Area_Focus"].ToString())) stringvalues[8] = areafocus;
                                stringvalues[8] = areafocus;
                            }
                        }

                        if (crminstr.GetAttributeValue<OptionSetValue>("smx_reagentvolume") != null)  //this too
                        {
                            if (crminstr.FormattedValues["smx_reagentvolume"] == null)
                            {
                            }
                            else
                            {
                                string reagentvolume = crminstr.FormattedValues["smx_reagentvolume"].ToString();
                                //if (!crminstr["smx_reagentvolume"].ToString().Trim().Equals(contactsdt.Rows[0]["Flow_Reagent_Volume"].ToString())) stringvalues[9] = reagentvolume;
                                stringvalues[9] = reagentvolume;
                            }
                        }

                        if (crminstr.GetAttributeValue<OptionSetValue>("smx_testmethod") != null)
                        {
                            string testmethod = crminstr.FormattedValues["smx_testmethod"].ToString();
                            if (!crminstr["smx_testmethod"].ToString().Trim().Equals(contactsdt.Rows[0]["TestMethod"].ToString())) stringvalues[10] = testmethod;
                        }

                        string instrumentdelete = crminstr.FormattedValues["statecode"];

                        if (instrumentdelete.ToUpper() == "INACTIVE")
                        {
                            SqlCommand asc = new SqlCommand("update ctplacement set inactive = 1 where ctplacement_Id = " + pivotalinstrumentid, sqlConn);
                            asc.ExecuteNonQuery();

                            //bool returnsendemail = SendEmail("akers@sysmex.com", "Check Contact detail: ",fname + " " + lname + ". Check Contact is inactive in CRM and Deleted in Pivotal" + stringvalues[0] + " " + stringvalues[1], "martinh@sysmex.com");

                            return true;
                        }
                        else
                        {
                            SqlCommand asc = new SqlCommand("update ctplacement set inactive = 0 where ctplacement_Id = " + pivotalinstrumentid, sqlConn);
                            asc.ExecuteNonQuery();
                        }

                        //not in pivotal
                        //if (!crminstr["smx_name"].ToString().Trim().Equals(contactsdt.Rows[0]["instrument_Name"].ToString())) stringvalues[0] = crminstr["smx_name"].ToString().Trim();
                        ////Manufacturer look up on crm find text and send to field for update maybe? the update should auto update in pivotal when the update to model takes place.
                        //if (!crminstr["smx_manufacturer"].ToString().Trim().Equals(contactsdt.Rows[0]["Placement_Filter"].ToString())) stringvalues[2] = crminstr["smx_manufacturer"].ToString().Trim();
                        //if (!crminstr["smx_frequencyofuse"].ToString().Trim().Equals(contactsdt.Rows[0]["Frequency_of_Use"].ToString())) stringvalues[3] = crminstr["smx_frequencyofuse"].ToString().Trim();
                        //if (crminstr.GetAttributeValue<OptionSetValue>("smx_reagentvolume") != null)
                        //if (crminstr.GetAttributeValue<OptionSetValue>("smx_middleware") != null)
                        //if (crminstr.GetAttributeValue<OptionSetValue>("smx_scopeofmenu") != null)

                        Id pivinstrumentid = CreateUpdateinstrumentRecord(pivotalinstrumentid, sqlConn, stringvalues, daab);
                    }
                }
                else
                {
                    //Create new instrument here...
                    //someValue = condition ? newValue : someValue;
                    Guid _labid = new Guid();
                    pivotalinstrumentid = null;
                    string model = "";

                    //_model = ((EntityReference)crminstr["smx_instrumentid"]).Id;
                    Guid crmid = crminstr.Id;

                    //First go fetch the Lab and the piv id..
                    string companyid = "";
                    string labid = "";
                    if (crminstr.Attributes.Contains("smx_lab"))
                    {
                        _labid = ((EntityReference)crminstr["smx_lab"]).Id;
                    }

                    Entity labe = GetCRMId(_labid, "smx_lab", "smx_labid", orgService);

                    if (labe == null)
                    {
                        return false;
                    }

                    var labpivid = labe.GetAttributeValue<string>("smx_migrationsourceid");
                    if (labpivid != null)
                    {
                        //companypivid = contct["smx_migrationsourceid"].ToString();
                        labid = labpivid.ToString();
                        stringvalues[1] = labid;

                        string sqlCompany = "SELECT * FROM ctLab where ctlab_id = " + labid;
                        DataTable labdt = GetDataSQL(sqlCompany, sqlConn);

                        // Read data and create account and sales invoice records
                        if (labdt.Rows.Count == 1)
                        {
                            Id compid = Id.Create(labdt.Rows[0]["Company_Id"]);
                            companyid = compid.ToString();
                            stringvalues[2] = companyid;
                        }
                        else
                        {
                            LogMessage("", "Error on reading companyid");
                        }
                    }

                    //if (crminstr.GetAttributeValue<string>("smx_name") != null)   //Name does not get filled in ##
                    if (crminstr.Attributes.Contains("smx_name"))
                    {
                        stringvalues[0] = crminstr["smx_name"].ToString().Trim();
                        model = crminstr["smx_name"].ToString().Trim();
                        if (model.Contains("Unknown"))
                        {
                            model = "Unknown";
                        }
                    }


                    //Lookups
                    if (crminstr.Attributes.Contains("smx_productline"))
                    {
                        _prodline = ((EntityReference)crminstr["smx_productline"]).Id;
                        Entity elab = GetCRMId(_prodline, "smx_productline", "smx_productlineid", orgService);
                        if (elab == null)
                        {
                            return false;
                        }
                        var labgid = elab.GetAttributeValue<string>("smx_name");
                        stringvalues[3] = labgid.ToString();
                    }

                    if (crminstr.Attributes.Contains("smx_model"))
                    {
                        _model = ((EntityReference)crminstr["smx_model"]).Id;
                        Entity elab = GetCRMId(_model, "smx_model", "smx_modelid", orgService);
                        if (elab == null)
                        {
                            return false;
                        }
                        var labgid = elab.GetAttributeValue<string>("smx_name");
                        stringvalues[4] = labgid.ToString();
                        model = labgid.ToString();
                    }
                    else
                    {
                        return false;
                    }

                    if (crminstr.Attributes.Contains("smx_manufacturer"))
                    {
                        _manuf = ((EntityReference)crminstr["smx_manufacturer"]).Id;
                        Entity elab = GetCRMId(_manuf, "smx_manufacturer", "smx_manufacturerid", orgService);
                        if (elab == null)
                        {
                            return false;
                        }
                        var labgid = elab.GetAttributeValue<string>("smx_name");
                        stringvalues[5] = labgid.ToString();
                    }

                    //rest of the fields
                    if (crminstr.GetAttributeValue<string>("smx_yearofacquisition") != null)
                    {
                        stringvalues[6] = crminstr["smx_yearofacquisition"].ToString().Trim();
                    }
                    if (crminstr.GetAttributeValue<OptionSetValue>("smx_ModeofAcquisition") != null)
                    {
                        string modeaq = crminstr.FormattedValues["smx_modeofacquisition"].ToString();
                        stringvalues[7] = modeaq;
                    }

                    //start fix here...

                    if (crminstr.GetAttributeValue<OptionSetValue>("smx_areaoffocus") != null)   ///fix issue and test area of focus
                    {
                        if (crminstr.FormattedValues["smx_areaoffocus"] == null)
                        {
                        }
                        else
                        {
                            string areafocus = crminstr.FormattedValues["smx_areaoffocus"].ToString();
                            stringvalues[8] = areafocus;
                        }
                    }

                    if (crminstr.GetAttributeValue<OptionSetValue>("smx_reagentvolume") != null)  //this too
                    {
                        if (crminstr.FormattedValues["smx_reagentvolume"] == null)
                        {
                        }
                        else
                        {
                            string reagentvolume = crminstr.FormattedValues["smx_reagentvolume"].ToString();
                            stringvalues[9] = reagentvolume;
                        }
                    }

                    if (stringvalues[3].ToUpper() == "URINALYSIS")
                    {
                        if (crminstr.GetAttributeValue<OptionSetValue>("smx_testmethod") != null)
                        {
                            string testmethod = crminstr.FormattedValues["smx_testmethod"].ToString();
                            stringvalues[10] = testmethod;
                        }
                    }
                    else
                    {
                        stringvalues[10] = "";
                    }

                    if (companyid == null && labpivid == null)
                    {

                    }
                    else
                    {
                        //sqlCommand = "SELECT * FROM ctPlacement where company_id = " + companyid + " and lab_id = " + labpivid;
                        //DataTable pivinstr = GetDataSQL(sqlCommand, sqlConn);

                        //// Read data and create account and sales invoice records
                        //if (pivinstr.Rows.Count > 0)
                        //{
                        //    if (pivinstr.Rows.Count == 1)
                        //    {
                        //        pivotalinstrumentid = Id.Create(pivinstr.Rows[0]["ctPlacement_Id"]).ToString();
                        //    }
                        //    else
                        //    {
                        //        foreach (DataRow idr in pivinstr.Rows)
                        //        {
                        //            string crmmodel = Convert.ToString(idr["Import_CRM"]);
                        //            sqlCommand = "SELECT * FROM ctPlacement where company_id = " + companyid + " and lab_id = " + labpivid + " and Import_CRM = '" + model.Trim() + "'";
                        //            DataTable singlepivinstr = GetDataSQL(sqlCommand, sqlConn);

                        //            // Read data and create account and sales invoice records
                        //            if (singlepivinstr.Rows.Count == 1)
                        //            {
                        //                pivotalinstrumentid = Id.Create(singlepivinstr.Rows[0]["ctPlacement_Id"]).ToString();
                        //                break;
                        //            }
                        //            else
                        //            {
                        //                if (singlepivinstr.Rows.Count > 1)
                        //                {
                        //                    string pausehere2 = "";   //check this record and update accordingly
                        //                    break;
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                    }


                    //test this
                    Id pivinstrumentid = CreateUpdateinstrumentRecord(pivotalinstrumentid, sqlConn, stringvalues, daab);

                    string MSID = pivinstrumentid.ToString();

                    bool success = WriteRecordToCRM(crmid, "smx_instrument", "smx_instrumentid", MSID, orgService);


                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
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

        private static DateTime GetMaxRunDate(SqlConnection sqlConn)
        {
            log.Debug("Retrieving last record in Customer Database");
            DateTime lastcontactfetchdate = DateTime.Today.AddDays(-7);
            string sqlCommand = "SELECT * FROM system";
            DataTable systemdt = GetDataSQL(sqlCommand, sqlConn);
            if (systemdt.Rows.Count == 1)
            {
                lastcontactfetchdate = Convert.ToDateTime(systemdt.Rows[0]["Last_CRM_Update_Date"]);
            }
            
            return lastcontactfetchdate;
        }

        private static QueryExpression BuildQueryExpression(DateTime maxRunDate, string entityName)
        {

            //QueryExpression query = new QueryExpression(contact.LogicalName);
            QueryExpression query = new QueryExpression();
            query.EntityName = entityName;
            query.ColumnSet = new ColumnSet(true);
            //query.Criteria.FilterOperator = LogicalOperator.Or;
            query.Criteria.AddCondition("modifiedon", ConditionOperator.GreaterThan, maxRunDate);

            //query.Criteria.AddCondition("businessunit", "name", ConditionOperator.Equal, "Sysmex Americas");

            //query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1);


            //just get first 5 records
            //query.PageInfo = new PagingInfo();
            //query.PageInfo.Count = 5; // or 50, or whatever
            //query.PageInfo.PageNumber = 1;

            ////query.Criteria.AddCondition("name", ConditionOperator.Equal, accountName);
            //EntityCollection contactresults = crmService.RetrieveMultiple(query);

            //log.Debug("Building Query Expression for CRM");
            //QueryExpression queryExpression = new QueryExpression();
            //queryExpression.EntityName = "contact";
            //queryExpression.ColumnSet = new ColumnSet("contactid", "modifiedon");
            //queryExpression.ColumnSet.Columns.Add("contactid");
            //queryExpression.AddOrder("modifiedon", OrderType.Ascending);
            //ConditionExpression conditionExpression = new ConditionExpression("modifiedon", ConditionOperator.GreaterThan, maxRunDate);
            //queryExpression.Criteria.AddCondition(conditionExpression);

            return query;
        }

        /// <summary>
        /// gets data from outside source
        /// </summary>
        /// <param name="text">sql< input with connection string/param>
        private static DataTable GetDataSQL(string sSql, SqlConnection sqlconn)
        {
            try
            {
                SqlCommand cmd1 = new SqlCommand(sSql, sqlconn);
                SqlDataAdapter da = new SqlDataAdapter(cmd1);

                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                //log.Error(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Write a record to pivotal if it is new, use DAAB to confiure pivotal id entry
        /// </summary>
        /// <param name="sqlConn"></param>
        /// <param name="stringvalues"></param>
        /// <param name="daab"></param>
        /// <returns></returns>
        private static int CreateUpdateAccountRecord(string accountid, SqlConnection sqlConn, string[] stringvalues, DAAB daab)
        {
            StringBuilder ft;
            StringBuilder fv;
            StringBuilder fn;

            var fieldNames = new List<string>();
            var fieldValues = new List<string>();
            var fieldTypes = new List<string>();

            try
            {

                if (stringvalues[0] != null)
                {
                    if (stringvalues[0].Length > 40)
                    {
                        stringvalues[0] = stringvalues[0].Substring(0, 40);
                    }
                    fieldNames.Add("Company_Name");
                    fieldValues.Add(stringvalues[0].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[1] != null)
                {
                    fieldNames.Add("cfAlt_Territory_Manager");
                    fieldValues.Add(stringvalues[1].ToString());
                    fieldTypes.Add("Id");
                }
                if (stringvalues[2] != null)
                {
                    fieldNames.Add("Partner_Company_Id");
                    fieldValues.Add(stringvalues[2].ToString());
                    fieldTypes.Add("Id");
                }
                else
                {
                    //Get partner id and check if changing from something to blank
                    string sqlCommand = "SELECT * FROM company where company_id = " + accountid;
                    DataTable companydt = GetDataSQL(sqlCommand, sqlConn);

                    // Read data and create account and sales invoice records
                    if (companydt.Rows.Count == 1)
                    {
                        Id pardid = Id.Create(companydt.Rows[0]["Partner_Company_Id"]);
                        if ((stringvalues[2] == null || stringvalues[2] == "") && pardid != null)
                        {
                            SqlCommand asc = new SqlCommand("update company set Partner_Company_Id = null, Partner_Contact_Id = null where company_id =  = " + accountid, sqlConn);
                            asc.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string issuehere = "";
                    }
                }

                if (stringvalues[3] != null)
                {
                    fieldNames.Add("ctGPO_Id");
                    fieldValues.Add(stringvalues[3].ToString());
                    fieldTypes.Add("Id");
                }

                if (stringvalues[4] != null)
                {
                    fieldNames.Add("ctIHN_Id");
                    fieldValues.Add(stringvalues[4].ToString());
                    fieldTypes.Add("Id");
                }

                if (stringvalues[5] != null)
                {
                    fieldNames.Add("ctIHN_Secondary_Id");
                    fieldValues.Add(stringvalues[5].ToString());
                    fieldTypes.Add("Id");
                }
                if (stringvalues[6] != null)
                {
                    fieldNames.Add("cfBed_Size");
                    fieldValues.Add(stringvalues[6].ToString());
                    fieldTypes.Add("Float");
                }
                if (stringvalues[7] != null)
                {
                    fieldNames.Add("Phone");
                    fieldValues.Add(stringvalues[7].ToString());
                    fieldTypes.Add("Text");
                }

                if (stringvalues[8] != null)
                {
                    fieldNames.Add("cfNo_of_CBC");
                    fieldValues.Add(stringvalues[8].ToString());
                    fieldTypes.Add("Float");
                }
                //else
                //{
                //    fieldNames.Add("cfNo_of_CBC");
                //    fieldValues.Add("0");
                //    fieldTypes.Add("Float");
                //}
    
                fn = new StringBuilder();
                foreach (var fieldName in fieldNames)
                {
                    fn.Append(fieldName);
                    fn.Append("|");
                }

                ft = new StringBuilder();
                foreach (var fieldName in fieldTypes)
                {
                    ft.Append(fieldName);
                    ft.Append("|");
                }

                fv = new StringBuilder();
                foreach (var fieldName in fieldValues)
                {
                    fv.Append(fieldName);
                    fv.Append("|");
                }

                if (accountid == null || accountid == "")
                {
                    //No add of account record in Pivotal
                }
                else
                {
                    daab.UpdateRecord("Company", fn.ToString().TrimEnd('|'),
                                                    fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), accountid.ToString(),
                                                    "Company_Id");

                }

                ft = null;
                fv = null;
                fn = null;
                return 1;

            }
            catch (Exception exc)
            {
                ft = null;
                fv = null;
                fn = null;
                return 0;
            }
        }

        //
        /// <summary>
        /// Write a record to pivotal if it is new, use DAAB to confiure pivotal id entry
        /// </summary>
        /// <param name="sqlConn"></param>
        /// <param name="stringvalues"></param>
        /// <param name="daab"></param>
        /// <returns></returns>
        private static Id CreateUpdateContactRecord(string contactid, SqlConnection sqlConn, string[] stringvalues, DAAB daab)
        {
            StringBuilder ft;
            StringBuilder fv;
            StringBuilder fn;

            var fieldNames = new List<string>();
            var fieldValues = new List<string>();
            var fieldTypes = new List<string>();

            Id pivcontactid = null;

            try
            {

                if (stringvalues[0] != null)
                {
                    fieldNames.Add("First_Name");
                    fieldValues.Add(stringvalues[0].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[1] != null)
                {
                    fieldNames.Add("Last_Name");
                    fieldValues.Add(stringvalues[1].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[2] != null)
                {
                    fieldNames.Add("Job_Title");
                    fieldValues.Add(stringvalues[2].ToString());
                    fieldTypes.Add("Text");
                    //fieldTypes.Add("Integer");
                }
                if (stringvalues[3] != null)
                {
                    fieldNames.Add("Title");
                    fieldValues.Add(stringvalues[3].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[4] != null)
                {
                    fieldNames.Add("cfJob_Title_Other");
                    fieldValues.Add(stringvalues[4].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[5] != null)
                {
                    fieldNames.Add("Address_1");
                    fieldValues.Add(stringvalues[5].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[6] != null)
                {
                    fieldNames.Add("Address_2");
                    fieldValues.Add(stringvalues[6].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[7] != null)
                {
                    fieldNames.Add("City");
                    fieldValues.Add(stringvalues[7].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[8] != null)
                {
                    fieldNames.Add("State_");
                    fieldValues.Add(stringvalues[8].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[9] != null)
                {
                    fieldNames.Add("Zip");
                    fieldValues.Add(stringvalues[9].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[10] != null)
                {
                    fieldNames.Add("Phone");
                    fieldValues.Add(stringvalues[10].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[11] != null)
                {
                    fieldNames.Add("Extension");
                    fieldValues.Add(stringvalues[11].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[12] != null)
                {
                    fieldNames.Add("Cell");
                    fieldValues.Add(stringvalues[12].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[13] != null)
                {
                    fieldNames.Add("Email");
                    fieldValues.Add(stringvalues[13].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[14] != null)
                {
                    fieldNames.Add("cfODIS_FC");
                    fieldValues.Add(stringvalues[14].ToString());
                    fieldTypes.Add("Boolean");
                }
                if (stringvalues[15] != null)
                {
                    fieldNames.Add("cfODIS_LIS");
                    fieldValues.Add(stringvalues[15].ToString());
                    fieldTypes.Add("Boolean");
                }
                if (stringvalues[16] != null)
                {
                    fieldNames.Add("cfODIS_PCN");
                    fieldValues.Add(stringvalues[16].ToString());
                    fieldTypes.Add("Boolean");
                }
                if (stringvalues[17] != null)
                {
                    fieldNames.Add("cfODIS_WSAR");
                    fieldValues.Add(stringvalues[17].ToString());
                    fieldTypes.Add("Boolean");
                }
                if (stringvalues[18] != null)
                {
                    fieldNames.Add("cfODIS_TCN");
                    fieldValues.Add(stringvalues[18].ToString());
                    fieldTypes.Add("Boolean");
                }

                if (stringvalues[19] != null)
                {
                    fieldNames.Add("cfODIS_MC1");
                    fieldValues.Add(stringvalues[19].ToString());
                    fieldTypes.Add("Boolean");
                }
                if (stringvalues[20] != null)
                {
                    fieldNames.Add("cfODIS_SAR");
                    fieldValues.Add(stringvalues[20].ToString());
                    fieldTypes.Add("Boolean");
                }
                if (stringvalues[21] != null)
                {
                    fieldNames.Add("cfODIS_CPR");
                    fieldValues.Add(stringvalues[21].ToString());
                    fieldTypes.Add("Boolean");
                }

                if (stringvalues[22] != null)
                {
                    fieldNames.Add("cfSysmex_Newsletter");
                    fieldValues.Add(stringvalues[22].ToString());
                    fieldTypes.Add("Boolean");
                }
                if (stringvalues[23] != null)
                {
                    fieldNames.Add("cfSysmex_Journal");
                    fieldValues.Add(stringvalues[23].ToString());
                    fieldTypes.Add("Boolean");
                }

                if (stringvalues[24] != null)
                {
                    fieldNames.Add("Comments");
                    fieldValues.Add(stringvalues[24].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[25] != null && stringvalues[25] != "")
                {
                    fieldNames.Add("Company_Id");
                    fieldValues.Add(stringvalues[25].ToString());
                    fieldTypes.Add("Id");
                }
                if (stringvalues[26] != null && stringvalues[26] != "")
                {
                    fieldNames.Add("Account_Manager_Id");
                    fieldValues.Add(stringvalues[26].ToString());
                    fieldTypes.Add("Id");
                }
                if (stringvalues[27] != null && stringvalues[27] != "")
                {
                    fieldNames.Add("Territory_Id");
                    fieldValues.Add(stringvalues[27].ToString());
                    fieldTypes.Add("Id");
                }

                if (stringvalues[28] != null && stringvalues[28] != "")
                {
                    fieldNames.Add("Type");
                    fieldValues.Add(stringvalues[28].ToString());
                    fieldTypes.Add("Text");
                }

                fieldNames.Add("Country");
                fieldValues.Add("US");
                fieldTypes.Add("Text");

                //if (stringvalues[16].ToString() != "")
                //{
                //    fieldNames.Add("Job_Title");
                //    fieldValues.Add(stringvalues[16].ToString());
                //    fieldTypes.Add("Integer");
                //    fieldTypes.Add("Boolean");
                //}

                //if (retic != "")
                //{
                //    fieldNames.Add("RETIC");
                //    fieldValues.Add(stringvalues[0].ToString());
                //    fieldTypes.Add("Float");
                //}

                //fieldNames.Add("Date_Recorded");
                //fieldValues.Add(stringvalues[0].ToString());
                //fieldTypes.Add("Date");


                fn = new StringBuilder();
                foreach (var fieldName in fieldNames)
                {
                    fn.Append(fieldName);
                    fn.Append("|");
                }

                ft = new StringBuilder();
                foreach (var fieldName in fieldTypes)
                {
                    ft.Append(fieldName);
                    ft.Append("|");
                }

                fv = new StringBuilder();
                foreach (var fieldName in fieldValues)
                {
                    fv.Append(fieldName);
                    fv.Append("|");
                }

                if (contactid == null || contactid == "")
                {
                     pivcontactid = daab.AddRecord("Contact", fn.ToString().TrimEnd('|'),
                                                    fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), "",
                                                    "Contact_Id");

                }
                else
                {
                    daab.UpdateRecord("Contact", fn.ToString().TrimEnd('|'),
                                                    fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), contactid.ToString(),
                                                    "Contact_Id");

                    pivcontactid = Id.Create(contactid);
                }

                ft = null;
                fv = null;
                fn = null;
                return pivcontactid;

            }
            catch (Exception exc)
            {
                ft = null;
                fv = null;
                fn = null;
                return null;
            }
        }

        /// <summary>
        /// Write a record to pivotal if it is new, use DAAB to confiure pivotal id entry
        /// </summary>
        /// <param name="sqlConn"></param>
        /// <param name="stringvalues"></param>
        /// <param name="daab"></param>
        /// <returns></returns>
        private static Id CreateUpdateAddressRecord(string addressid, string action, SqlConnection sqlConn, string[] stringvalues, DAAB daab)
        {
            StringBuilder ft;
            StringBuilder fv;
            StringBuilder fn;

            var fieldNames = new List<string>();
            var fieldValues = new List<string>();
            var fieldTypes = new List<string>();

            Id pivcontactid = null;

            try
            {

                if (action == "CU" || action == "CA")
                {
                    if (stringvalues[0] != null)
                    {
                        fieldNames.Add("Company_Id");
                        fieldValues.Add(addressid.ToString());
                        fieldTypes.Add("Id");
                    }
                }
                if (action == "LU" || action == "LA")
                {
                    if (stringvalues[0] != null)
                    {
                        fieldNames.Add("ctLab_Id");
                        fieldValues.Add(addressid.ToString());
                        fieldTypes.Add("Text");
                    }
                }

                if (stringvalues[0] != null)
                {
                    fieldNames.Add("Country");
                    fieldValues.Add(stringvalues[0].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[1] != null)
                {
                    fieldNames.Add("Address_1");
                    fieldValues.Add(stringvalues[1].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[2] != null)
                {
                    fieldNames.Add("Address_2");
                    fieldValues.Add(stringvalues[2].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[4] != null)
                {
                    fieldNames.Add("City");
                    fieldValues.Add(stringvalues[4].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[5] != null)
                {
                    fieldNames.Add("State_");
                    fieldValues.Add(stringvalues[5].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[6] != null)
                {
                    fieldNames.Add("Zip");
                    fieldValues.Add(stringvalues[6].ToString());
                    fieldTypes.Add("Text");
                }
    
                fn = new StringBuilder();
                foreach (var fieldName in fieldNames)
                {
                    fn.Append(fieldName);
                    fn.Append("|");
                }

                ft = new StringBuilder();
                foreach (var fieldName in fieldTypes)
                {
                    ft.Append(fieldName);
                    ft.Append("|");
                }

                fv = new StringBuilder();
                foreach (var fieldName in fieldValues)
                {
                    fv.Append(fieldName);
                    fv.Append("|");
                }

                if (action == "CU" || action == "CA")
                {
                    if (action == "CA")
                    {
                        pivcontactid = daab.AddRecord("Company", fn.ToString().TrimEnd('|'),
                                                        fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), "",
                                                        "Company_Id");
                    }
                    else
                    {
                        daab.UpdateRecord("Company", fn.ToString().TrimEnd('|'),
                                                        fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), addressid.ToString(),
                                                        "Company_Id");

                        pivcontactid = Id.Create(addressid);
                    }
                }
                if (action == "LU" || action == "LA")
                {
                    if (action == "LA")
                    {
                        pivcontactid = daab.AddRecord("ctLab", fn.ToString().TrimEnd('|'),
                                                        fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), "",
                                                        "ctLab_Id");
                    }
                    else
                    {
                        daab.UpdateRecord("ctLab", fn.ToString().TrimEnd('|'),
                                                        fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), addressid.ToString(),
                                                        "ctLab_Id");

                        pivcontactid = Id.Create(addressid);
                    }
                }

                ft = null;
                fv = null;
                fn = null;
                return pivcontactid;

            }
            catch (Exception exc)
            {
                ft = null;
                fv = null;
                fn = null;
                return null;
            }
        }

        /// <summary>
        /// Write a record to pivotal if it is new, use DAAB to confiure pivotal id entry
        /// </summary>
        /// <param name="sqlConn"></param>
        /// <param name="stringvalues"></param>
        /// <param name="daab"></param>
        /// <returns></returns>
        private static Id CreateUpdateLabRecord(string labid, SqlConnection sqlConn, string[] stringvalues, DAAB daab)
        {
            StringBuilder ft;
            StringBuilder fv;
            StringBuilder fn;

            var fieldNames = new List<string>();
            var fieldValues = new List<string>();
            var fieldTypes = new List<string>();

            Id pivlabid = null;

            try
            {

                if (stringvalues[0] != null)
                {
                    fieldNames.Add("Lab_Name");
                    fieldValues.Add(stringvalues[0].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[1] != null)
                {
                    fieldNames.Add("Company_Id");
                    fieldValues.Add(stringvalues[1].ToString());
                    fieldTypes.Add("Id");
                }
                if (stringvalues[2] != null)
                {
                    fieldNames.Add("Days_of_Operation");
                    fieldValues.Add(stringvalues[2].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[3] != null)
                {
                    fieldNames.Add("Respondent");
                    fieldValues.Add(stringvalues[3].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[4] != null)
                {
                    fieldNames.Add("No_of_CBC");
                    fieldValues.Add(stringvalues[4].ToString());
                    fieldTypes.Add("Integer");
                }

                if (stringvalues[5] != null)
                {
                    fieldNames.Add("SAP_Number");
                    fieldValues.Add(stringvalues[5].ToString());
                    fieldTypes.Add("Text");
                }
                //if (stringvalues[5] != null)
                //{
                //    fieldNames.Add("Address_1");
                //    fieldValues.Add(stringvalues[6].ToString());
                //    fieldTypes.Add("Text");
                //}
                //if (stringvalues[6] != null)
                //{
                //    fieldNames.Add("Address_2");
                //    fieldValues.Add(stringvalues[7].ToString());
                //    fieldTypes.Add("Text");
                //}
                //if (stringvalues[7] != null)
                //{
                //    fieldNames.Add("City");
                //    fieldValues.Add(stringvalues[8].ToString());
                //    fieldTypes.Add("Text");
                //}
                //if (stringvalues[8] != null)
                //{
                //    fieldNames.Add("State_");
                //    fieldValues.Add(stringvalues[9].ToString());
                //    fieldTypes.Add("Text");
                //}
                //if (stringvalues[9] != null)
                //{
                //    fieldNames.Add("Zip");
                //    fieldValues.Add(stringvalues[9].ToString());
                //    fieldTypes.Add("Text");
                //}

                fn = new StringBuilder();
                foreach (var fieldName in fieldNames)
                {
                    fn.Append(fieldName);
                    fn.Append("|");
                }

                ft = new StringBuilder();
                foreach (var fieldName in fieldTypes)
                {
                    ft.Append(fieldName);
                    ft.Append("|");
                }

                fv = new StringBuilder();
                foreach (var fieldName in fieldValues)
                {
                    fv.Append(fieldName);
                    fv.Append("|");
                }

                if (labid == null || labid == "")
                {
                   pivlabid  = daab.AddRecord("ctLab", fn.ToString().TrimEnd('|'),
                                                    fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), "",
                                                    "ctLab_Id");
                }
                else
                {
                    daab.UpdateRecord("ctLab", fn.ToString().TrimEnd('|'),
                                                    fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), labid.ToString(),
                                                    "ctLab_Id");

                    pivlabid = Id.Create(labid);
                }

                ft = null;
                fv = null;
                fn = null;
                return pivlabid;

            }
            catch
            {
                ft = null;
                fv = null;
                fn = null;
                return null;
            }
        }

        /// <summary>
        /// Write a record to pivotal if it is new, use DAAB to confiure pivotal id entry
        /// </summary>
        /// <param name="sqlConn"></param>
        /// <param name="stringvalues"></param>
        /// <param name="daab"></param>
        /// <returns></returns>
        private static Id CreateUpdateinstrumentRecord(string instrumentid, SqlConnection sqlConn, string[] stringvalues, DAAB daab)
        {
            StringBuilder ft;
            StringBuilder fv;
            StringBuilder fn;

            var fieldNames = new List<string>();
            var fieldValues = new List<string>();
            var fieldTypes = new List<string>();

            try
            {
                string type = "";
                if (stringvalues[3] != null && stringvalues[3] != "")
                {
                    type = stringvalues[3].ToString();
                    if (type.ToUpper() != "URINALYSIS")
                    {
                        stringvalues[10] = "";
                    }
                }
                else
                {
                    return null;
                }

                string testmethod = "";
                string placementfilter = "";
                Id modelid = null;
                Id returnId = null;

                //Lookups
                if (stringvalues[1] != null)
                {
                    fieldNames.Add("Lab_Id");
                    fieldValues.Add(Id.Create(stringvalues[1]).ToString());
                    fieldTypes.Add("Id");
                }
                if (stringvalues[2] != null)
                {
                    fieldNames.Add("Company_Id");
                    fieldValues.Add(Id.Create(stringvalues[2]).ToString());
                    fieldTypes.Add("Id");
                }

                //Start yoa, mode, type, id for heme and text for urine and flow
                if (stringvalues[4] != null)
                {
                    if (type.ToUpper().Trim() == "HEMATOLOGY")
                    {
                        placementfilter = "H";              
                        modelid = GetModelId(sqlConn, stringvalues[4].ToString());
                        if (modelid != null)
                        {
                            fieldNames.Add("Heme_Model_Id");
                            fieldValues.Add(modelid.ToString());
                            fieldTypes.Add("Id");

                            if (stringvalues[6] != null)
                            {
                                fieldNames.Add("Year_of_Acquisition_Heme");
                                fieldValues.Add(stringvalues[6].ToString());
                                fieldTypes.Add("Text");
                            }
                            if (stringvalues[7] != null)
                            {
                                fieldNames.Add("Mode_of_Acquisition_Heme");
                                fieldValues.Add(stringvalues[7].ToString());
                                fieldTypes.Add("Text");
                            }
                        }
                    }   
                

                    if (type.ToUpper().Trim() == "FLOW")
                    {
                        placementfilter = "F";
                        fieldNames.Add("Flow_Manufacturer");
                        fieldValues.Add(stringvalues[5].ToString());
                        fieldTypes.Add("Text");

                        if (stringvalues[6] != null)
                        {
                            fieldNames.Add("Year_of_Acquisition_Flow");
                            fieldValues.Add(stringvalues[6].ToString());
                            fieldTypes.Add("Text");
                        }
                        if (stringvalues[7] != null)
                        {
                            fieldNames.Add("Mode_of_Acquisition_Flow");
                            fieldValues.Add(stringvalues[7].ToString());
                            fieldTypes.Add("Text");
                        }
                    }

                    //start urine here....
                    if (type.ToUpper().Trim() == "URINALYSIS")
                    {
                        //Set the testmethod
                        placementfilter = "U"; 
                        if (stringvalues[10] != null && stringvalues[10] != "")
                        {
                            testmethod = stringvalues[10].ToString();
                            fieldNames.Add("TestMethod");
                            fieldValues.Add(stringvalues[10].ToString());
                            fieldTypes.Add("Text");
                        }

                        if (testmethod.ToUpper() == "STRIP & MICRO")
                        {
                            fieldNames.Add("Strip_Manufacturer");
                            fieldValues.Add(stringvalues[0].ToString());
                            fieldTypes.Add("Text");

                            fieldNames.Add("Sediment_Manufacturer");
                            fieldValues.Add(stringvalues[0].ToString());
                            fieldTypes.Add("Text");

                            if (stringvalues[6] != null)
                            {
                                fieldNames.Add("Year_of_Acquis_Urine_Sediment");
                                fieldValues.Add(stringvalues[6].ToString());
                                fieldTypes.Add("Text");
                                fieldNames.Add("Year_of_Acquis_Urine_Strip");
                                fieldValues.Add(stringvalues[6].ToString());
                                fieldTypes.Add("Text");
                            }
                            if (stringvalues[7] != null)
                            {
                                fieldNames.Add("Mode_of_Acquis_Urine_Sediment");
                                fieldValues.Add(stringvalues[7].ToString());
                                fieldTypes.Add("Text");
                                fieldNames.Add("Mode_of_Acquis_Urine_Strip");
                                fieldValues.Add(stringvalues[7].ToString());
                                fieldTypes.Add("Text");
                            }
                        }
                        if (testmethod.ToUpper() == "STRIP ONLY")
                        {
                            fieldNames.Add("Strip_Manufacturer");
                            fieldValues.Add(stringvalues[0].ToString());
                            fieldTypes.Add("Text");  

                            //fieldNames.Add("Strip_Manufacturer");
                            //fieldValues.Add(stringvalues[5].ToString());
                            //fieldTypes.Add("Text");

                            if (stringvalues[6] != null)
                            {
                                fieldNames.Add("Year_of_Acquis_Urine_Strip");
                                fieldValues.Add(stringvalues[6].ToString());
                                fieldTypes.Add("Text");
                            }
                            if (stringvalues[7] != null)
                            {
                                fieldNames.Add("Mode_of_Acquis_Urine_Strip");
                                fieldValues.Add(stringvalues[7].ToString());
                                fieldTypes.Add("Text");
                            }
                        }
                        if (testmethod.ToUpper() == "MICRO")
                        {
                            fieldNames.Add("Sediment_Manufacturer");
                            fieldValues.Add(stringvalues[0].ToString());
                            fieldTypes.Add("Text");

                            //fieldNames.Add("Sediment_Manufacturer");
                            //fieldValues.Add(stringvalues[5].ToString());
                            //fieldTypes.Add("Text");

                            if (stringvalues[6] != null)
                            {
                                fieldNames.Add("Year_of_Acquis_Urine_Sediment");
                                fieldValues.Add(stringvalues[6].ToString());
                                fieldTypes.Add("Text");
                            }
                            if (stringvalues[7] != null)
                            {
                                fieldNames.Add("Mode_of_Acquis_Urine_Sediment");
                                fieldValues.Add(stringvalues[7].ToString());
                                fieldTypes.Add("Text");
                            }
                        }
                    }
                }

                if (type.ToUpper().Trim() == "CHEMISTRY/IA")
                {
                    placementfilter = "C";
                }
                if (type.ToUpper().Trim() == "COAGULATION")
                {
                    placementfilter = "A";
                }
                if (type.ToUpper().Trim() == "ESR")
                {
                    placementfilter = "E";
                }

                //product line
                if (placementfilter != "")
                {
                    fieldNames.Add("Placement_Filter");
                    fieldValues.Add(placementfilter);
                    fieldTypes.Add("Text");
                }
                
                if (stringvalues[8] != null)
                {
                    fieldNames.Add("Flow_Area_Focus");
                    fieldValues.Add(stringvalues[8].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[9] != null)
                {
                    fieldNames.Add("Flow_Reagent_Volume");
                    fieldValues.Add(stringvalues[9].ToString());
                    fieldTypes.Add("Text");
                }

                //if (stringvalues[11] != null)
                //{
                //    fieldNames.Add("Days_of_Operation");
                //    fieldValues.Add(stringvalues[11].ToString());
                //    fieldTypes.Add("Text");
                //}

                if (testmethod != "")
                {
                    fieldNames.Add("TestMethod");
                    fieldValues.Add(testmethod.ToString());
                    fieldTypes.Add("Text");
                }

                if (stringvalues[5] != null)
                {
                    fieldNames.Add("Import_Manufacturer");
                    fieldValues.Add(stringvalues[5].ToString());
                    fieldTypes.Add("Text");
                }
                if (stringvalues[5] != null)
                {
                    fieldNames.Add("Import_CRM");
                    fieldValues.Add(stringvalues[0].ToString());
                    fieldTypes.Add("Text");
                }

                if (instrumentid == null || instrumentid == "")
                {
                    fieldNames.Add("Inactive");
                    fieldValues.Add("false");
                    fieldTypes.Add("Boolean");
                }
                
                fn = new StringBuilder();
                foreach (var fieldName in fieldNames)
                {
                    fn.Append(fieldName);
                    fn.Append("|");
                }

                ft = new StringBuilder();
                foreach (var fieldName in fieldTypes)
                {
                    ft.Append(fieldName);
                    ft.Append("|");
                }

                fv = new StringBuilder();
                foreach (var fieldName in fieldValues)
                {
                    fv.Append(fieldName);
                    fv.Append("|");
                }

                if (instrumentid == null || instrumentid == "")
                {
                    returnId = daab.AddRecord("ctPlacement", fn.ToString().TrimEnd('|'),
                                                    fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), "",
                                                    "ctPlacement_Id");
                }
                else
                {
                    daab.UpdateRecord("ctPlacement", fn.ToString().TrimEnd('|'),
                                                    fv.ToString().TrimEnd('|'), ft.ToString().TrimEnd('|'), instrumentid.ToString(),
                                                    "ctPlacement_Id");

                    returnId = Id.Create(instrumentid);
                }

                ft = null;
                fv = null;
                fn = null;
                return returnId;

            }
            catch (Exception exc)
            {
                ft = null;
                fv = null;
                fn = null;
                return null;
            }
        }

        //Get dataTable from PIVotal
        /// <summary>
        /// Send a select statement and it returns a datatable
        /// <remarks>
        /// /// </remarks>
        private static bool SendEmail(string to, string subject, string body, string bccemail)
        {
            try
            {
                //Setup Email connection
                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                mail.From = new System.Net.Mail.MailAddress("merrillc@sysmex.com");
                //mail.From = new System.Net.Mail.MailAddress("testprac@sysmex.com");

                string[] emailsto = to.Split(';');

                int count = 0;
                foreach (string s in emailsto)
                {
                    mail.To.Add(s);
                    count++;
                }

                if (bccemail != "")
                {
                    string[] emailsbcc = bccemail.Split(';');

                    foreach (string bc in emailsbcc)
                    {
                        mail.Bcc.Add(bc);
                        count++;
                    }
                }

                //if (bccemail != "")
                //{
                //    mail.Bcc.Add("martinh@sysmex.com");
                //    mail.Bcc.Add("kleinj@sysmex.com");
                //}

                string messageBody = "";
                mail.IsBodyHtml = false;
                //send email when done
                messageBody += Environment.NewLine;
                messageBody += Environment.NewLine;
                mail.Body = body;
                mail.Subject = subject;
                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("owa.sysmex.com");
                smtp.Send(mail);

                mail.Bcc.Clear();
                mail.To.Clear();

                return true;

            }
            catch (Exception exc)
            {
                string kk = "";
                return false;
            }
        }

        //Get a crm entity ref record
        private static Entity GetCRMId(Guid crmid, string entityname, string idname, IOrganizationService orgService)
        {
            QueryExpression queryaccountsingleExp = new QueryExpression(entityname);
            queryaccountsingleExp.ColumnSet = new ColumnSet(true);
            queryaccountsingleExp.Criteria.Conditions.Add(new ConditionExpression(idname, ConditionOperator.Equal, crmid));

            EntityCollection singlegetresults = orgService.RetrieveMultipleAll(queryaccountsingleExp);

            if (singlegetresults.Entities.Count == 1)
            {
                foreach (Entity entityrecord in singlegetresults.Entities)
                {
                    return entityrecord;
                }
            }

            return null;           
        }

        //Get a crm entity ref record
        private static bool WriteRecordToCRM(Guid crmid, string entityname, string idname, string MSID, IOrganizationService orgService)
        {
            QueryExpression queryrecordsingleExp = new QueryExpression(entityname);
            queryrecordsingleExp.ColumnSet = new ColumnSet(true);
            queryrecordsingleExp.Criteria.Conditions.Add(new ConditionExpression(idname, ConditionOperator.Equal, crmid));

            EntityCollection singlegetresults = orgService.RetrieveMultipleAll(queryrecordsingleExp);

	        foreach(Entity record in singlegetresults.Entities)
	        {
		        Entity updateRecord = new Entity(entityname);
		        updateRecord.Id = crmid;
		
                updateRecord["smx_migrationsourceid"] =  MSID.ToString();
                //updateRecord["smx_testmethod"] = MSID.ToString();///add testmethod here  ##
		
		        orgService.Update(updateRecord);
	        }
            //<condition attribute='smx_labid' operator='eq' uiname='1007 Tranquility Branch Clinic' uitype='smx_lab' value='{DCE1752E-2EDD-E611-80FD-5065F38B5281}' />

            return true;
        }

        //Get model from type and name of model
        private static Id GetModelId(SqlConnection sqlConn, string modelname)
        {
            Id modelid = null;

            if (modelname.Contains("Unknown"))
            {
            }
            string sqlCommand = "SELECT * FROM ctModel where Model_Description = '" + modelname.Trim() + "'";
            DataTable systemdt = GetDataSQL(sqlCommand, sqlConn);
            if (systemdt.Rows.Count == 1)
            {
                modelid = Id.Create(systemdt.Rows[0]["ctModel_Id"]);
            }
            else
            {
                if (modelname.Contains("Unknown") && systemdt.Rows.Count > 0)
                {
                    modelid = Id.Create(systemdt.Rows[0]["ctModel_Id"]);
                }
            }
            
            return modelid;
        }

        public static void LogMessage(string filename, string strText)
        {
            try
            {
                string filePath = "";
                string strDOY = Convert.ToString(DateTime.Now.DayOfYear);
                string strYear = Convert.ToString(DateTime.Now.Year);
                filePath = filename + "\\CRMIntegrationtoPiv_" + strYear + strDOY + ".txt";

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.AppendAllText(filePath, DateTime.Now + "," + strText + "," + "\r");
                }
                else
                {
                    System.IO.File.AppendAllText(filePath, DateTime.Now + "," + strText + "," + "\r");
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        /// <summary>
        /// Force end the program from anywhere
        /// </summary>
        private static void EndProgram()
        {
            // if not running from CMD (e.g. running from VisualStudio), we want to ReadLine to make sure the console doesn't immediately close
            //Console.ReadLine(); 
            Environment.Exit(0);
        }
    }
}
