var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

    global.Sysmex.Forms.contactQuickCreate = (function () {
        var formContext;

        function onLoad(executionContext) {
            var stringMapping = [['smx_zippostalcode', 'address1_postalcode'], ['smx_city', 'address1_city'], ['smx_addressstreet2', 'address1_line2'], ['smx_addressstreet1', 'address1_line1']];
            formContext = executionContext.getFormContext();
            var implementationId = formContext.getAttribute('smx_implementation') && formContext.getAttribute('smx_implementation').getValue()
                ? formContext.getAttribute('smx_implementation').getValue()[0].id : null;

            if (!implementationId) {
                return;
            }

            var fetchXml = [
                "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>",
                    "<entity name='smx_address'>",
                        "<attribute name='smx_addressid' />",
                        "<attribute name='smx_zippostalcode' />",
                        "<attribute name='smx_statesap' />",
                        "<attribute name='smx_countrysap' />",
                        "<attribute name='smx_city' />",
                        "<attribute name='smx_addressstreet2' />",
                        "<attribute name='smx_addressstreet1' />",
                        "<attribute name='smx_account' />",
                        "<link-entity name='smx_implementation' from='smx_instrumentshiptoid' to='smx_addressid' link-type='inner' alias='ac'>",
                            "<filter type='and'>",
                                "<condition attribute='smx_implementationid' operator='eq' value='" + implementationId + " ' />",
                            "</filter>",
                        "</link-entity>",
                    "</entity>",
                "</fetch>"
            ].join("");

            fetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);

            return Xrm.WebApi.retrieveMultipleRecords("smx_address", fetchXml).then(
                function success(result) {
                    var addresses, currentAttr, currentValue, contactFieldName, lookupObject = [{}];
                    if (result.entities.length > 0) {
                        addresses = result.entities[0];

                        for (var i = 0; i < stringMapping.length; i++) {
                            contactFieldName = stringMapping[i][1];
                            currentAttr = formContext.getAttribute(contactFieldName) ? formContext.getAttribute(contactFieldName) : null;

                            currentValue = addresses[stringMapping[i][0]];

                            if (currentAttr) {
                                currentAttr.setValue(currentValue)
                            }
                        }

                        currentAttr = formContext.getAttribute('smx_countrysap');
                        if (currentAttr) {
                            lookupObject[0].id = addresses._smx_countrysap_value;
                            lookupObject[0].logicalName = "smx_country";
                            lookupObject[0].entityType = "smx_country";
                            lookupObject[0].name = addresses["_smx_countrysap_value@OData.Community.Display.V1.FormattedValue"];
                            currentAttr.setValue(lookupObject);
                            lookupObject = [{}];
                        }

                        currentAttr = formContext.getAttribute('smx_statesap');
                        if (currentAttr) {
                            lookupObject[0].id = addresses._smx_statesap_value;
                            lookupObject[0].logicalName = "smx_state";
                            lookupObject[0].entityType = "smx_state";
                            lookupObject[0].name = addresses["_smx_statesap_value@OData.Community.Display.V1.FormattedValue"];
                            currentAttr.setValue(lookupObject);
                            lookupObject = [{}];
                        }

                        currentAttr = formContext.getAttribute('parentcustomerid');
                        if (currentAttr) {
                            lookupObject[0].id = addresses._smx_account_value;
                            lookupObject[0].logicalName = "account";
                            lookupObject[0].entityType = "account";
                            lookupObject[0].name = addresses["_smx_account_value@OData.Community.Display.V1.FormattedValue"];
                            currentAttr.setValue(lookupObject);
                            lookupObject = [{}];
                        }
                    }
                },
                function (error) {
                    Xrm.Navigation.openAlertDialog({
                        text: error.message
                    }, null);
                }
            );
        }

        return {
            onLoad: onLoad
        };

    }());
}(this));