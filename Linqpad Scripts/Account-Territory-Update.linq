<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.ServiceModel.dll</Reference>
  <NuGetReference>Microsoft.CrmSdk.CoreAssemblies</NuGetReference>
  <NuGetReference>Microsoft.CrmSdk.XrmTooling.CoreAssembly</NuGetReference>
  <NuGetReference>SonomaPartners.Crm.Toolkit</NuGetReference>
  <Namespace>Microsoft.Xrm.Sdk</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Client</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Query</Namespace>
  <Namespace>Microsoft.Xrm.Tooling.Connector</Namespace>
  <Namespace>SonomaPartners.Crm.Toolkit</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Messages</Namespace>
</Query>

void Main()
{
	// Update all account territory data and owner
	var _orgService = new CrmServiceClient("AuthType=Office365;Username=anargyrosa@sysmex.com;Password=Nongovernment67;Url=https://sysmexdev.crm.dynamics.com;RequireNewInstance=true;");

	var fetchXml = @"
	<fetch {0}>
		<entity name='account'>
		<attribute name='smx_address' />
			<filter type='and'>
				<condition attribute='statecode' operator='eq' value='0'/>
				<condition attribute='smx_accounttype' operator='in'>
			        <value>180700000</value>
			        <value>180700002</value>
			      </condition>
			</filter>
		</entity>
	</fetch>";

	var accounts = RetrieveAllRecords(_orgService, fetchXml);
	var accountUpdates = new ExecuteMultipleRequest
	{
		Requests = new OrganizationRequestCollection(),
		Settings = new ExecuteMultipleSettings
		{
			ContinueOnError = true,
			ReturnResponses = false
		}
	};

	foreach (var acc in accounts)
	{
		if (accountUpdates.Requests.Count >= 100)
		{
			try
			{
				_orgService.Execute(accountUpdates);
				("Hit max amount of possible requests in a single ExecuteMultiple for Accounts. Exeucting and clearing.").Dump();
				accountUpdates.Requests.Clear();
			}
			catch
			{
				("Error: Dump and start again").Dump();
				accountUpdates.Requests.Clear();
			}
		}

		var account = new Entity(acc.LogicalName, acc.Id);

		var addressRef = acc.GetAttributeValue<EntityReference>("smx_address");
		if (addressRef == null)
		{
			"Account entity does not contain a reference to a valid address record.".Dump();
			continue;
		}

		var address = _orgService.Retrieve(addressRef.LogicalName, addressRef.Id, new ColumnSet("smx_zippostalcode", "smx_countrysap"));
		if (!address.Contains("smx_zippostalcode"))
		{
			"Address does not contain zip/postal code, so it may not be used to find a distributor zone.".Dump();
			continue;
		}

		// Check for Zipcode
		var zipPostal = RetrieveAddressZipCodeRecord(address, _orgService);
		if (zipPostal == null)
		{
			"smx_zippostalcode record not found for the specified zip/postal code.".Dump();
			continue;
		}

		//If country is not provided exit the plugin
		if (!address.Contains("smx_countrysap"))
		{
			"No Country, Null Teritory".Dump();
			RemoveTeritoryDetails(account);
			continue;
		}

		//Check if country lookup is United State of America or Canada, if not exit
		var countryList = new EntityReferenceCollection(){
				new EntityReference("smx_country", new Guid("509252F6-E2E1-E711-812F-E0071B6A3101")), //USA
                new EntityReference("smx_country", new Guid("D89052F6-E2E1-E711-812F-E0071B6A3101"))  //Canda
            };
		if (!countryList.Contains(address["smx_countrysap"]))
		{
			"Country not USA or Canada".Dump();
			RemoveTeritoryDetails(account);
			continue;
		}

		account["smx_tis"] = zipPostal.GetAttributeValue<EntityReference>("smx_tis");
		account["smx_lsc"] = zipPostal.GetAttributeValue<EntityReference>("smx_lsc");
		account["smx_dsm"] = zipPostal.GetAttributeValue<EntityReference>("smx_dsm");
		account["territoryid"] = zipPostal.GetAttributeValue<EntityReference>("smx_territory");
		account["smx_altterritory"] = zipPostal.GetAttributeValue<EntityReference>("smx_distributorzone");
		account["smx_altterritorymanager"] = zipPostal.GetAliasedAttributeValue<EntityReference>("distributorzone.smx_accountmanager");
		account["smx_region"] = zipPostal.GetAliasedAttributeValue<EntityReference>("territory.smx_region");

		var accountManager = zipPostal.GetAliasedAttributeValue<EntityReference>("territory.smx_accountmanager");
		if (accountManager != null)
		{
			account["ownerid"] = accountManager;
		}

		var updateRequest = new UpdateRequest()
		{
			Target = account
		};

		accountUpdates.Requests.Add(updateRequest);
	}
	_orgService.Execute(accountUpdates);
}

private Entity RetrieveAddressZipCodeRecord(Entity address, CrmServiceClient orgService)
{
	var query = new QueryExpression("smx_zippostalcode");
	query.Criteria.AddCondition(new ConditionExpression("smx_name", ConditionOperator.Equal, address.GetAttributeValue<string>("smx_zippostalcode")));
	query.ColumnSet.AddColumns("smx_dsm", "smx_lsc", "smx_tis", "smx_territory", "smx_distributorzone");

	// The AltTerritory / AltTerritoryManagerId field are from the DistributorZone field on the Zip/Postal Code
	query.LinkEntities.Add(new LinkEntity("smx_zippostalcode", "territory", "smx_distributorzone", "territoryid", JoinOperator.LeftOuter));
	query.LinkEntities[0].Columns.AddColumn("smx_accountmanager");
	query.LinkEntities[0].EntityAlias = "distributorzone";

	query.LinkEntities.Add(new LinkEntity("smx_zippostalcode", "territory", "smx_territory", "territoryid", JoinOperator.LeftOuter));
	query.LinkEntities[1].Columns.AddColumns("smx_accountmanager", "smx_region");
	query.LinkEntities[1].EntityAlias = "territory";

	return orgService.RetrieveMultiple(query).Entities.FirstOrDefault();
}

public static List<Entity> RetrieveAllRecords(IOrganizationService service, string fetch)
{
	//Dont forget to include <fetch {0}>
	var moreRecords = false;
	int page = 1;
	var cookie = string.Empty;
	List<Entity> Entities = new List<Entity>();
	do
	{
		var xml = string.Format(fetch, cookie);
		var collection = service.RetrieveMultiple(new FetchExpression(xml));

		if (collection.Entities.Count >= 0) Entities.AddRange(collection.Entities);

		moreRecords = collection.MoreRecords;
		if (moreRecords)
		{
			page++;
			cookie = string.Format("paging-cookie='{0}' page='{1}'", System.Security.SecurityElement.Escape(collection.PagingCookie), page);
		}
	} while (moreRecords);

	return Entities;
}

public static int GetRecordCount(IOrganizationService service, string fetch)
{
	var result = RetrieveAllRecords(service, fetch);
	return result.Count;
}

private static void RemoveTeritoryDetails(Entity account)
{
	account["smx_tis"] = null;
	account["smx_lsc"] = null;
	account["smx_dsm"] = null;
	account["territoryid"] = null;
	account["smx_altterritory"] = null;
	account["smx_altterritorymanager"] = null;
	account["smx_region"] = null;
}