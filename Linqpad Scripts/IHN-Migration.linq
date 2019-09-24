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

/*
IHN records should all be migrated to Account records with the Account Type of IHN.
Field Mapping document is attached.
Each Account should be created, and have an associated address record.
Once Account records are created, existing accounts should be associated with the newly created records based off of the old IHN and GPO Lookups.
*/

void Main()
{
	var _orgService = new CrmServiceClient("AuthType=Office365;Username=anargyrosa@sysmex.com;Password=Nongovernment67;Url=https://sysmexdev.crm.dynamics.com;RequireNewInstance=true;");
	
	QueryExpression query = new QueryExpression
	{
		EntityName = "smx_ihn",
		ColumnSet = new ColumnSet(true),
		Criteria = new FilterExpression(LogicalOperator.Or)
	};
	query.Criteria.Conditions.Add(new ConditionExpression("smx_processed", ConditionOperator.NotEqual, true));
	
	var IHNs = _orgService.RetrieveMultiple(query);
	var CreatedIHN = 0;
	
	IHNs.Entities.Count.Dump();
	
	foreach (Entity ihn in IHNs.Entities)
	{
		Entity account = new Entity("account");
		account.Attributes.Add("ownerid", ihn.GetAttributeValue<EntityReference>("ownerid"));
		account.Attributes.Add("name", ihn.GetAttributeValue<string>("smx_name"));
		account.Attributes.Add("statuscode", new OptionSetValue(1));
		account.Attributes.Add("smx_gpoihncode", ihn.GetAttributeValue<string>("smx_ihncode"));
		account.Attributes.Add("address1_line1", ihn.GetAttributeValue<string>("smx_addressstreet1"));
		account.Attributes.Add("address1_line2", ihn.GetAttributeValue<string>("smx_addressstreet2"));
		account.Attributes.Add("address1_line3", ihn.GetAttributeValue<string>("smx_addressstreet3"));
		account.Attributes.Add("address1_city", ihn.GetAttributeValue<string>("smx_addresscity"));
		account.Attributes.Add("smx_stateprovincesap", ihn.GetAttributeValue<EntityReference>("smx_statesap"));
		account.Attributes.Add("address1_postalcode", ihn.GetAttributeValue<string>("smx_zippostalcode"));
		account.Attributes.Add("smx_countrysap", ihn.GetAttributeValue<EntityReference>("smx_countrysap"));
		account.Attributes.Add("smx_accounttype", new OptionSetValue(180700005));
		var IHNaccountID = _orgService.Create(account);
		("New IHN Account ID | " + IHNaccountID.ToString()).Dump();

		//Associate all related accounts to new IHN account
		QueryExpression query2 = new QueryExpression
		{
			EntityName = "account",
			ColumnSet = new ColumnSet(true)
		};
		query2.Criteria.Conditions.Add(new ConditionExpression("smx_ihn", ConditionOperator.Equal, ihn.Id));

		var updatedAccounts = 0;
		var accounts = _orgService.RetrieveMultiple(query2);
		("Number related accounts | " + accounts.Entities.Count.ToString()).Dump();
		
		foreach (Entity acc in accounts.Entities)
		{
			try
			{
				acc.Attributes["smx_accountihn"] = new EntityReference("account", IHNaccountID);
				_orgService.Update(acc);
				updatedAccounts++;
			}catch{
				acc.Attributes["name"].Dump();
			}
		}
		("Number of accounts Updated | " + updatedAccounts.ToString()).Dump();
		
		//Delay to let Plugin Catch Up
		int milliseconds = 30000;
		Thread.Sleep(milliseconds);
		
		//Set SAP ID on Address Record
		var IHNAddressLookup = _orgService.Retrieve("account", IHNaccountID, new ColumnSet("smx_address"));
		var IHNAddress = _orgService.Retrieve("smx_address", IHNAddressLookup.GetAttributeValue<EntityReference>("smx_address").Id, new ColumnSet(true));

		var sapID = ihn.GetAttributeValue<string>("smx_sapnumber");
		if (sapID != null)
		{
			IHNAddress.Attributes.Add("smx_sapnumber", sapID);
			_orgService.Update(IHNAddress);
			("SAP ID Updated").Dump();
		}else{
			("NO SAP ID!!!").Dump();
		}
				
		//Set newly created IHN Account to Inactive if original IHN record was Inactive
		if (ihn.GetAttributeValue<OptionSetValue>("statuscode").Value == 0)
		{
			Entity account2 = new Entity("account", IHNaccountID);
			account2.Attributes.Add("statecode", new OptionSetValue(1));
			_orgService.Update(account2);
			("IHN Account set to Inactive").Dump();
		}
		
		var ihnUpdate = new Entity("smx_ihn", ihn.Id);
		ihnUpdate.Attributes.Add("smx_processed",  true);
		_orgService.Update(ihnUpdate);
		
		CreatedIHN++;
	}
	("Created: " + CreatedIHN.ToString() + " Out Of " + IHNs.Entities.Count()).Dump();
}