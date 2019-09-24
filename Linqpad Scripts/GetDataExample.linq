<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.ServiceModel.dll</Reference>
  <NuGetReference>Microsoft.CrmSdk.CoreAssemblies</NuGetReference>
  <NuGetReference>Microsoft.CrmSdk.XrmTooling.CoreAssembly</NuGetReference>
  <Namespace>Microsoft.Xrm.Sdk</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Client</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Query</Namespace>
  <Namespace>Microsoft.Xrm.Tooling.Connector</Namespace>
</Query>

string orgUrl = "https://sysmexdev.crm.dynamics.com";
string authType = "Office365"; // AD, IFD, OAuth, Office365
string user = "";
string password = "";
string domain = ""; //not required unless using AD
IOrganizationService _orgService;
ITracingService _tracer;

void Main()
{
	_orgService = CreateOrgService();
	_tracer = new TracingSpoof();

	// General API service retrieve
	var result = (_orgService.RetrieveMultiple(new FetchExpression($@"
                <fetch>
                    <entity name='organization'>
						<attribute name='organizationid' />						
					</entity>
                </fetch>")));
	if (result.Entities.Any())
	{
		result.Entities.Dump();
	}
	
	"Done!".Dump();
}
IOrganizationService CreateOrgService()
{
	var connectionString = $"Url={orgUrl};AuthType={authType};RequireNewInstance=true";
	if (!string.IsNullOrEmpty(user))
	{
		connectionString += $";UserName={user}";
	}
	if (!string.IsNullOrEmpty(password))
	{
		connectionString += $";Password={password}";
	}
	if (!string.IsNullOrEmpty(domain))
	{
		connectionString += $";Domain={domain}";
	}
	return new CrmServiceClient(connectionString);
}

class TracingSpoof : ITracingService
{
	public void Trace(string format, params object[] args)
	{
		string.Format(format, args).Dump();
	}
}