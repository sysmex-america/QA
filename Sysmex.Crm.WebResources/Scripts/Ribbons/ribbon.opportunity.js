/// <reference path="../../Scripts/Intellisense/XrmPage-vsdoc.js" />
/// <reference path="../../Scripts/Libraries/sonoma.js" />
/// <reference path="../../Scripts/Libraries/SP.Window.js" />

(function (global) {
    "use strict";
    global.SMX = global.SMX || {};

    global.SMX.opportunityRibbon = (function () {
        var AccountStatusReasons = {
            ReviewCompleted: 1,
            ToBeReviewed: 180700000,
            InitialCreation: 180700001
        };

        function showProductDialog() {
            parent.window.top.SharedXrmContext = Xrm;

            var DialogOption = new Xrm.DialogOptions;
            DialogOption.width = 400;
            DialogOption.height = 360;

            var id = Xrm.Page.data.entity.getId(),
                logicalName = Xrm.Page.data.entity.getEntityName();

            var dataString = encodeURIComponent('entityId=' + id + '&entityName=' + logicalName);
            Xrm.Internal.openDialog('/WebResources/smx_/Dialogs/SplitOpportunity.html?Data=' + dataString, DialogOption, null, null, dialogCallBack);
        }

        function dialogCallBack(callbackArguments) {
            if (callbackArguments.cloned) {
                Xrm.Utility.openEntityForm("opportunity", callbackArguments.entityId);
            }
        }

        function enableSplitOpportunityButton(primaryControl) {
            var formContext = primaryControl.getFormContext();

            if (formContext.context.client.getClient() === 'Mobile') {
                return false;
            }

            if (formContext.getAttribute('smx_isparentopportunity')) {
                if (formContext.getAttribute('smx_isparentopportunity').getValue()) {
                    return false;
                }
            }

            return true;
        }

        function enableAddNewButton(primaryControl) {
            var formContext = primaryControl.getFormContext();

            // Do not run rules if the main entity is not Account
            var entityName = formContext.data.entity.getEntityName();
            if (entityName !== 'account') {
                return true;
            }

            // Disable the button if the Associated Opportunied View is the main Control
            var controlId = primaryControl._element.id;
            if (controlId === 'crmFormProxyForRibbon') {
                return false;
            }

            var statusReasonAttribute = formContext.getAttribute('statuscode');
            if (!statusReasonAttribute) {
                return false;
            }

            var statusReason = statusReasonAttribute.getValue();
            if (statusReason !== null && statusReason === AccountStatusReasons.ReviewCompleted) {
                return true;
            }

            return false;
        }

        return {
            showProductDialog: showProductDialog,
            enableSplitOpportunityButton: enableSplitOpportunityButton,
            enableAddNewButton: enableAddNewButton
        };
    }());
}(this));