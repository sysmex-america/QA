
 namespace Sysmex.Crm.ConsoleApp.Model
{
    class ApplicationSettings
    {
        public string CrmUrl { get; set; }
        public string CrmUsername { get; set; }
        public string CrmPassword { get; set; }
        public string CrmAuthenticationType { get; set; }
        public string SQLDBConnectionString { get; set; }
        public bool UseActiveDirectoryService { get; set; }
    }
}
