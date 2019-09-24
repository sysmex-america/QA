var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

    global.Sysmex.Forms.opportunity = (function () {
		var AccountStatusReasons = {
			ReviewCompleted: 1,
			ToBeReviewed: 180700000,
			InitialCreation: 180700001
		},
			formContext;

		function onLoad(executionContext) {
			formContext = executionContext.getFormContext();

            collapseBusinessProcessFlow();
            filterAccountLookup();

            if (formContext && formContext.ui && formContext.ui.getFormType && formContext.ui.getFormType() == 1) {//create
                populateOpportunityFields();
            }
        }

        function populateOpportunityFields() {
            var account = formContext.getAttribute("parentaccountid"),
                compass = formContext.getAttribute("smx_compass"),
                country = formContext.getAttribute("smx_country"),
                government = formContext.getAttribute("smx_governmentaccount"),
                state = formContext.getAttribute("smx_stateprovincesap"),
                territory = formContext.getAttribute("smx_territory"),
                annual = formContext.getAttribute("smx_annualcbc"),
                parent = formContext.getAttribute("parentcontactid"),
                currency = formContext.getAttribute("transactioncurrencyid"),
                priceLevel = formContext.getAttribute("pricelevelid"),
                soldTo = formContext.getAttribute("smx_contractsoldtoaddress"),
                zip = formContext.getAttribute("smx_zippostalcode");

            if (account && account.getValue() && account.getValue()[0].id) {
                
                retrieveParentAccountInfo(account.getValue()[0].id).then(
                    function(accountRef) {
                        if (compass && accountRef["smx_compass"] !== null) compass.setValue(accountRef["smx_compass"]);
                        if (government && accountRef["smx_governmentaccount"] !== null) government.setValue(accountRef["smx_governmentaccount"]);
                        if (zip && accountRef["address1_postalcode"] !== null) zip.setValue(accountRef["address1_postalcode"]);
                        if (annual && accountRef["smx_noofcbc"] !== null) annual.setValue(accountRef["smx_noofcbc"]);
                        
                        if (soldTo && accountRef["_smx_address_value"]) soldTo.setValue([{
                            entityType: accountRef["_smx_address_value@Microsoft.Dynamics.CRM.lookuplogicalname"],
                            id: accountRef["_smx_address_value"],
                            name: accountRef["_smx_address_value@OData.Community.Display.V1.FormattedValue"]
                        }]);
                        if (territory && accountRef["_territoryid_value"]) territory.setValue([{
                            entityType: accountRef["_territoryid_value@Microsoft.Dynamics.CRM.lookuplogicalname"],
                            id: accountRef["_territoryid_value"],
                            name: accountRef["_territoryid_value@OData.Community.Display.V1.FormattedValue"]
                        }]);
                        if (country && accountRef["_smx_countrysap_value"]) country.setValue([{
                            entityType: accountRef["_smx_countrysap_value@Microsoft.Dynamics.CRM.lookuplogicalname"],
                            id: accountRef["_smx_countrysap_value"],
                            name: accountRef["_smx_countrysap_value@OData.Community.Display.V1.FormattedValue"]
                        }]);
                        if (state && accountRef["_smx_stateprovincesap_value"]) state.setValue([{
                            entityType: accountRef["_smx_stateprovincesap_value@Microsoft.Dynamics.CRM.lookuplogicalname"],
                            id: accountRef["_smx_stateprovincesap_value"],
                            name: accountRef["_smx_stateprovincesap_value@OData.Community.Display.V1.FormattedValue"]
                        }]);
                        if (parent && accountRef["_primarycontactid_value"]) parent.setValue([{
                            entityType: accountRef["_primarycontactid_value@Microsoft.Dynamics.CRM.lookuplogicalname"],
                            id: accountRef["_primarycontactid_value"],
                            name: accountRef["_primarycontactid_value@OData.Community.Display.V1.FormattedValue"]
                        }]);
                        if (currency && accountRef["_transactioncurrencyid_value"]) currency.setValue([{
                            entityType: accountRef["_transactioncurrencyid_value@Microsoft.Dynamics.CRM.lookuplogicalname"],
                            id: accountRef["_transactioncurrencyid_value"],
                            name: accountRef["_transactioncurrencyid_value@OData.Community.Display.V1.FormattedValue"]
                        }]);
                        if (priceLevel && accountRef["_defaultpricelevelid_value"]) priceLevel.setValue([{
                            entityType: accountRef["_defaultpricelevelid_value@Microsoft.Dynamics.CRM.lookuplogicalname"],
                            id: accountRef["_defaultpricelevelid_value"],
                            name: accountRef["_defaultpricelevelid_value@OData.Community.Display.V1.FormattedValue"]
                        }]);
                        
                    });
            }
        }
        function retrieveParentAccountInfo(accountId) {
            var fetchxml = [
                '<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">',
                '<entity name="account" >',
                '<attribute name="name" />',
                '<attribute name="territoryid" />',
                '<attribute name="smx_stateprovincesap" />',
                '<attribute name="smx_noofcbc" />',
                '<attribute name="smx_governmentaccount" />',
                '<attribute name="smx_countrysap" />',
                '<attribute name="smx_compass" />',
                '<attribute name="smx_address" />',
                '<attribute name="primarycontactid" />',
                '<attribute name="defaultpricelevelid" />',
                '<attribute name="address1_postalcode" />',
                '<attribute name="transactioncurrencyid" />',
                '<order attribute="name" descending="false" />',
                '<filter type="and">',
                '<condition attribute="accountid" operator="eq" uitype="account" value="', accountId, '" />',
                '</filter>',
                '</entity>',
                '</fetch>'].join('');

            fetchxml = "?fetchXml=" + encodeURIComponent(fetchxml);

            return Xrm.WebApi.retrieveMultipleRecords("account", fetchxml).then(
                function success(result) {
                    if (result.entities.length > 0) {
                        return result.entities[0];
                    }
                },
                function (error) {
                    Xrm.Navigation.openAlertDialog({
                        text: "false" + error.message
                    }, null);
                    displayOppButton = false;
                }
            );
        }
        function collapseBusinessProcessFlow() {
			formContext.ui.process.setDisplayState('collapsed');
        }

        function filterAccountLookup() {
            var accountControl = formContext.getControl('parentaccountid');
            if (!accountControl) {
                return;
            }

            accountControl.addPreSearch(function () {
                accountLookupFilter();
            });
        }

        function accountLookupFilter() {
            var accountControl = formContext.getControl('parentaccountid');
            var fetchXml = [
                '<filter type="and">',
                    '<condition attribute="statuscode" operator="eq" value="', AccountStatusReasons.ReviewCompleted, '" />',
                '</filter>'].join('');

            accountControl.addCustomFilter(fetchXml);
        }

        return {
            onLoad: onLoad
        };
    }());
}(this));