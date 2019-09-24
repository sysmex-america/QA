var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

	global.Sysmex.Forms.smx_lab = (function () {
		var formContext;

		function onLoad(executionContext) {
			formContext = executionContext.getFormContext();

            setDefaultCountry();

            if (formContext.getControl("smx_labaddress") != null) {
                _lockAddressFieldBasedOnSap();
            }
        }

        function _lockAddressFieldBasedOnSap() {
            var addControl = formContext.getControl('smx_labaddress'),
                street1Control = formContext.getControl('smx_street1'),
                street2Control = formContext.getControl('smx_street2'),
                zipControl = formContext.getControl('smx_zippostalcode'),
                cityControl = formContext.getControl('smx_city'),
                countryControl = formContext.getControl('smx_countrysap'),
                stateControl = formContext.getControl('smx_statesap');

            if (!addControl) { return; }

            var address = addControl.getAttribute().getValue();
            if (!address || !address[0] || !address[0].id) { return };
            var addressFetch = [
                '<fetch top="1">',
                    '<entity name="smx_address">',
                        '<attribute name="smx_sapnumber" />',
                        '<order attribute="modifiedon" descending="true" />',
                        '<filter type="and">',
                            '<condition attribute="smx_addressid" operator="eq" value="', address[0].id, '" />',
                            '<condition attribute="smx_sapnumber" operator="not-null" />',
                        '</filter>',
                    '</entity>',
                '</fetch>'].join('');
               Sonoma.WebAPI.fetch("smx_addresses", addressFetch).then(
                function (result) {
                    var hasSap = true;
                    if (result.value.length > 0) {
                        hasSap = true;
                    }
                    else {
                        hasSap = false;
                    }

                    if (street1Control) {
                        street1Control.setDisabled(hasSap);
                    }
                    if (street2Control) {
                        street2Control.setDisabled(hasSap);
                    }
                    if (zipControl) {
                        zipControl.setDisabled(hasSap);
                    }
                    if (cityControl) {
                        cityControl.setDisabled(hasSap);
                    }
                    if (countryControl) {
                        countryControl.setDisabled(hasSap);
                    }
                    if (stateControl) {
                        stateControl.setDisabled(hasSap);
                    }
                }).catch(function (error) {
                    Xrm.Navigation.openAlertDialog({text: error});
                }
            );
        }

        function setDefaultCountry() {
            if (formContext.ui.getFormType() !== 1) {
                return;
            }

            var userId = Sonoma.Guid.decapsulate(formContext.context.getUserId());
            var webApiQuery = ['$filter=systemuserid eq ', userId, '&$select=_businessunitid_value'].join('');

            Sonoma.WebAPI.query('systemusers', webApiQuery)
                .then(function (result) {
                    var user = result.value[0];
                    var businessUnitName = user['_businessunitid_value@OData.Community.Display.V1.FormattedValue'];
                    if (businessUnitName == null) {
                        return;
                    }

                    setDefaultCountryBasedOnBusinessUnit(businessUnitName);
                }, function (error) {
                    Xrm.Navigation.openAlertDialog({text: error});
                });
        }

        function setDefaultCountryBasedOnBusinessUnit(businessUnitName) {
            var countryAttribute = formContext.getAttribute('smx_countrysap');

            if (!countryAttribute) { return; }
            var countryCode;
            switch (businessUnitName) {
                case 'United States':
                    countryCode = 'US';
                    break;
                case 'Canada':
                    countryCode = 'CA';
                    break;
                default:
                    return;
            }

            var webApiQuery = ["$filter=smx_countrycode eq '", countryCode, "'&$select=smx_countryid,smx_name"].join("");

            Sonoma.WebAPI.query('smx_countries', webApiQuery)
                .then(function (result) {
                    if (!result || !result.value) { return; }

                    var country = result.value[0];
                    if (!country) { return;}
                    
                    countryAttribute.setValue([{
                        id: country.smx_countryid,
                        name: country.smx_name,
                        entityType: 'smx_country'
                    }]);
                }, function (error) {
                    Xrm.Navigation.openAlertDialog({text: error});
                });
        }

        return {
            onLoad: onLoad
        };
    }());
}(this));