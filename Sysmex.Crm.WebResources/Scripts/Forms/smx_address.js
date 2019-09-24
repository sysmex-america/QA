var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

	global.Sysmex.Forms.smx_address = (function () {
		var formContext;
		
		function onLoad(executionContext) {
			formContext = executionContext.getFormContext();

            formContext.data.entity.addOnSave(_lockAddressFieldBasedOnSap);

            filterStatesBasedOnCountry();
            setDefaultCountry();
            lockStateFieldBasedOnCountry();
            attachHandlers()

            if (formContext.getControl("smx_sapnumber") != null) {
                _lockAddressFieldBasedOnSap();
            }

        }
        
        function attachHandlers() {
            var countryAttribute = formContext.getAttribute('smx_countrysap');
            if (countryAttribute) {
                countryAttribute.addOnChange(lockStateFieldBasedOnCountry);
            }
        }

        function lockStateFieldBasedOnCountry() {
            var stateControl = formContext.getControl('smx_statesap');
            if (!stateControl) { return; }

            var countryAttribute = formContext.getAttribute('smx_countrysap');
            if (!countryAttribute || !countryAttribute.getValue) { return; }

            // Lock if country is not populated
            stateControl.setDisabled(!countryAttribute.getValue());
        }

        function filterStatesBasedOnCountry() {
            var stateControl = formContext.getControl('smx_statesap');
            if (!stateControl) { return; }

            stateControl.addPreSearch(function () {
                var countryAttribute = formContext.getAttribute('smx_countrysap');
                if (!countryAttribute || !countryAttribute.getValue || !countryAttribute.getValue() || !countryAttribute.getValue()[0]) { return; }

                var countryId = Sonoma.Guid.decapsulate(countryAttribute.getValue()[0].id);

                var fetchXml = ["<filter type='and'><condition attribute='smx_country' operator='eq' value='", countryId, "'/></filter>"].join('');
                formContext.getControl("smx_statesap").addCustomFilter(fetchXml);
            });
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

        function _lockAddressFieldBasedOnSap() {
            var sap = formContext.getAttribute('smx_sapnumber'),
                street1Control = formContext.getControl('smx_addressstreet1'),
                street2Control = formContext.getControl('smx_addressstreet2'),
                zipControl = formContext.getControl('smx_zippostalcode'),
                cityControl = formContext.getControl('smx_city'),
                countryControl = formContext.getControl('smx_countrysap'),
                stateControl = formContext.getControl('smx_statesap');

            if (!sap) { return; }
            
            var hasSap = sap.getValue() !== null;

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