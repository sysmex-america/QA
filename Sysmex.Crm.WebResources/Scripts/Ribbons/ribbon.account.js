/// <reference path="../../Scripts/Intellisense/XrmPage-vsdoc.js" />
/// <reference path="../../Scripts/Libraries/sonoma.js" />

(function (global) {
    "use strict";
    global.SMX = global.SMX || {};
    global.SMX.accountRibbon = (function () {
        var formContext;
        var displayOppButton = false;

        function openNewOpportunity(primaryControl) {
            
            formContext = primaryControl;
            
            var parameters = {
                parentaccountid: formContext.data.entity.getId(),
                parentaccountidname: formContext.data.entity.getPrimaryAttributeValue()
            };
            var entityFormOptions = {
                navBar: 'on',
                entityName: 'opportunity',
                cmdBar: true
            };
            Xrm.Navigation.openForm(entityFormOptions, parameters);
        }

        function enableNewOpportunityButton(primaryControl) {
            formContext = primaryControl;
            if (displayOppButton) return displayOppButton;

            var fetchxml = [
                '<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">',
                '<entity name="account" >',
                '<attribute name="name" />',
                '<attribute name="primarycontactid" />',
                '<attribute name="telephone1" />',
                '<attribute name="accountid" />',
                '<order attribute="name" descending="false" />',
                '<filter type="and">',
                '<condition attribute="accountid" operator="eq" uitype="account" value="', formContext.data.entity.getId(), '" />',
                '</filter>',
                '<link-entity name="smx_address" from="smx_account" to="accountid" link-type="inner" alias="ae">',
                '<filter type="and">',
                '<condition attribute="smx_type" operator="eq" value="180700001" />',
                '</filter>',
                '</link-entity>',
                '<link-entity name="smx_address" from="smx_account" to="accountid" link-type="inner" alias="af">',
                '<filter type="and">',
                '<condition attribute="smx_type" operator="eq" value="180700002" />',
                '</filter>',
                '</link-entity>',
                '<link-entity name="smx_lab" from="smx_account" to="accountid" link-type="inner" alias="ag" />',
                '</entity>',
                '</fetch>'].join('');

            fetchxml = "?fetchXml=" + encodeURIComponent(fetchxml);
            

            Xrm.WebApi.retrieveMultipleRecords("account", fetchxml).then(
                function success(result) {
                    if (result.entities.length > 0) {
                        displayOppButton = true;
                        formContext.ui.refreshRibbon(true);
                    } else {
                        displayOppButton = false;
                    }
                },
                function (error) {
                    Xrm.Navigation.openAlertDialog({
                        text: "false" + error.message
                    }, null);
                    displayOppButton = false;
                }
            );
            return displayOppButton;
        }

        return {
            openNewOpportunity: openNewOpportunity,
            enableNewOpportunityButton: enableNewOpportunityButton
        };

    }());
}(this));