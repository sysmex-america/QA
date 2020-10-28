var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

    global.Sysmex.Forms.smx_implementation = (function () {
        var formContext;
        var _shipToAccountId;
        var attributesConfirmedValues = new Object();
        var attributesToConfirmChanges = ["smx_contracttype", "smx_revenuetype" , "smx_orderrush" , "smx_orderrushreason"];

        function executeOnLoad(executionContext) {
            formContext = executionContext.getFormContext();

            setShipToAccount();

            attributesToConfirmChanges.forEach(setAttributeConfirmedValue);

            var shipTo = formContext.getAttribute("smx_instrumentshiptoid");  
            if (shipTo) {
                shipTo.addOnChange(setShipToAccount)
            }
        }

        function setShipToAccount() {
            var shipTo = formContext.getAttribute("smx_instrumentshiptoid");
            var shipToValue;

            if (shipTo && shipTo.getValue() && shipTo.getValue().length > 0) {
                shipToValue = shipTo.getValue();
                
                if (shipToValue) {
                    getShipToAccountId(shipToValue[0].id);
                    addContactPreFilter();                    
                }
                else {
                    _shipToAccountId = null;
                    removeContactPreFilter();
                }
            }
        }

        function getShipToAccountId(addressId) {
            var webApiQuery = ["$filter=smx_addressid eq '", addressId, "'&$select=smx_Account"].join("");

            Sonoma.WebAPI.query('smx_addresses', webApiQuery)
                .then(function (result) {
                    if (!result || !result.value || !result.value.length) { return; }

                    var account = result.value[0];

                    _shipToAccountId = account['_smx_account_value'];
                });
        }

        function removeContactPreFilter() {
            var contactNameControl = formContext.getControl("smx_customercontactid");
            if (contactNameControl) {
                contactNameControl.removePreSearch(filterContactName);
            }
        }

        function addContactPreFilter() {
            var contactNameControl = formContext.getControl("smx_customercontactid");
            if (contactNameControl) {
                contactNameControl.addPreSearch(filterContactName);
            }
        }

        function filterContactName() {
            var contactNameControl = formContext.getControl("smx_customercontactid"); 

            if (!_shipToAccountId || !contactNameControl) {
                return;
            }

            var filter = "<filter type='and'><condition attribute='parentcustomerid' operator='eq' value='" + _shipToAccountId + "' /></filter>";
            contactNameControl.addCustomFilter(filter, "contact");         
        }

        function setAttributeConfirmedValue(attributeName) {
            var attribute = formContext.getAttribute(attributeName);
            if (attribute) {
                attribute.addOnChange(handleAttributeOnChange);
                attributesConfirmedValues[attributeName] = attribute.getValue();
            }
        }

        function handleAttributeOnChange(e) {
            var changedAttribute = formContext.getAttribute(e.getEventSource().getName());
            var confirmStrings = {
                text: "You've changed the ${formContext.getControl(changedAttribute.getName()).getLabel()}. Do you want to proceed with this change?",
                title: 'Confirm Change',
                cancelButtonLabel: 'No',
                confirmButtonLabel: 'Yes'
            };
            var confirmOptions = { height: 200, width: 450 };

            Xrm.Navigation.openConfirmDialog(confirmStrings, confirmOptions).then(
                function (success) {
                    if (success.confirmed) {
                        attributesConfirmedValues[changedAttribute.getName()] = changedAttribute.getValue();
                    }
                    else {
                        changedAttribute.setValue(attributesConfirmedValues[changedAttribute.getName()]);
                    }
                });
        }

        return {
            executeOnLoad: executeOnLoad
        };
    }());
}(this));