var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

    global.Sysmex.Forms.smx_implementationproduct = (function () {
        var formContext;
        var attributesConfirmedValues = new Object();
        var attributesToConfirmChanges = ["smx_icndate", "smx_serialno", "smx_seicn"];
        var normalProcessReasonCode = 180700008;
        var icnFlagChanged = false;

        function executeOnLoad(executionContext) { 
            formContext = executionContext.getFormContext();
            attributesToConfirmChanges.forEach(setAttributeConfirmedValue);
            if (formContext.getAttribute("smx_icncompletedbyid")) {
                attributesConfirmedValues["smx_icncompletedbyid"] = formContext.getAttribute("smx_icncompletedbyid").getValue();
            }

            if (formContext.getAttribute("smx_icncomplete")) {
                formContext.getAttribute("smx_icncomplete").addOnChange(setIcn);
            }

            if (formContext.getAttribute("smx_potentialrevenuedate")) {
                formContext.getAttribute("smx_potentialrevenuedate").addOnChange(potentialRevChange);
            }

            if (formContext.getAttribute("smx_delayreasoncode")) {
                formContext.getAttribute("smx_delayreasoncode").addOnChange(reasonCodeChange);
            }
        }

        function potentialRevChange() {
            var reasonCode = formContext.getAttribute("smx_delayreasoncode");
            var potRevDate = formContext.getAttribute("smx_potentialrevenuedate");
            var normalDate = formContext.getAttribute("smx_normalprocessdate");

            if (reasonCode && potRevDate && normalDate) {
                if (normalDate.getValue() && potRevDate.getValue() > normalDate.getValue() && reasonCode.getValue() == normalProcessReasonCode) {
                    reasonCode.setValue(null);
                    reasonCode.setRequiredLevel("required");
                }
                else {
                    reasonCode.setRequiredLevel("none");
                }
            }
        }

        function reasonCodeChange(e) {
            var reasonCode = formContext.getAttribute("smx_delayreasoncode");
            var potRevDate = formContext.getAttribute("smx_potentialrevenuedate");
            var normalDate = formContext.getAttribute("smx_normalprocessdate");

            if (reasonCode && potRevDate && normalDate) {
                if (normalDate.getValue() && potRevDate.getValue() > normalDate.getValue() && reasonCode.getValue() == normalProcessReasonCode) {
                    reasonCode.setValue(null);
                    reasonCode.setRequiredLevel("required");
                }
                else {
                    reasonCode.setRequiredLevel("none");
                }
            }
        }

        function setIcn() {
            icnFlagChanged = true;
            var icnAttribute = formContext.getAttribute("smx_icncomplete");
            var icnValue = icnAttribute.getValue();
            var completedByAttribute = formContext.getAttribute("smx_icncompletedbyid");
            var icnDateAttribute = formContext.getAttribute("smx_icndate");

            if (icnValue) {
                var confirmStrings = {
                    text: "You've changed the ICN Complete field to Yes. This will populate the ICN Date. Do you want to proceed?",
                    title: 'Confirm Change',
                    cancelButtonLabel: 'No',
                    confirmButtonLabel: 'Yes'
                };
                var confirmOptions = { height: 200, width: 450 };

                Xrm.Navigation.openConfirmDialog(confirmStrings, confirmOptions).then(
                    function (success) {
                        if (success.confirmed) {
                            //skipNextConfirmation = true;

                            icnDateAttribute.setValue(new Date());
                            completedByAttribute.setValue(createEntityReference(
                                Xrm.Utility.getGlobalContext().userSettings.userId,
                                "systemuser",
                                Xrm.Utility.getGlobalContext().userSettings.userName,
                                "systemuser"
                            ));
                        }
                        else {
                            icnAttribute.setValue(false);
                            completedByAttribute.setValue(null);
                            icnDateAttribute.setValue(null);
                        }
                    });
            }
            else {
                completedByAttribute.setValue(null);
                icnDateAttribute.setValue(null);
                handleAttributeOnChange(icnDateAttribute, "smx_icndate");
            }
        }

        function createEntityReference(id, logicalName, name, entityType) {
            var entityReference = new Array();
            entityReference[0] = new Object();
            entityReference[0].id = id;            
            entityReference[0].name = name;
            entityReference[0].entityType = entityType;
            return entityReference;
        }

        function setAttributeConfirmedValue(attributeName) {
            var attribute = formContext.getAttribute(attributeName);
            if (attribute) {
                attribute.addOnChange(handleAttributeOnChange);
                attributesConfirmedValues[attributeName] = attribute.getValue();
            }
        }

        function handleAttributeOnChange(e, fieldName) {
            var changedAttribute = fieldName
                ? formContext.getAttribute(fieldName)
                : formContext.getAttribute(e.getEventSource().getName());

            var confirmStrings = {
                text: "You've changed the " + formContext.getControl(changedAttribute.getName()).getLabel() + ". Do you want to proceed with this change?",
                title: 'Confirm Change',
                cancelButtonLabel: 'No',
                confirmButtonLabel: 'Yes'
            };
            var confirmOptions = { height: 200, width: 450 };

            Xrm.Navigation.openConfirmDialog(confirmStrings, confirmOptions).then(
                function (success) {
                    if (success.confirmed) {                        
                        attributesConfirmedValues[changedAttribute.getName()] = changedAttribute.getValue();
                        handleSpecialCasesForSettingField(changedAttribute.getName(), changedAttribute.getValue());
                    }
                    else {
                        changedAttribute.setValue(attributesConfirmedValues[changedAttribute.getName()]);
                        handleSpecialCasesForRoleBacks(changedAttribute.getName());
                    }
                    icnFlagChanged = false;
                });
        }

        function handleSpecialCasesForSettingField(attributeName, value) {
            var icnAttribute, icnFieldName = "smx_icncomplete", completedByField = "smx_icncompletedbyid", completedByAttribute;
            icnAttribute = formContext.getAttribute(icnFieldName);
            completedByAttribute = formContext.getAttribute(completedByField);
            if (attributeName === "smx_icndate" && value && icnAttribute && completedByAttribute) {
                icnAttribute.setValue(true);
                completedByAttribute.setValue(createEntityReference(
                    Xrm.Utility.getGlobalContext().userSettings.userId,
                    "systemuser",
                    Xrm.Utility.getGlobalContext().userSettings.userName,
                    "systemuser"
                ));
            }
            else if (!value && icnAttribute.getValue() === true && formContext.getAttribute(attributeName) && !formContext.getAttribute(attributeName).getValue()) {
                if (formContext.getAttribute(completedByField)) {
                    formContext.getAttribute(completedByField).setValue(null);
                }
                icnAttribute.setValue(false);
            }
        }

        function handleSpecialCasesForRoleBacks(attributeName) {
            var icnAttribute, icnFieldName = "smx_icncomplete", completedByField = "smx_icncompletedbyid";
            if (attributeName === "smx_icndate") {
                icnAttribute = formContext.getAttribute(icnFieldName);
                if (icnFlagChanged) {
                    if (icnAttribute.getValue() === true) {
                        icnAttribute.setValue(false);
                        if (formContext.getAttribute(completedByField)) {
                            formContext.getAttribute(completedByField).setValue(null);
                        }
                    }
                    else if (icnAttribute.getValue() === false) {
                        icnAttribute.setValue(true);
                        formContext.getAttribute(attributeName).setValue(new Date());
                        if (formContext.getAttribute(completedByField)) {
                            if (attributesConfirmedValues[completedByField]) {
                                formContext.getAttribute(completedByField).setValue(attributesConfirmedValues[completedByField]);
                            }
                            else {
                                formContext.getAttribute(completedByField).setValue(createEntityReference(
                                    Xrm.Utility.getGlobalContext().userSettings.userId,
                                    "systemuser",
                                    Xrm.Utility.getGlobalContext().userSettings.userName,
                                    "systemuser"));
                            }
                        }
                    }
                }
                else {
                    if (icnAttribute.getValue() === false) {
                        if (formContext.getAttribute(completedByField)) {
                            formContext.getAttribute(completedByField).setValue(null);
                        }
                    }                    
                }
            }
        }

        return {
            executeOnLoad: executeOnLoad
        };

    }());
}(this));