using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using SonomaPartners.Crm.Toolkit;
using System.Reflection;
using Microsoft.Xrm.Sdk.Messages;
using System.Collections.Generic;
using System;

namespace Sysmex.Crm.Plugins
{
	class SyncLabAccountAddressLogic
	{
		private const int SHIP_TO = 180700001;
		private const int SOLD_TO = 180700002;


		private IOrganizationService _orgService;
		private ITracingService _tracer;

		public SyncLabAccountAddressLogic(IOrganizationService orgService, ITracingService tracer)
		{
			_orgService = orgService;
			_tracer = tracer;
		}

		public void CreateAddressRecord(Entity target, string businessUnitName)
		{
			if (target.LogicalName.ToLower() == "account")
			{
				Trace("Create new address record from account.");
				Entity address = new Entity("smx_address");
				//Added by yash on 16-09-2020 - 58125
				//address["smx_name"] = target.GetAttributeValue<string>("name") + " Address";
				address["smx_name"] = target.GetAttributeValue<string>("name");
				//End
				address["smx_account"] = new EntityReference("account", target.Id);
				address["smx_type"] = new OptionSetValue(SOLD_TO);
				address["smx_addressstreet1"] = target.GetAttributeValue<string>("address1_line1");
				address["smx_addressstreet2"] = target.GetAttributeValue<string>("address1_line2");
				address["smx_city"] = target.GetAttributeValue<string>("address1_city");
				address["smx_zippostalcode"] = target.GetAttributeValue<string>("address1_postalcode");
				address["smx_statesap"] = target.GetAttributeValue<EntityReference>("smx_stateprovincesap") ??
					RetrieveReferenceByName(target.GetAttributeValue<string>("address1_stateorprovince"), "smx_state", "smx_name");
				address["smx_countrysap"] = target.GetAttributeValue<EntityReference>("smx_countrysap") ??
					RetrieveReferenceByName(target.GetAttributeValue<string>("address1_country"), "smx_country", "smx_name");

				var addressId = _orgService.Create(address);
				Trace("Address Created: " + addressId.ToString());
				//Added by Yash on 01-12-2020 - 59225
				//string zipCode = target.GetAttributeValue<string>("address1_postalcode"); 
		        string zipCode = target.GetAttributeValue<string>("smx_shiptozip");
				Entity territoryDetails = new Entity();
				EntityReference territory = new EntityReference();
				EntityReference regionalmanager = new EntityReference();
				//Added by Yash on 06-10-2020 - 58322
				//Customer-180700000/Prospect-180700002/Tradeshow-180700003
				OptionSetValue accountType = target.Contains("smx_accounttype") ? target.GetAttributeValue<OptionSetValue>("smx_accounttype") : null;
				if (!string.IsNullOrEmpty(zipCode) && businessUnitName != "Latin America" && accountType != null && (accountType.Value == 180700000 || accountType.Value == 180700002 || accountType.Value == 180700003))
				{
					territoryDetails = GetTerritoryDetails(_orgService, zipCode);
					if (territoryDetails != null && territoryDetails.Id != null && territoryDetails.Id != Guid.Empty)
					{
						territory = new EntityReference("territory", territoryDetails.Id);
						regionalmanager = territoryDetails.Contains("smx_accountmanager") ? territoryDetails.GetAttributeValue<EntityReference>("smx_accountmanager") : null;
						if (VerifyUserRoles(regionalmanager.Id))
						{
							target["ownerid"] = regionalmanager;
						}
						else
						{
							Trace("regional manager is not Verified");
						}
						_tracer.Trace("Account Type Is :" + accountType.Value);
						target["territoryid"] = territory;
						//Added by Yash on 03-02-2021 - 60081
						var zipPostal = RetrieveAddressZipCodeRecord(zipCode);
						var accountManager = zipPostal.Contains("territory.smx_accountmanager") ? zipPostal.GetAliasedAttributeValue<EntityReference>("territory.smx_accountmanager") : null;
						target["smx_flowterritory"] = zipPostal.Contains("smx_flowterritory") ? zipPostal.GetAttributeValue<EntityReference>("smx_flowterritory") : null;
						target["smx_fcam"] = accountManager;
						_tracer.Trace("Updated flowTerritory and FlowCytometryAccountManager");
						//End
					}
				}
				//End
				target["smx_address"] = new EntityReference("smx_address", addressId);
				//Added by Yash on 23-07-2020 - 57410
				Entity zipPostalCode = new Entity();
				EntityReference lscUser = new EntityReference();
				if (!string.IsNullOrEmpty(zipCode))
				{
					zipPostalCode = GetZipPostalCode(_orgService, zipCode);
					lscUser = zipPostalCode.Contains("smx_lscuser") ? zipPostalCode.GetAttributeValue<EntityReference>("smx_lscuser") : null;
				    target["smx_lscuser"] = lscUser;
				}
				//End
				_orgService.Update(target);
				Trace("Account updated with address association");
			}
			else if (target.LogicalName.ToLower() == "smx_lab")
			{
				Trace("Create new address record from lab.");
				Entity address = new Entity("smx_address");
				// address["smx_name"] = target.GetAttributeValue<string>("smx_name") + " Address";

				//Added by yash on 24-06-2020 - 57043
				Entity _accountName = _orgService.Retrieve("account", target.GetAttributeValue<EntityReference>("smx_account").Id, new ColumnSet("name"));
				//Added by yash on 29-09-2020 - 58350
				string labTypeText = target.Contains("smx_labtype") ? target.FormattedValues["smx_labtype"].ToString() : string.Empty;
				//address["smx_name"] = _accountName.GetAttributeValue<string>("name");
				//Added by yash on 08-02-2021 - 60296
				//address["smx_name"] = _accountName.GetAttributeValue<string>("name") + "-" + labTypeText;
				address["smx_name"] = _accountName.GetAttributeValue<string>("name");
				address["smx_account"] = target.GetAttributeValue<EntityReference>("smx_account");
				address["smx_lab"] = new EntityReference("smx_lab", target.Id);
				address["smx_type"] = new OptionSetValue(SHIP_TO);
				address["smx_addressstreet1"] = target.GetAttributeValue<string>("smx_street1");
				address["smx_addressstreet2"] = target.GetAttributeValue<string>("smx_street2");
				address["smx_city"] = target.GetAttributeValue<string>("smx_city");
				address["smx_zippostalcode"] = target.GetAttributeValue<string>("smx_zippostalcode");
				address["smx_statesap"] = target.GetAttributeValue<EntityReference>("smx_statesap");
				address["smx_countrysap"] = target.GetAttributeValue<EntityReference>("smx_countrysap");

				string zipCode = target.GetAttributeValue<string>("smx_zippostalcode");
				Entity territoryDetails = new Entity();
				EntityReference territory = new EntityReference();
				EntityReference regionalmanager = new EntityReference();

				if (!string.IsNullOrEmpty(zipCode))
				{
					territoryDetails = GetTerritoryDetails(_orgService, zipCode);
					if (territoryDetails != null && territoryDetails.Id != null && territoryDetails.Id != Guid.Empty)
					{
						territory = new EntityReference("territory", territoryDetails.Id);
						regionalmanager = territoryDetails.Contains("smx_accountmanager") ? territoryDetails.GetAttributeValue<EntityReference>("smx_accountmanager") : null;
						target["smx_regionalmanager"] = regionalmanager;
						target["smx_territory"] = territory;
					}
				}



				var addressId = _orgService.Create(address);
				Trace("Address Created: " + addressId.ToString());


				target["smx_labaddress"] = new EntityReference("smx_address", addressId);
				_orgService.Update(target);
				Trace("lab updated with address association");
			}
			else
			{
				Trace("Unexpected entity type. Exiting");
				return;
			}
		}

		public string getUserBusinessUnit(Guid initiatingUserId)
		{
			Entity _businessUnitEntity = _orgService.Retrieve("systemuser", initiatingUserId, new ColumnSet("businessunitid"));
			return (_businessUnitEntity.GetAttributeValue<EntityReference>("businessunitid").Name);
		}

		public void CopyAddressFields(Entity target, Entity preImage)
		{
			if (target.LogicalName.ToLower() == "account")
			{
				var preAddressRef = preImage?.GetAttributeValue<EntityReference>("smx_address");

				if (preAddressRef != null) // didn't touch address, changed address fields
				{
					Trace("Account address fields changed, updating address.");
					var addressRetrieve = _orgService.Retrieve("smx_address", preAddressRef.Id, new ColumnSet("smx_sapnumber"));
					if (addressRetrieve.Contains("smx_sapnumber") && addressRetrieve.GetAttributeValue<string>("smx_sapnumber") != null)
					{
						Trace("Address has a SAP Id. Exiting.");
						return;
					}

					Entity address = new Entity("smx_address");
					address.Id = preAddressRef.Id;
					var accountAddressMap = new Dictionary<string, string>()
					{
						{ "address1_line1", "smx_addressstreet1" },
						{ "address1_line2", "smx_addressstreet2" },
						{ "address1_city", "smx_city" },
						{ "address1_postalcode", "smx_zippostalcode" },
						{ "smx_stateprovincesap", "smx_statesap" },
						{ "smx_countrysap", "smx_countrysap" }
					}; //key: account
					foreach (var key in accountAddressMap)
					{
						if (target.Contains(key.Key.ToString().ToLower())) //target is account
						{

							if (key.Key.ToString().ToLower() == "smx_stateprovincesap")
							{
								address["smx_statesap"] = target.GetAttributeValue<EntityReference>("smx_stateprovincesap") ??
						RetrieveReferenceByName(target.GetAttributeValue<string>("address1_stateorprovince"), "smx_state", "smx_name");
							}
							else if (key.Key.ToString().ToLower() == "smx_countrysap")
							{
								address["smx_countrysap"] = target.GetAttributeValue<EntityReference>("smx_countrysap") ??
						RetrieveReferenceByName(target.GetAttributeValue<string>("address1_country"), "smx_country", "smx_name");
							}
							else
							{
								address[key.Value] = target.GetAttributeValue<string>(key.Key);
							}
						}
					}
					_orgService.Update(address);
				}
			}
			else if (target.LogicalName.ToLower() == "smx_lab")
			{
				var preAddressRef = preImage?.GetAttributeValue<EntityReference>("smx_labaddress");

				if (preAddressRef != null) // didn't touch address, changed address fields
				{
					Trace("Lab address fields changed, updating address.");
					var addressRetrieve = _orgService.Retrieve("smx_address", preAddressRef.Id, new ColumnSet("smx_sapnumber"));
					if (addressRetrieve.Contains("smx_sapnumber") && addressRetrieve.GetAttributeValue<string>("smx_sapnumber") != null)
					{
						Trace("Address has a SAP Id. Exiting.");
						return;
					}

					Entity address = new Entity("smx_address");
					address.Id = preAddressRef.Id;
					var labAddressMap = new Dictionary<string, string>()
					{
						{ "smx_street1", "smx_addressstreet1" },
						{ "smx_street2", "smx_addressstreet2" },
						{ "smx_city", "smx_city" },
						{ "smx_zippostalcode", "smx_zippostalcode" },
						{ "smx_statesap", "smx_statesap" },
						{ "smx_countrysap", "smx_countrysap" }
					}; //key: lab
					foreach (var key in labAddressMap)
					{
						if (target.Contains(key.Key.ToString().ToLower())) //target is lab
						{

							if (key.Key.ToString().ToLower() == "smx_statesap")
							{
								address["smx_statesap"] = target.GetAttributeValue<EntityReference>("smx_statesap");
							}
							else if (key.Key.ToString().ToLower() == "smx_countrysap")
							{
								address["smx_countrysap"] = target.GetAttributeValue<EntityReference>("smx_countrysap");
							}
							else
							{
								address[key.Value] = target.GetAttributeValue<string>(key.Key);
							}
						}
					}
					_orgService.Update(address);
				}
			}
			Trace("End of logic execution. Exiting...");
		}
		public void SetAddressTypeToShipTo(Entity lab)
		{
			// We want to get the address of the lab that's being updated/created, and then if that address has a lab, set its type field to Ship To
			Trace("Executing SetAddressTypeToShipTo");

			if (!lab.Contains("smx_labaddress") || lab["smx_labaddress"] == null)
			{
				Trace("Lab entity does not contain a reference to a valid address record.");
				return;
			}

			Trace("Retrieving address record.");

			var addressReference = lab.GetAttributeValue<EntityReference>("smx_labaddress");

			if (addressReference == null)
			{
				Trace("Lab is not associated with an address.");
				return;
			}

			var address = _orgService.Retrieve(
					addressReference.LogicalName,
					addressReference.Id,
					new ColumnSet("smx_lab"));

			var addressLab = address.GetAttributeValue<EntityReference>("smx_lab");
			if (addressLab == null)
			{
				Trace("Address does not have an address value - not setting the type to Ship To");
				return;
			}

			Trace("Updating the type field on the address.");

			var addressUpdate = new Entity("smx_address", address.Id);
			addressUpdate["smx_type"] = new OptionSetValue(SHIP_TO);

			_orgService.Update(addressUpdate);
		}

		public void PopulateFieldsBasedOnAddressZipCode(Entity account, string message)
		{
			// We want to get the Zip/Postal code of the address they chose, and then set the account's TIS, LSC, DSM, Territory, AltTerritory, and AltTerritoryManager Id
			Trace("Executing PopulateFieldsBasedOnAddressZipCode");

			// Do not continue if account type is GPO or IHN
			var accountType = _orgService.Retrieve(account.LogicalName, account.Id, new ColumnSet("smx_accounttype"));
			//Added by Yash on 06-10-2020 - 58322
			//int[] CustomerProspect = new[] { 180700000, 180700002 };//Account Type customer and prospect
			int[] CustomerProspect = new[] { 180700000, 180700002, 180700003 };//Account Type customer and prospect and Tradeshow
			if (accountType.Attributes.Contains("smx_accounttype") &&
				!CustomerProspect.Contains(accountType.GetAttributeValue<OptionSetValue>("smx_accounttype").Value))
			{
				_tracer.Trace("Account is not a customer or prospect or Tradeshow, exiting PopulateFieldsBasedOnAddressZipCode");
				return;
			}
			// Added by Yash on 01 - 12 - 2020 - 59225
			//string accountZip = account.GetAttributeValue<string>("address1_postalcode");
			string accountZip = account.GetAttributeValue<string>("smx_shiptozip");
			EntityReference accountAddressCountry = new EntityReference();

			if (!account.Contains("smx_countrysap"))
			{
				Trace("Retrieving address record.");
				var addressCountry = _orgService.Retrieve(account.LogicalName, account.Id, new ColumnSet("smx_countrysap"));
				accountAddressCountry = addressCountry.GetAttributeValue<EntityReference>("smx_countrysap");
			}
			else
			{
				accountAddressCountry = account.GetAttributeValue<EntityReference>("smx_countrysap");
			}

			//Check if country lookup is United State of America or Canada, if not exit
			//var countryList = new EntityReferenceCollection(){
			//    new EntityReference("smx_country", new Guid("509252F6-E2E1-E711-812F-E0071B6A3101")), //USA
			//    new EntityReference("smx_country", new Guid("D89052F6-E2E1-E711-812F-E0071B6A3101"))  //Canda
			//};

			var countryList = GetUSAndCanadaCountries(_orgService);
			if (accountAddressCountry == null || !countryList.Contains(accountAddressCountry))
			{
				Trace("Country lookup is not United State of America or Canada. EXIT");
				account["smx_tis"] = null;
				account["smx_lsc"] = null;
				account["smx_dsm"] = null;
				account["territoryid"] = null;
				account["smx_altterritory"] = null;
				account["smx_altterritorymanager"] = null;
				account["smx_region"] = null;
				//account["smx_lscuser"] = null;
				return;
			}

			var zipPostal = RetrieveAddressZipCodeRecord(accountZip);
			if (zipPostal == null)
			{
				Trace("smx_zippostalcode record not found for the specified zip/postal code.");
				return;
			}

			account["smx_tis"] = zipPostal.GetAttributeValue<EntityReference>("smx_tis");
			account["smx_lsc"] = zipPostal.GetAttributeValue<EntityReference>("smx_lsc");
			account["smx_dsm"] = zipPostal.GetAttributeValue<EntityReference>("smx_dsm");
			account["territoryid"] = zipPostal.GetAttributeValue<EntityReference>("smx_territory");
			account["smx_altterritory"] = zipPostal.GetAttributeValue<EntityReference>("smx_distributorzone");
			account["smx_altterritorymanager"] = zipPostal.GetAliasedAttributeValue<EntityReference>("distributorzone.smx_accountmanager");
			account["smx_region"] = zipPostal.GetAliasedAttributeValue<EntityReference>("territory.smx_region");
			//Added by Yash on 03-02-2021 - 60081
			var accountManager = zipPostal.Contains("territory.smx_accountmanager") ? zipPostal.GetAliasedAttributeValue<EntityReference>("territory.smx_accountmanager") : null;
			account["smx_flowterritory"] = zipPostal.Contains("smx_flowterritory") ? zipPostal.GetAttributeValue<EntityReference>("smx_flowterritory") : null;
			account["smx_fcam"] = accountManager;
			//End
			//account["smx_lscuser"] = zipPostal.Contains("smx_lscuser") ? zipPostal.GetAttributeValue<EntityReference>("smx_lscuser") : null;

			if (accountManager != null && VerifyUserRoles(accountManager.Id))
			{
				Trace($"Changing Account Owner");
				account["ownerid"] = accountManager;
			}
			
			//Adding Update for create since it runs on postOperation for another reason
			if (message == "create")
			{
				_orgService.Update(account);
			}
		}

		public void UpdateAssociatedAccountsAndLabs(Entity address, string businessUnitName)
		{
			// When an address gets updated, we need to make sure to update all accounts and labs that are referencing the address (smx_address field).
			// Both of these record types have their own address fields, which will just be a copy of the fields from the updated smx_address record themselves.
			Trace("Executing UpdateAssociatedAccountAndLab");

			var accounts = RetrieveAccountsAssociatedToAddress(address);
			var labs = RetrieveLabsAssociatedToAddress(address);

			Trace("Updating {0} accounts.", accounts.Count());
			if (accounts.Any())
			{

				foreach (var account in accounts)
				{

					var updatedAccount = new Entity(account.LogicalName, account.Id);
					updatedAccount["smx_address"] = address.ToEntityReference();
					updatedAccount["address1_line1"] = address.GetAttributeValue<string>("smx_addressstreet1");
					updatedAccount["address1_line2"] = address.GetAttributeValue<string>("smx_addressstreet2");
					updatedAccount["address1_city"] = address.GetAttributeValue<string>("smx_city");
					//updatedAccount["address1_stateorprovince"] = address.GetAttributeValue<EntityReference>("smx_statesap")?.Name;
					updatedAccount["address1_postalcode"] = address.GetAttributeValue<string>("smx_zippostalcode");
					//updatedAccount["address1_country"] = address.GetAttributeValue<EntityReference>("smx_countrysap")?.Name;
					updatedAccount["smx_stateprovincesap"] = address.GetAttributeValue<EntityReference>("smx_statesap");
					updatedAccount["smx_countrysap"] = address.GetAttributeValue<EntityReference>("smx_countrysap");

					string zipCode = address.GetAttributeValue<string>("smx_zippostalcode");

					//Added by Yash on 23-07-2020 - 57410
					//Added by Yash on 01-12-2020 - 59225
					//if (!string.IsNullOrEmpty(zipCode))
					//{
					//	Entity zipPostalCode = new Entity();
					//	EntityReference lscUser = new EntityReference();
					//	zipPostalCode = GetZipPostalCode(_orgService, zipCode);
					//	lscUser = zipPostalCode.Contains("smx_lscuser") ? zipPostalCode.GetAttributeValue<EntityReference>("smx_lscuser") : null;
					//	updatedAccount["smx_lscuser"] = lscUser;
					//}
					//End

					Entity territoryDetails = new Entity();
					EntityReference territory = new EntityReference();
					EntityReference regionalmanager = new EntityReference();

					int _accountType = account.GetAttributeValue<OptionSetValue>("smx_accounttype").Value;

					//Added by Yash on 19-06-2020  Customer = 180700000  Prospect = 180700002
					//Added by Yash on 01-12-2020 - 59225
					//if ((!string.IsNullOrEmpty(zipCode)) && businessUnitName != "Latin America" && (_accountType == 180700000 || _accountType == 180700002))
					//{
					//	territoryDetails = GetTerritoryDetails(_orgService, zipCode);
					//	if (territoryDetails != null && territoryDetails.Id != null && territoryDetails.Id != Guid.Empty)
					//	{
					//		territory = new EntityReference("territory", territoryDetails.Id);
					//		regionalmanager = territoryDetails.Contains("smx_accountmanager") ? territoryDetails.GetAttributeValue<EntityReference>("smx_accountmanager") : null;
					//		if (VerifyUserRoles(regionalmanager.Id))
					//		{
					//			updatedAccount["ownerid"] = regionalmanager;
					//		}
					//		else
					//		{
					//			Trace("regional manager is not Verified User Roles");
					//		}
					//		updatedAccount["territoryid"] = territory;
					//	}
					//}
					var updateRequest = new UpdateRequest()
					{
						Target = updatedAccount
					};

					_orgService.Execute(updateRequest);
				}
			}

			Trace("Updating {0} labs.", labs.Count());
			if (labs.Any())
			{
				foreach (var lab in labs)
				{
					var updatedLab = new Entity(lab.LogicalName, lab.Id);
					updatedLab["smx_labaddress"] = address.ToEntityReference();
					updatedLab["smx_street1"] = address.GetAttributeValue<string>("smx_addressstreet1");
					updatedLab["smx_street2"] = address.GetAttributeValue<string>("smx_addressstreet2");
					updatedLab["smx_city"] = address.GetAttributeValue<string>("smx_city");
					//updatedLab["smx_stateprovinceid"] = address.GetAttributeValue<OptionSetValue>("smx_stateprovinceid");
					updatedLab["smx_statesap"] = address.GetAttributeValue<EntityReference>("smx_statesap");
					updatedLab["smx_zippostalcode"] = address.GetAttributeValue<string>("smx_zippostalcode");
					updatedLab["smx_countrysap"] = address.GetAttributeValue<EntityReference>("smx_countrysap");

					string zipCode = address.GetAttributeValue<string>("smx_zippostalcode");
					Entity territoryDetails = new Entity();
					EntityReference territory = new EntityReference();
					EntityReference regionalmanager = new EntityReference();

					if (!string.IsNullOrEmpty(zipCode))
					{
						territoryDetails = GetTerritoryDetails(_orgService, zipCode);
						if (territoryDetails != null && territoryDetails.Id != null && territoryDetails.Id != Guid.Empty)
						{
							territory = new EntityReference("territory", territoryDetails.Id);
							regionalmanager = territoryDetails.Contains("smx_accountmanager") ? territoryDetails.GetAttributeValue<EntityReference>("smx_accountmanager") : null;
							updatedLab["smx_regionalmanager"] = regionalmanager;
							updatedLab["smx_territory"] = territory;
						}
					}
					var updateRequest = new UpdateRequest()
					{
						Target = updatedLab
					};
					_orgService.Execute(updateRequest);
				}

			}
		}

		public Boolean VerifyUserRoles(Guid userId)
		{
			var fetch = new FetchExpression($@"
                <fetch>
                    <entity name='systemuserroles'>
                        <all-attributes />
							<filter type='and'>
								<condition attribute='systemuserid' operator='eq' value='{userId}' />
							</filter>
						<link-entity name='role' from='roleid' to='roleid' link-type='inner' alias='aa'>
							<attribute name='name'/>
							<filter type='or'>
								<condition attribute='name' operator='eq' value='Sysmex - Account Manager' />
                                <condition attribute='name' operator='eq' value='System Administrator' />
                                <condition attribute='name' operator='eq' value='System Customizer' />
							    <condition attribute='name' operator='eq' value='Sysmex - Sales Leadership' />
							</filter>
						</link-entity>
                    </entity>
                </fetch>");

			var result = _orgService.RetrieveMultiple(fetch);
			return result.Entities.Any();
		}

		private IEnumerable<Entity> RetrieveAccountsAssociatedToAddress(Entity address)
		{
			Trace("RetrieveAccountsAssociatedToAddress");

			var query = new QueryExpression("account");
			query.Criteria.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
			query.Criteria.Conditions.Add(new ConditionExpression("smx_address", ConditionOperator.Equal, address.Id));
			query.ColumnSet.AddColumns("accountid", "smx_accounttype", "smx_lscuser");

			return _orgService.RetrieveMultiple(query).Entities;
		}

		private IEnumerable<Entity> RetrieveLabsAssociatedToAddress(Entity address)
		{
			Trace("RetrieveLabsAssociatedToAddress");

			var query = new QueryExpression("smx_lab");
			query.Criteria.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
			query.Criteria.Conditions.Add(new ConditionExpression("smx_labaddress", ConditionOperator.Equal, address.Id));
			query.ColumnSet.AddColumns("smx_labid");

			return _orgService.RetrieveMultiple(query).Entities;
		}

		private Entity RetrieveAddressZipCodeRecord(string zipcode)
		{
			//Added by Yash on 06-10-2020 - 58322
			//Added by Yash on 03-02-2021 - 60081
			if (zipcode != null)
			{
				var query = new QueryExpression("smx_zippostalcode");
				query.Criteria.AddCondition(new ConditionExpression("smx_name", ConditionOperator.Equal, zipcode));
				query.ColumnSet.AddColumns("smx_dsm", "smx_lsc", "smx_tis", "smx_territory", "smx_distributorzone", "smx_lscuser", "smx_flowterritory", "smx_fcam");

				// The AltTerritory / AltTerritoryManagerId field are from the DistributorZone field on the Zip/Postal Code
				query.LinkEntities.Add(new LinkEntity("smx_zippostalcode", "territory", "smx_distributorzone", "territoryid", JoinOperator.LeftOuter));
				query.LinkEntities[0].Columns.AddColumn("smx_accountmanager");
				query.LinkEntities[0].EntityAlias = "distributorzone";

				query.LinkEntities.Add(new LinkEntity("smx_zippostalcode", "territory", "smx_territory", "territoryid", JoinOperator.LeftOuter));
				query.LinkEntities[1].Columns.AddColumns("smx_accountmanager", "smx_region");
				query.LinkEntities[1].EntityAlias = "territory";

				return _orgService.RetrieveMultiple(query).Entities.FirstOrDefault();
			}
			else
				return null;
			//End
		}

		private string RetrieveOptionSetLabel(OptionSetValue optionSet, string fieldName)
		{
			Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
			if (optionSet == null)
			{
				return null;
			}

			var metadataService = new MetadataService(_orgService);
			var optionSetLabel = metadataService.GetStringValueFromPicklistInt("smx_address", fieldName, optionSet.Value);
			Trace($"Option Set Label: {optionSetLabel}");

			return optionSetLabel;
		}

		private int RetrieveOptionSetValueByLabel(string optionSetLabel, string fieldName)
		{
			Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
			if (optionSetLabel == null)
			{
				return -1;
			}

			var metadataService = new MetadataService(_orgService);
			var optionSetValue = metadataService.GetIntValueFromPicklistString("smx_address", fieldName, optionSetLabel);
			return optionSetValue;
		}

		private EntityReference RetrieveReferenceByName(string name, string entityLogicalName, string labelAttribute)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(entityLogicalName) || string.IsNullOrEmpty(labelAttribute)) { return null; }

			var fetch = new FetchExpression($@"
                <fetch top='1'>
                    <entity name='{entityLogicalName}'>
                        <attribute name='{labelAttribute}' />
                        <filter>
                            <condition attribute='{labelAttribute}' operator='eq' value='{name}' />
                        </filter>
                    </entity>
                </fetch>");

			return _orgService.RetrieveMultiple(fetch).Entities.FirstOrDefault().ToEntityReference();
		}

		private void Trace(string message, params object[] args)
		{
			if (_tracer != null)
			{
				_tracer.Trace(message, args);
			}
		}
		private Entity GetTerritoryDetails(IOrganizationService service, string zipCode)
		{
			Entity territory = new Entity();
			try
			{
				var qe = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                              <entity name='territory'>
                                <attribute name='name' />
                                <attribute name='territoryid' />
                                <attribute name='smx_accountmanager' />
                                <order attribute='name' descending='false' />
                                <link-entity name='smx_zippostalcode' from='smx_territory' to='territoryid' link-type='inner' alias='ad'>
                                  <filter type='and'>
                                    <condition attribute='smx_name' operator='eq' value='{ zipCode }' />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";

				EntityCollection accountList = service.RetrieveMultiple(new FetchExpression(qe));
				if (accountList.Entities.Count() > 0)
				{
					territory = accountList.Entities.FirstOrDefault();
				}

			}
			catch (Exception)
			{
				return territory;
			}
			return territory;
		}
		////Added by Yash on 23-07-2020 - 57410
		private Entity GetZipPostalCode(IOrganizationService service, string zipCode)
		{
			Entity zipPostalCode = new Entity();
			try
			{
				var qe = $@"<fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='false'>
                               <entity name='smx_zippostalcode'>
                               <attribute name='smx_zippostalcodeid' />
                               <attribute name='smx_name' />
                               <attribute name='smx_lscuser' />
                               <filter type='and'>
                                <condition attribute='smx_name' operator='eq' value='{zipCode}' />
                               </filter>
                               </entity>
                           </fetch>";

				EntityCollection accountList = service.RetrieveMultiple(new FetchExpression(qe));
				if (accountList.Entities.Count() > 0)
				{
					zipPostalCode = accountList.Entities.FirstOrDefault();
				}

			}
			catch (Exception)
			{
				return zipPostalCode;
			}
			return zipPostalCode;
		}
		//End
		public void PopulateTerritoryAndRegionalManager(Entity target, IOrganizationService _orgService,string businessUnitName)
		{
			Entity territoryDetails = new Entity();
			EntityReference territory = new EntityReference();
			EntityReference regionalmanager = new EntityReference();
			//Added by Yash on 23-07-2020 - 57410
			//Added by Yash on 01-12-2020 - 59225
			//if (target.LogicalName.ToLower() == "account" && target.Contains("address1_postalcode"))
			if (target.LogicalName.ToLower() == "account" && target.Contains("smx_shiptozip"))
			{
				string zipCode = target.GetAttributeValue<string>("smx_shiptozip");
				if (!string.IsNullOrEmpty(zipCode))
				{
					Entity zipPostalCode = new Entity();
					EntityReference lscUser = new EntityReference();
					zipPostalCode = GetZipPostalCode(_orgService, zipCode);
					lscUser = zipPostalCode.Contains("smx_lscuser") ? zipPostalCode.GetAttributeValue<EntityReference>("smx_lscuser") : null;
					target["smx_lscuser"] = lscUser;
				}
			}
			//End
			if (target.LogicalName.ToLower() == "account" && target.Contains("smx_zippostalcode") && businessUnitName!= "Latin America")
			{
				string zipCode = target.GetAttributeValue<string>("smx_zippostalcode");
				//Added by Yash on 06-10-2020 - 58322
				//Customer-180700000/Prospect-180700002/Tradeshow-180700003
				Entity enAccount = _orgService.Retrieve(target.LogicalName, target.Id, new ColumnSet("smx_accounttype"));
				OptionSetValue accountType = enAccount.Contains("smx_accounttype") ? enAccount.GetAliasedAttributeValue<OptionSetValue>("smx_accounttype") : null;
				if (!string.IsNullOrEmpty(zipCode) && accountType != null && (accountType.Value == 180700000 || accountType.Value == 180700002 || accountType.Value == 180700003))
				{
					territoryDetails = GetTerritoryDetails(_orgService, zipCode);
					if (territoryDetails != null && territoryDetails.Id != null && territoryDetails.Id != Guid.Empty)
					{
						territory = new EntityReference("territory", territoryDetails.Id);
						regionalmanager = territoryDetails.Contains("smx_accountmanager") ? territoryDetails.GetAttributeValue<EntityReference>("smx_accountmanager") : null;
						if (VerifyUserRoles(regionalmanager.Id))
						{
							target["ownerid"] = regionalmanager;
						}
						else
						{
							_tracer.Trace("reginal manager was not verified");
						}
						_tracer.Trace("Account Type Is :" + accountType.Value);
						target["territoryid"] = territory;
						//Added by Yash on 03-02-2021 - 60081
						var zipPostal = RetrieveAddressZipCodeRecord(zipCode);
						var accountManager = zipPostal.Contains("territory.smx_accountmanager") ? zipPostal.GetAliasedAttributeValue<EntityReference>("territory.smx_accountmanager") : null;
						target["smx_flowterritory"] = zipPostal.Contains("smx_flowterritory") ? zipPostal.GetAttributeValue<EntityReference>("smx_flowterritory") : null;
						target["smx_fcam"] = accountManager;
						_tracer.Trace("Updated flowTerritory and FlowCytometryAccountManager");
						//End

					}
				}
				//End
			}
			else if (target.LogicalName.ToLower() == "smx_lab" && target.Contains("smx_zippostalcode"))
			{
				string zipCode = target.GetAttributeValue<string>("smx_zippostalcode");

				if (!string.IsNullOrEmpty(zipCode))
				{
					territoryDetails = GetTerritoryDetails(_orgService, zipCode);
					if (territoryDetails != null && territoryDetails.Id != null && territoryDetails.Id != Guid.Empty)
					{
						territory = new EntityReference("territory", territoryDetails.Id);
						regionalmanager = territoryDetails.Contains("smx_accountmanager") ? territoryDetails.GetAttributeValue<EntityReference>("smx_accountmanager") : null;
						target["smx_regionalmanager"] = regionalmanager;
						target["smx_territory"] = territory;
						//Added by Yash on 08-02-2021 - 60312
						EntityCollection opportunityLabs = GetRelatedOpportunityLabs(_orgService, target.Id);
						if(opportunityLabs.Entities.Count > 0)
						{
							_tracer.Trace("opportunityLabs Count : " +opportunityLabs.Entities.Count);
							foreach (Entity opportunityLab in opportunityLabs.Entities)
							{
								opportunityLab["smx_territory"] = territory;
								_orgService.Update(opportunityLab);
							}
						}
						//End
					}
				}
			}
		}

		private EntityReferenceCollection GetUSAndCanadaCountries(IOrganizationService service)
		{
			EntityReferenceCollection countriesRefcollection = new EntityReferenceCollection();
			try
			{
				var qe = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='smx_country'>
                                <attribute name='smx_name' />
                                <attribute name='smx_countrycode' />
                                <attribute name='smx_countryid' />
                                <order attribute='smx_name' descending='false' />
                                <filter type='and'>
                                  <filter type='or'>
                                    <condition attribute='smx_name' operator='like' value='%united states of america%' />
                                    <condition attribute='smx_name' operator='like' value='%Canada%' />
                                  </filter>
                                </filter>
                              </entity>
                            </fetch>";

				EntityCollection countrylist = service.RetrieveMultiple(new FetchExpression(qe));
				if (countrylist.Entities.Count() > 0)
				{
					Trace("received countries :" + countrylist.Entities.Count());
					foreach (var country in countrylist.Entities)
					{
						countriesRefcollection.Add(country.ToEntityReference());
					}
				}
			}
			catch (Exception)
			{
				return countriesRefcollection;
			}
			return countriesRefcollection;
		}
		//Added by Yash on 08-02-2021 - 60312
		private EntityCollection GetRelatedOpportunityLabs(IOrganizationService service, Guid labId)
		{
			EntityCollection opportunityLabs = new EntityCollection();
			try
			{
				var qe = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='smx_opportunitylab'>
								<attribute name='smx_opportunitylabid' />
							    <attribute name='smx_name' />
									<filter type='and'>
										 <condition attribute='smx_labid' operator='eq' value='{labId }' />
									 </filter>
							 <link-entity name='opportunity' from='opportunityid' to='smx_opportunityid' link-type='inner' alias='aa'>
								<filter type='and'>
									<condition attribute='statecode' operator='eq' value='0' />
							    </filter>
						    </link-entity>
							 </entity>
                            </fetch>";

				opportunityLabs = service.RetrieveMultiple(new FetchExpression(qe));
				

			}
			catch (Exception)
			{
				return opportunityLabs;
			}
			return opportunityLabs;
		}
	}
}
