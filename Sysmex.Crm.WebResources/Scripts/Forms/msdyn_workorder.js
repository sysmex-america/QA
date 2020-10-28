var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

    global.Sysmex.Forms.msdyn_workorder = (function () {
        var formContext;

        function onLoad(executionContext) {     
            formContext = executionContext.getFormContext();

            if (formContext.ui.getFormType() == 1) //create mode
                loadImplementationInfo();
        }

        function createEntityReference(jsonEntity, entityType) {
            var entityReference = new Array();
            entityReference[0] = new Object();
            entityReference[0].id = jsonEntity.id;
            entityReference[0].logicalName = jsonEntity.logicalName;
            entityReference[0].name = jsonEntity.name;
            entityReference[0].entityType = entityType;
            return entityReference;
        }
                

        function loadImplementationInfo() {
            var implementationProduct = formContext.getAttribute("smx_implementationproductid");

            if (implementationProduct && implementationProduct.getValue() && implementationProduct.getValue().length > 0) {
                var implementationProduct = implementationProduct.getValue();
                var implementationProductId = implementationProduct[0].id;
                var actionName = "smx_GetWorkOrderFieldMappingAction";
                var parameters = { "ImplementationProductId": implementationProductId };

                var callback = function (results) {
                    var implementationInfo = JSON.parse(results.response).Output;
                    if (implementationInfo) {
                        implementationInfo = JSON.parse(implementationInfo);

                        formContext.getAttribute("smx_sapnumber").setValue(implementationInfo.smx_sapshiptonumber);
                        if (implementationInfo.smx_projectmanagerid)
                            formContext.getAttribute("smx_projectmanagerid").setValue(createEntityReference(implementationInfo.smx_projectmanagerid, "systemuser"));
                        if (implementationInfo.smx_instrumentshiptoid.smx_lab != null)
                            formContext.getAttribute("smx_labid").setValue(createEntityReference(implementationInfo.smx_instrumentshiptoid.smx_lab, "smx_lab"));
                        if (implementationInfo.smx_instrumentshiptoid.smx_account != null)
                            formContext.getAttribute("msdyn_serviceaccount").setValue(createEntityReference(implementationInfo.smx_instrumentshiptoid.smx_account,"account"));
                        if (implementationInfo.smx_soldtoid.smx_account != null)
                            formContext.getAttribute("msdyn_billingaccount").setValue(createEntityReference(implementationInfo.smx_soldtoid.smx_account, "account"));
                        formContext.getAttribute("msdyn_address1").setValue(implementationInfo.smx_instrumentshiptoid.smx_addressstreet1);
                        formContext.getAttribute("msdyn_address2").setValue(implementationInfo.smx_instrumentshiptoid.smx_addressstreet2);
                        formContext.getAttribute("msdyn_city").setValue(implementationInfo.smx_instrumentshiptoid.smx_city);
                        formContext.getAttribute("msdyn_stateorprovince").setValue(implementationInfo.smx_instrumentshiptoid.smx_statesap.smx_name);
                        formContext.getAttribute("msdyn_country").setValue(implementationInfo.smx_instrumentshiptoid.smx_countrysap);
                        formContext.getAttribute("msdyn_postalcode").setValue(implementationInfo.smx_instrumentshiptoid.smx_zippostalcode);
                    }
                };
                var errorCallback = function (error) {
                    if (error) {
                        Xrm.Navigation.openAlertDialog({ text: error });
                    }
                }
                EY.ManagedServices.Controls.CustomActionHelper.executeCustomAction(actionName, parameters, callback, errorCallback);
            }
        }

        return {
            onLoad: onLoad
        };
    }());
})(this);
