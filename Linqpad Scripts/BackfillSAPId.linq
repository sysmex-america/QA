<Query Kind="Program">
  <NuGetReference>Microsoft.CrmSdk.XrmTooling.CoreAssembly</NuGetReference>
  <Namespace>Microsoft.Crm.Sdk</Namespace>
  <Namespace>Microsoft.Crm.Sdk.Messages</Namespace>
  <Namespace>Microsoft.Xrm.Sdk</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Client</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Messages</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Metadata</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Organization</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Query</Namespace>
  <Namespace>Microsoft.Xrm.Tooling.Connector</Namespace>
</Query>

// Hit F4 to add local project assemblies and namespaces
// e.g. *.Crm.Plugins, *.Crm.Model, SonomaPartners.Crm.Toolkit, etc.
/* References
Microsoft.Crm.Sdk
Microsoft.Crm.Sdk.Messages
Microsoft.Xrm.Sdk
Microsoft.Xrm.Sdk.Client
Microsoft.Xrm.Sdk.Messages
Microsoft.Xrm.Sdk.Metadata
Microsoft.Xrm.Sdk.Organization
Microsoft.Xrm.Sdk.Query
Microsoft.Xrm.Tooling.Connector
*/

string orgUrl = "https://sysmexdev.crm.dynamics.com"; //replace with your url
string authType = "Office365"; // AD, IFD, OAuth, Office365
string user = "user@example.com";
string password = "examplepw";
string domain = "";
IOrganizationService _orgService;
ITracingService _tracer;

void Main()
{

	_orgService = CreateOrgService();
	_tracer = new TracingSpoof();
	var fetch = new FetchExpression($@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
				  <entity name='smx_address'>
				    <attribute name='smx_addressid' />
				    <attribute name='smx_name' />
				    <attribute name='createdon' />
				    <order attribute='smx_name' descending='false' />
				    <filter type='and'>
				      <condition attribute='smx_sapnumber' operator='null' />
				    </filter>
				    <link-entity name='smx_customermaster' from='smx_addressid' to='smx_addressid' link-type='inner' alias='ab'>
				      <attribute name='smx_sapid' />
				      <filter type='and'>
				        <condition attribute='statuscode' operator='eq' value='2' />
				        <condition attribute='smx_sapid' operator='not-null' />
				      </filter>
				    </link-entity>
				  </entity>
				</fetch>");
	"Starting logic operation.".Dump();
	var result = _orgService.RetrieveMultiple(fetch);
	
	if (result.Entities.Count < 1) {
		return;
	}
	ExecuteMultipleRequest batchRequest = new ExecuteMultipleRequest()
	{
		Settings = new ExecuteMultipleSettings()
		{
			ContinueOnError = false,
			ReturnResponses = true
		},
		Requests = new OrganizationRequestCollection()
	};
	
	foreach (Entity address in result.Entities)
	{
		var record = new Entity("smx_address")
		{
			Id = address.Id,
			["smx_sapnumber"] = address.GetAttributeValue<AliasedValue>("ab.smx_sapid").Value
		};
		"Adding update request".Dump();
		batchRequest.Requests.Add(new UpdateRequest() { Target = record });
	}

	if (!batchRequest.Requests.Any()) {
		"No addresses to update. Exiting.".Dump();
		return;
	}
	
	try
	{
		$"Updated {batchRequest.Requests.Count} Addresses".Dump();
		_orgService.Execute(batchRequest);
	}
	catch (Exception e)
	{
		Console.WriteLine(e);
	}
	
	_tracer.Trace("Exiting Logic.");

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