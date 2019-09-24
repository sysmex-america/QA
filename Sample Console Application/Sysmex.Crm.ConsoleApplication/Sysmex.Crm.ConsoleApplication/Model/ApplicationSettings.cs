namespace Sysmex.Crm.ConsoleApplication.Model
{
    class ApplicationSettings
    {
        // URL for the organization that's being connected to
        public string CrmUrl { get; set; } = "https://sysmexdev.crm.dynamics.com";

        // Username of the person being logged in
        public string Username { get; set; } = "";

        // Password for the person being logged in
        public string Password { get; set; } = "";

        // AuthenticationType that tells the CrmServiceClient what type of org it's connecting to
        // Options are AD (Active Directory), Office365, IFD (Internet-facing Deployment)
        public string AuthenticationType { get; set; } = "Office365";

        // Whether or not we loaded these settings from a JSON file or used the above default values
        public bool IsDefaultSettings { get; set; } = true;
    }
}
