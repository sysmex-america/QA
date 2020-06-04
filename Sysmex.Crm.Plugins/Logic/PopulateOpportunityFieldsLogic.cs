using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sysmex.Crm.Plugins
{
    class PopulateOpportunityFieldsLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _tracer;
        private Dictionary<string, string> opportunityMap = new Dictionary<string, string>()
            {
                { "smx_distributor", "smx_partneraccount" },
                { "smx_ihn", "smx_accountihn" },
                { "smx_gpoheme", "smx_hemegpo" },
                { "smx_gpourinalysis", "smx_urinalysisgpo" },
                { "smx_gpoesr", "smx_esrgpo" },
                { "smx_gpocoag", "smx_coaggpo" },
                { "smx_gpoflow", "smx_flowgpo" },
                { "smx_gpochemistryia", "smx_chemistryiagpo" }
            };
        public PopulateOpportunityFieldsLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public void PopulateOpportunityFields(Entity opportunity)
        {
            _tracer.Trace("Entering PopulateOpportunityFields Logic.");
            if (opportunity.Contains("parentaccountid"))
            {
                try
                {
                    _tracer.Trace("Adding Parent Account fields");
                    var accountId = opportunity.GetAttributeValue<EntityReference>("parentaccountid").Id;

                    var account = RetrieveAccountRelatedFields(accountId);
                    Guid primaryLabid = opportunity.Contains("smx_primarysite") ? opportunity.GetAttributeValue<EntityReference>("smx_primarysite").Id : Guid.Empty;
                    var primaryLab = RetrievePrimaryLab(primaryLabid, _tracer);
                    EntityReference accountManagerFef = null;
                    if (primaryLab != null)
                    {
                        if (primaryLab.Contains("smx_regionalmanager"))
                        {
                            _tracer.Trace($"regional manager is :{primaryLab.GetAttributeValue<EntityReference>("smx_regionalmanager").Name}");
                            accountManagerFef = primaryLab.GetAttributeValue<EntityReference>("smx_regionalmanager");
                        }
                        else
                        {
                            _tracer.Trace($"regional manager is not Available for This promary Lab id :{primaryLabid.ToString()}");
                        }
                    }
                    if (account != null)
                    {
                        opportunity["smx_distributor"] = account.Contains("smx_partneraccount") ? account.GetAttributeValue<EntityReference>("smx_partneraccount") : null;
                        opportunity["smx_ihn"] = account.Contains("smx_accountihn") ? account.GetAttributeValue<EntityReference>("smx_accountihn") : null;
                        opportunity["smx_gpoheme"] = account.Contains("smx_hemegpo") ? account.GetAttributeValue<EntityReference>("smx_hemegpo") : null;
                        opportunity["smx_gpourinalysis"] = account.Contains("smx_urinalysisgpo") ? account.GetAttributeValue<EntityReference>("smx_urinalysisgpo") : null;
                        opportunity["smx_gpoesr"] = account.Contains("smx_esrgpo") ? account.GetAttributeValue<EntityReference>("smx_esrgpo") : null;
                        opportunity["smx_gpocoag"] = account.Contains("smx_coaggpo") ? account.GetAttributeValue<EntityReference>("smx_coaggpo") : null;
                        opportunity["smx_gpoflow"] = account.Contains("smx_flowgpo") ? account.GetAttributeValue<EntityReference>("smx_flowgpo") : null;
                        opportunity["smx_gpochemistryia"] = account.Contains("smx_chemistryiagpo") ? account.GetAttributeValue<EntityReference>("smx_chemistryiagpo") : null;
                        opportunity["smx_corporateaccountexecihn"] = account.Contains("smx_ihncae") ? account.GetAttributeValue<EntityReference>("smx_ihncae") : null;
                        opportunity["smx_aggregationgroup"] = account.Contains("smx_aggregationgroup") ? account.GetAttributeValue<EntityReference>("smx_aggregationgroup") : null;
                        opportunity["smx_distributionsalesmanager"] = account.Contains("smx_altterritorymanager") ? account.GetAttributeValue<EntityReference>("smx_altterritorymanager") : null;
                        opportunity["smx_regionalsalesdirector"] = account.Contains("ab.smx_accountmanager") ? account.GetAliasedAttributeValue<EntityReference>("ab.smx_accountmanager") : null;
                        opportunity["smx_areasalesdirector"] = account.Contains("ab.smx_regionalmanager") ? account.GetAliasedAttributeValue<EntityReference>("ab.smx_regionalmanager") : null;
                        opportunity["smx_canadiandirectorofsales"] = account.Contains("ab.smx_accountmanager") ? account.GetAliasedAttributeValue<EntityReference>("ab.smx_regionalmanager") : null;
                        opportunity["smx_directordistributorsales"] = account.Contains("ah.smx_regionalmanager") ? account.GetAliasedAttributeValue<EntityReference>("ah.smx_regionalmanager") : null;
                        opportunity["smx_corporateaccountdirector"] = account.Contains("ac.smx_corporateaccountdirector") ? account.GetAliasedAttributeValue<EntityReference>("ac.smx_corporateaccountdirector") : null;
                        //opportunity["smx_fcam"] = account.Contains("smx_fcam") ? account.GetAttributeValue<EntityReference>("smx_fcam") : null;
                        opportunity["smx_accountmanager"] = accountManagerFef;

                    }
                }
                catch (Exception ex)
                {
                    _tracer.Trace($"Exception is : {ex.Message}");
                    _tracer.Trace($"stacktrace is : {ex.StackTrace}");
                }
            }

            var sysmexconfig = RetrieveSysmexConfigRelatedFields();
            if (sysmexconfig != null)
            {
                _tracer.Trace("Adding sysmex config fields");
                if (sysmexconfig.Contains("smx_ussalesdirector"))
                {
                    _tracer.Trace("Populating US sales director field");
                    opportunity["smx_ussalesdirector"] = sysmexconfig.GetAttributeValue<EntityReference>("smx_ussalesdirector");
                }
                if (sysmexconfig.Contains("smx_directordistributorrelationships"))
                {
                    _tracer.Trace("Populating director distributor relationships field");
                    opportunity["smx_directordistributorrelationships"] = sysmexconfig.GetAttributeValue<EntityReference>("smx_directordistributorrelationships");
                }
                if (sysmexconfig.Contains("smx_managerofsalesbusinesssupport"))
                {
                    _tracer.Trace("Populating manager of sales business field");
                    opportunity["smx_managerofsalesbusinesssupport"] = sysmexconfig.GetAttributeValue<EntityReference>("smx_managerofsalesbusinesssupport");
                }
                if (sysmexconfig.Contains("smx_canadiannationalservicedirector"))
                {
                    _tracer.Trace("Populating canadian national service director field");
                    opportunity["smx_canadiannationalservicedirector"] = sysmexconfig.GetAttributeValue<EntityReference>("smx_canadiannationalservicedirector");
                }
                if (sysmexconfig.Contains("smx_canadiangeneralmanager"))
                {
                    _tracer.Trace("Populating canadian general manager field");
                    opportunity["smx_canadiangeneralmanager"] = sysmexconfig.GetAttributeValue<EntityReference>("smx_canadiangeneralmanager");
                }
                if (sysmexconfig.Contains("smx_saicfo"))
                {
                    _tracer.Trace("Populating saicfo field");
                    opportunity["smx_saicfo"] = sysmexconfig.GetAttributeValue<EntityReference>("smx_saicfo");
                }
                if (sysmexconfig.Contains("smx_coo"))
                {
                    _tracer.Trace("Populating coo field");
                    opportunity["smx_coo"] = sysmexconfig.GetAttributeValue<EntityReference>("smx_coo");
                }
            }

            _tracer.Trace("Exiting Logic.");
        }

        private Entity RetrieveAccountRelatedFields(Guid accountId)
        {
            var fetch = new FetchExpression($@"
				<fetch>
					<entity name='account'>
						<attribute name='smx_partneraccount' />
						<attribute name='smx_accountihn' />
						<attribute name='smx_hemegpo' />
						<attribute name='smx_urinalysisgpo' />
						<attribute name='smx_esrgpo' />
						<attribute name='smx_coaggpo' />
						<attribute name='smx_chemistryiagpo' />
						<attribute name='smx_flowgpo' />
						<attribute name='smx_aggregationgroup' />
						<attribute name='smx_altterritorymanager' />
						<attribute name='smx_ihncae' />
						<attribute name='smx_fcam' />
							<filter type='and'>
								<condition attribute='accountid' operator='eq' value='{accountId}' />
							</filter>
						<link-entity name='territory' from='territoryid' to='territoryid' link-type='inner' alias='aa'>
							<link-entity name='territory' from='territoryid' to='smx_region' link-type='inner' alias='ab'>
								<attribute name='smx_accountmanager' />
								<attribute name='smx_regionalmanager' />
							</link-entity>
						</link-entity>
						<link-entity name='account' from='accountid' to='smx_accountihn' link-type='outer' alias='ac'>
							<attribute name='smx_corporateaccountdirector'/>
						</link-entity>
						<link-entity name='account' from='accountid' to='smx_partneraccount' link-type='outer' alias='ag'>
							<link-entity name='territory' from='territoryid' to='smx_altterritory' link-type='outer' alias='ah'>
								<attribute name='smx_regionalmanager'/>
							</link-entity>
						</link-entity>
					</entity>
				</fetch>");
            var result = _orgService.RetrieveMultiple(fetch);
            if (result.Entities.Any())
            {
                return result.Entities.FirstOrDefault();
            }
            return null;
        }

        private Entity RetrieveSysmexConfigRelatedFields()
        {
            var fetch = new FetchExpression($@"
				<fetch>
					<entity name='smx_sysmexconfig'>
						<attribute name='smx_directordistributorrelationships' />
						<attribute name='smx_ussalesdirector' />
						<attribute name='smx_managerofsalesbusinesssupport' />
						<attribute name='smx_canadiannationalservicedirector' />
						<attribute name='smx_canadiangeneralmanager' />
						<attribute name='smx_saicfo' />
						<attribute name='smx_coo' />
					</entity>
				</fetch>");

            var result = _orgService.RetrieveMultiple(fetch);
            if (result.Entities.Any())
            {
                return result.Entities.FirstOrDefault();
            }
            return null;
        }
        private Entity RetrievePrimaryLab(Guid labid, ITracingService _tracer)
        {
            try
            {
                var fetch = new FetchExpression($@"
				<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                      <entity name='smx_lab'>
                        <attribute name='smx_labid' />
                        <attribute name='smx_regionalmanager' />
                          <filter type='and'>
                            <condition attribute='smx_labid' operator='eq' value='{ labid }' />
                          </filter>
                        </entity>
                    </fetch>");

                var result = _orgService.RetrieveMultiple(fetch);
                if (result.Entities.Any())
                {
                    return result.Entities.FirstOrDefault();
                }
                else
                {
                    _tracer.Trace("Not found any lab for this opportunity");
                }
            }
            catch (Exception ex)
            {
                _tracer.Trace($"Exception is : {ex.Message}");
                _tracer.Trace($"stacktrace is : {ex.StackTrace}");
                return null;
            }
            return null;
        }
    }
}
