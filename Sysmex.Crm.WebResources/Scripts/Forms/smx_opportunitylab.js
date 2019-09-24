var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

    global.Sysmex.Forms.smx_opportunitylab = (function () {
		var formContext;
		
		function onLoad(executionContext) {
            formContext = executionContext.getFormContext();

            attachHandlers();
        }
        
        function attachHandlers() {
            var labIdAttribute = formContext.getAttribute('smx_labid');

            if (labIdAttribute) {
                labIdAttribute.addOnChange(processShipToAddress);
            }
        }

        function processShipToAddress() {
            var opportunityIdAttribute = formContext.getAttribute('smx_opportunityid');
            if (!opportunityIdAttribute || !opportunityIdAttribute.getValue()) {
                return;
            }

            var shipToAddressIdAttribute = formContext.getAttribute('smx_shiptoaddressid');
            if (!shipToAddressIdAttribute) {
                return;
            }

            var labIdAttribute = formContext.getAttribute('smx_labid');
            if (!labIdAttribute.getValue()) {
                shipToAddressIdAttribute.setValue(null);
                return;
            }

            var opportunityLabFetch = [
                '<fetch top="1">',
                '  <entity name="smx_opportunitylab">',
                '    <attribute name="smx_opportunitylabid" />',
                '    <filter type="and">',
                '      <condition attribute="statecode" operator="eq" value="0" />',
                '      <condition attribute="smx_opportunitylabid" operator="ne" value="', Xrm.Page.data.entity.getId(), '" />',
                '      <condition attribute="smx_opportunityid" operator="eq" value="', opportunityIdAttribute.getValue()[0].id, '" />',
                '      <condition attribute="smx_labid" operator="eq" value="', labIdAttribute.getValue()[0].id, '" />',
                '    </filter>',
                '  </entity>',
                '</fetch>'].join('');

            Sonoma.WebAPI.fetch("smx_opportunitylabs", opportunityLabFetch)
                .then(function (result) {
                    return populateShipToAddress(result.value);
                })
                .catch(function (error) {
                    Xrm.Navigation.openAlertDialog({ text: error });
                });
        }

        function populateShipToAddress(val) {
            var shipToAddressIdAttribute = formContext.getAttribute('smx_shiptoaddressid');
            var labIdAttribute = formContext.getAttribute('smx_labid');

            if (val.length > 0) {
                Xrm.Navigation.openAlertDialog({
                    text: 'This Lab is already associated to the current Opportunity'
                });
                labIdAttribute.setValue(null);
                return null;
            }

            return Sonoma.WebAPI.query('smx_labs',
                ['$filter=smx_labid eq ', Sonoma.Guid.decapsulate(labIdAttribute.getValue()[0].id), '&$select=_smx_labaddress_value'].join(''))
                .then(function (result) {
                    if (!result.value.length || !result.value[0]._smx_labaddress_value) {
                        shipToAddressIdAttribute.setValue(null);
                        return;
                    }

                    var labAddressId = result.value[0]._smx_labaddress_value;
                    var labAddressName = result.value[0]['_smx_labaddress_value@OData.Community.Display.V1.FormattedValue'];

                    shipToAddressIdAttribute.setValue([{
                        id: labAddressId,
                        name: labAddressName,
                        entityType: 'smx_address'
                    }]);
                });
        }

        return {
            onLoad: onLoad
        };
    }());
}(this));