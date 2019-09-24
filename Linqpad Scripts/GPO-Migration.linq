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
GPO records should all be migrated to Account records with the Account Type of GPO.
Field Mapping document is attached.
Each Account should be created, and have an associated address record.
Once Account records are created, existing accounts should be associated with the newly created records based off of the old IHN and GPO Lookups

//TODO: How to Associate Accounts to GPO
//smx_GPO > smx_hemeGPO
//smx_GPOChemistryIA > smx_ChemistryIAGPO
//smx_GPOCoag > smx_CoagGPO
//smx_GPOESR > smx_ESRGPO
//smx_GPOFlow > smx_FlowGPO
//smx_GPOUrinalysis > smx_UrinalysisGPO
//smx_IHN > smx_AccountIHN
//smx_IHNSecondary > smx_aggregationgroup
*/

void Main()
{
	// General API service client
	var _orgService = new CrmServiceClient("AuthType=Office365;Username=anargyrosa@sysmex.com;Password=Nongovernment67;Url=https://sysmexdev.crm.dynamics.com;RequireNewInstance=true;");

	QueryExpression query = new QueryExpression
	{
		EntityName = "smx_gpo",
		ColumnSet = new ColumnSet(true),
		Criteria = new FilterExpression(LogicalOperator.Or)
	};
	query.Criteria.Conditions.Add(new ConditionExpression("smx_processed", ConditionOperator.NotEqual, true));

	var GPOs = _orgService.RetrieveMultiple(query);
	var CreatedGPO = 0;
	
	
	foreach (Entity gpo in GPOs.Entities)
	{
		Entity account = new Entity("account");
		account.Attributes.Add("ownerid", gpo.GetAttributeValue<EntityReference>("ownerid"));
		account.Attributes.Add("statuscode", new OptionSetValue(1));
		account.Attributes.Add("name", gpo.GetAttributeValue<string>("smx_name"));
		account.Attributes.Add("smx_gpoihncode", gpo.GetAttributeValue<string>("smx_gpocode"));
		account.Attributes.Add("address1_line1", gpo.GetAttributeValue<string>("smx_addressstreet1"));
		account.Attributes.Add("address1_line2", gpo.GetAttributeValue<string>("smx_addressstreet2"));
		account.Attributes.Add("address1_line3", gpo.GetAttributeValue<string>("smx_addressstreet3"));
		account.Attributes.Add("address1_city", gpo.GetAttributeValue<string>("smx_addresscity"));
		account.Attributes.Add("smx_stateprovincesap", gpo.GetAttributeValue<EntityReference>("smx_statesap"));
		account.Attributes.Add("address1_postalcode", gpo.GetAttributeValue<string>("smx_zippostalcode"));
		account.Attributes.Add("smx_countrysap", gpo.GetAttributeValue<EntityReference>("smx_countrysap"));
		account.Attributes.Add("smx_accounttype", new OptionSetValue(180700004));
		var GPOaccountID = _orgService.Create(account);
		("New GPO Account ID | " + GPOaccountID.ToString()).Dump();

		//Associate all related accounts to new GPO account
		QueryExpression query2 = new QueryExpression
		{
			EntityName = "account",
			ColumnSet = new ColumnSet(true)
		};
		query2.Criteria.Conditions.Add(new ConditionExpression("smx_gpo", ConditionOperator.Equal, gpo.Id));
//HEME is the only one used right now
//		FilterExpression filter = new FilterExpression(LogicalOperator.Or);
//		filter.AddCondition("smx_gpo", ConditionOperator.Equal, gpo.Id);
//		filter.AddCondition("smx_GPOChemistryIA", ConditionOperator.Equal, gpo.Id);
//		filter.AddCondition("smx_GPOCoag", ConditionOperator.Equal, gpo.Id);
//		filter.AddCondition("smx_GPOESR", ConditionOperator.Equal, gpo.Id );
//		filter.AddCondition("smx_GPOFlow", ConditionOperator.Equal, gpo.Id);
//		filter.AddCondition("smx_GPOUrinalysis", ConditionOperator.Equal, gpo.Id);
//		query2.Criteria = filter;
//8523d2da-3afd-e611-8110-e0071b6a3101
		var updatedAccounts = 0;
		var accounts = _orgService.RetrieveMultiple(query2);
		foreach (Entity acc in accounts.Entities)
		{
			try{
				acc.Attributes["smx_hemegpo"] = new EntityReference("account", GPOaccountID);
				_orgService.Update(acc);
				updatedAccounts++;
			}
			catch
			{
				acc.Attributes["name"].Dump();
			}
		}
		("Number of accounts Updated | " + updatedAccounts.ToString()).Dump();

		//Delay to let Plugin Catch Up
		int milliseconds = 30000;
		Thread.Sleep(milliseconds);

		//Set SAP ID on Address Record
		var GPOAddressLookup = _orgService.Retrieve("account", GPOaccountID, new ColumnSet("smx_address"));
		var GPOAddress = _orgService.Retrieve("smx_address", GPOAddressLookup.GetAttributeValue<EntityReference>("smx_address").Id, new ColumnSet(true));
		var sapID = gpo.GetAttributeValue<string>("smx_sapnumber");
		if (sapID != null)
		{
			GPOAddress.Attributes.Add("smx_sapnumber", sapID);
			_orgService.Update(GPOAddress);
			("SAP ID Updated").Dump();
		}else{
			("NO SAP ID!!!").Dump();
		}
				
		//Set newly created GPO Account to Inactive if original GPO record was Inactive
		if (gpo.GetAttributeValue<OptionSetValue>("statuscode").Value == 0)
		{
			Entity account2 = new Entity("account", GPOaccountID);
			account2.Attributes.Add("statecode", new OptionSetValue(1));
			_orgService.Update(account2);
			("GPO Account set to Inactive").Dump();
		}

		var ihnUpdate = new Entity("smx_gpo", gpo.Id);
		ihnUpdate.Attributes.Add("smx_processed", true);
		_orgService.Update(ihnUpdate);

		CreatedGPO++;
	}
	("Created: " + CreatedGPO.ToString() + " Out Of " + GPOs.Entities.Count()).Dump();
}