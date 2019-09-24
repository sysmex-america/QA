<Query Kind="Program">
  <Reference Relative="..\packages\SonomaPartners.Crm.Toolkit.8.0.10\lib\SonomaPartners.Crm.Toolkit.dll">C:\workspace\Sysmex\sysmexdynamics\packages\SonomaPartners.Crm.Toolkit.8.0.10\lib\SonomaPartners.Crm.Toolkit.dll</Reference>
  <NuGetReference>Microsoft.CrmSdk.CoreAssemblies</NuGetReference>
  <NuGetReference>Microsoft.CrmSdk.Extensions</NuGetReference>
  <Namespace>Microsoft.Xrm.Client</Namespace>
  <Namespace>Microsoft.Xrm.Client.Services</Namespace>
  <Namespace>Microsoft.Xrm.Sdk</Namespace>
  <Namespace>Microsoft.Xrm.Sdk.Query</Namespace>
  <Namespace>SonomaPartners.Crm.Toolkit</Namespace>
</Query>

private IOrganizationService _orgService;
private int failedRecords = 1;

void Main()
{
	string connection = "Url=https://sysmexdev.crm.dynamics.com; Username='anargyrosa@sysmex.com'; Password='36Aldermanities'";
	
	_orgService = BuildOrgService(connection);

	// Country
	var affectedEntitiesCountry = RetrieveAffectedEntitiesCountry();
	BackfillCountryAndStateLookups(affectedEntitiesCountry);

	// State
	var affectedEntities = RetrieveAffectedEntitiesState();
	BackfillStateLookups(affectedEntities);
}

// Initializes the connection service to the organization
private static IOrganizationService BuildOrgService(string connectionString)
{
	CrmConnection connection = CrmConnection.Parse(connectionString);
	var orgService = new OrganizationService(connection);
	return orgService;
}

private List<Entity> RetrieveAffectedEntitiesState()
{
	var addresses = RetrieveEntityRecordSet("smx_address", "smx_stateprovince", "smx_statesap");
	var demoEvals = RetrieveEntityRecordSet("smx_demoeval", "smx_stateprovince", "smx_statesap");
	var GPOs = RetrieveEntityRecordSet("smx_gpo", "smx_stateprovince", "smx_statesap");
	var IHNs = RetrieveEntityRecordSet("smx_ihn", "smx_stateprovince", "smx_statesap");
	var labs = RetrieveEntityRecordSet("smx_lab", "smx_stateprovince", "smx_statesap");
	var zipPostalCodes = RetrieveEntityRecordSet("smx_zippostalcode", "smx_state", "smx_statesap");
	
	var allRecords = new List<Entity>();
	allRecords.AddRange(addresses);
	allRecords.AddRange(demoEvals);
	allRecords.AddRange(GPOs);
	allRecords.AddRange(IHNs);
	allRecords.AddRange(labs);
	allRecords.AddRange(zipPostalCodes);
	
	return allRecords;
}

private List<Entity> RetrieveAffectedEntitiesCountry()
{
	var addresses = RetrieveEntityRecordSet("smx_address", "smx_addresscountry", "smx_countrysap");
	var labs = RetrieveEntityRecordSet("smx_lab", "smx_country", "smx_countrysap");
	var zipPostalCodes = RetrieveEntityRecordSet("smx_zippostalcode", "smx_country", "smx_countrysap");

	var allRecords = new List<Entity>();
	allRecords.AddRange(addresses);
	allRecords.AddRange(labs);
	allRecords.AddRange(zipPostalCodes);

	return allRecords;
}

private List<Entity> RetrieveEntityRecordSet(string entityLogicalName, string entityOldStateField, string entityNewStateField)
{
	int page = 1;
	string pagingCookie = string.Empty;
	bool moreRecords = false;
	List<Entity> records = new List<Entity>();
	do
	{
		var fetchXml = $@"
		<fetch page='{page}' paging-cookie='{pagingCookie}'>
		  	<entity name='{entityLogicalName}'>
				<all-attributes />
				<filter type='and'>
					<condition attribute='{entityOldStateField}' operator='not-null' />
					<condition attribute='{entityNewStateField}' operator='null' />
				</filter>
		  	</entity>
		</fetch>";

		var results = _orgService.RetrieveMultiple(new FetchExpression(fetchXml));
		page++;
		records.AddRange(results.Entities);
		moreRecords = results.MoreRecords;
		pagingCookie = results.PagingCookie.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

	} while (moreRecords == true);
	
	return records;
}

private void BackfillStateLookups(List<Entity> entities)
{
	foreach (var entity in entities)
	{
		string oldStateField = entity.LogicalName == "smx_zippostalcode" ? "smx_state" : "smx_stateprovince";
		var stateString = entity.GetAttributeValue<string>(oldStateField);

		var stateReference = RetrieveStateReference(stateString);
		if (stateReference == null)
		{
			LogFailedBackfill(entity, "State", stateString);
			continue;
		}

		var updateEntity = new Entity(entity.LogicalName)
		{
			Id = entity.Id,
			["smx_statesap"] = stateReference
		};
		
		_orgService.Update(updateEntity);
	}
}

private void BackfillCountryAndStateLookups(List<Entity> entities)
{
	foreach (var entity in entities)
	{
		string oldCountryField = entity.LogicalName == "smx_address" ? "smx_addresscountry" : "smx_country";
		var countryString = entity.GetAttributeValue<string>(oldCountryField);

		string oldStateField = entity.LogicalName == "smx_zippostalcode" ? "smx_state" : "smx_stateprovince";
		var stateString = entity.GetAttributeValue<string>(oldStateField);

		EntityReference countryReference = null;
		EntityReference stateReference = null;
		RetrieveCountryAndStateReferences(countryString, stateString, out countryReference, out stateReference);
		if (countryReference == null)
		{
			LogFailedBackfill(entity, "Country", countryString);
			continue;
		}
		
		var updateEntity = new Entity(entity.LogicalName)
		{
			Id = entity.Id,
			["smx_countrysap"] = countryReference,
		};
		
		var statesap = entity.GetAttributeValue<EntityReference>("smx_statesap");
		if (entity.GetAttributeValue<EntityReference>("smx_statesap") == null && stateReference != null)
		{
			updateEntity["smx_statesap"] = stateReference;
		}

		_orgService.Update(updateEntity);
	}
}

private void LogFailedBackfill(Entity entity, string type, string label)
{
	Console.WriteLine($"{failedRecords}. Entity {entity.LogicalName} failed. Logging Information\nName: {RetrieveEntityName(entity)}\nID: {entity.Id}\nOld {type} Label: {label}\n");
	
	failedRecords++;
}

private string RetrieveEntityName(Entity entity)
{
	switch (entity.LogicalName)
	{
		case "smx_address":
			return entity.GetAttributeValue<string>("smx_name");
		case "smx_demoeval":
			return entity.GetAttributeValue<string>("smx_demoeval");
		case "smx_gpo":
			return entity.GetAttributeValue<string>("smx_name");
		case "smx_ihn":
			return entity.GetAttributeValue<string>("smx_name");
		case "smx_lab":
			return entity.GetAttributeValue<string>("smx_name");
		case "smx_zippostalcode":
			return entity.GetAttributeValue<string>("smx_name");
	}
	
	return null;
}

private void RetrieveCountryAndStateReferences(string countryString, string stateString, out EntityReference countryReference, out EntityReference stateReference)
{
	countryReference = null;
	stateReference = null;
	
	if (string.IsNullOrEmpty(countryString)) { return; }
	
	if (countryString.ToLower() == "usa" || countryString.ToLower() == "united states")
	{
		countryString = "US";
	}
	else if (countryString.ToLower() == "can" || countryString.ToLower() == "canada")
	{
		countryString = "CA";
	}
	else if (countryString.ToLower() == "brazil" || countryString.ToLower() == "brasil")
	{
		countryString = "BR";
	}
	else if (countryString.ToLower() == "colombia" || countryString.ToLower() == "colômbia")
	{
		countryString = "CO";
	}
	else if (countryString.ToLower() == "mexico" || countryString.ToLower() == "méxico")
	{
		countryString = "MX";
	}
	else if (countryString.ToLower() == "chile")
	{
		countryString = "CL";
	}

	var fetch = new FetchExpression($@"
		<fetch top='1'>
			<entity name='smx_country'>
				<attribute name='smx_name' />
				<attribute name='smx_countrycode' />
				<attribute name='smx_countryid' />
				<filter type='and'>
					<filter type='or'>
	                    <condition attribute='smx_name' operator='eq' value='{countryString}' />
						<condition attribute='smx_countrycode' operator='eq' value='{countryString}' />
	                </filter>
					<condition attribute='statecode' operator='eq' value='0' />
				</filter>
				{(!string.IsNullOrEmpty(stateString) ?
					$@"<link-entity name='smx_state' from='smx_country' to='smx_countryid' alias='state' link-type='outer'>
			            <attribute name='smx_name' />
			            <attribute name='smx_stateid' />
			            <filter type='and'>
			                <condition attribute='statecode' operator='eq' value='0' />
			                <filter type='or'>
			                    <condition attribute='smx_name' operator='eq' value='{stateString}' />
			                    <condition attribute='smx_region' operator='eq' value='{stateString}' />
			                </filter>
			            </filter>
			        </link-entity>" : ""
				)}

			</entity>
		</fetch>");

	var country = _orgService.RetrieveMultiple(fetch).Entities.FirstOrDefault();
	if (country == null) { return; }
	
	countryReference = country.ToEntityReference();
	if (country.Contains("state.smx_stateid"))
	{
		stateReference = new EntityReference{
			Id = country.GetAliasedAttributeValue<Guid>("state.smx_stateid"),
			Name = country.GetAliasedAttributeValue<string>("state.smx_name"),
			LogicalName = "smx_state"
		};
	}
}

private EntityReference RetrieveStateReference(string stateString)
{
	if (string.IsNullOrEmpty(stateString)) { return null; }
	
	var fetch = new FetchExpression($@"
		<fetch>
			<entity name='smx_state'>
				<attribute name='smx_name' />
				<attribute name='smx_region' />
				<attribute name='smx_stateid' />
				<filter type='and'>
					<condition attribute='statecode' operator='eq' value='0' />
					<filter type='or'>
						<condition attribute='smx_name' operator='eq' value='{stateString}' />
						<condition attribute='smx_region' operator='eq' value='{stateString}' />
					</filter>
				</filter>
				<link-entity name='smx_country' from='smx_countryid' to='smx_country' alias='country' link-type='outer'>
		            <attribute name='smx_countrycode' />
		        </link-entity>
			</entity>
		</fetch>");

	var states = _orgService.RetrieveMultiple(fetch).Entities;

	// Try getting state by name first
	var state = states.FirstOrDefault(s => s.GetAttributeValue<string>("smx_name") == stateString)?.ToEntityReference();
	if (state != null) { return state; }
	
	// Try getting state by code and from United States states next
	state = states.FirstOrDefault(s => s.GetAliasedAttributeValue<string>("country.smx_countrycode") == "US")?.ToEntityReference();
	if (state != null)  {return state; }

	// Otherwise, just return whichever state we find
	return states.FirstOrDefault()?.ToEntityReference();
}