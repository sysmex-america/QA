var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

    global.Sysmex.Forms.emailMessage = (function () {
        var formContext;

        function onLoad(executionContext) {
            formContext = executionContext.getFormContext();

            var regardingObjectCtrl = formContext.getAttribute("regardingobjectid")
            if (regardingObjectCtrl) {
                regardingObjectCtrl.addOnChange(handlerRegardingObjectChange);
                if (regardingObjectCtrl.getValue() != null && (formContext.ui.getFormType() === 1 || formContext.getAttribute("to").getValue() == null)) 
                    handlerRegardingObjectChange();
            }
        }

        function handlerRegardingObjectChange() {
            var regardingObjectCtrl = formContext.getAttribute("regardingobjectid")
            if (regardingObjectCtrl && regardingObjectCtrl.getValue() && regardingObjectCtrl.getValue().length && regardingObjectCtrl.getValue().length > 0) {
                var regardingObjectType = regardingObjectCtrl.getValue()[0].entityType;
                var regardingObjectGuid = regardingObjectCtrl.getValue()[0].id.replace("{", "").replace("}", "");
                setEmailSubject(regardingObjectType, regardingObjectGuid);
            }
        }

        function setEmailSubject(regardingObjectType, regardingObjectGuid) {
            var setEmailToCtrl = false;
            var ret;
            switch (regardingObjectType) {
                case "smx_implementation":
                    var fetchXml = [
                        '   <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">',
                        '       <entity name="smx_implementation">',
                        '           <attribute name="smx_contractnumber" alias="implementation.smx_contractnumber" />',
                        '           <attribute name="smx_additionalcontactoneid" alias="implementation.smx_additionalcontactoneid" />',
                        '           <attribute name="smx_additionalcontacttwoid" alias="implementation.smx_additionalcontacttwoid" />',
                        '           <attribute name="smx_additionalcontactthreeid" alias="implementation.smx_additionalcontactthreeid" />',
                        '           <attribute name="smx_customercontactid" alias="implementation.smx_customercontactid" />',
                        '           <filter>',
                        '               <condition attribute="smx_implementationid" operator="eq" value="' + regardingObjectGuid + '" />',
                        '           </filter>',
                        '           <link-entity name="smx_address" from="smx_addressid" to="smx_instrumentshiptoid" visible="false" link-type="outer" ', 'alias="address">',
                        '               <attribute name="smx_statesap" />',
                        '               <attribute name="smx_name" />',
                        '               <attribute name="smx_city" />',
                        '               <link-entity name="smx_state" from="smx_stateid" to="smx_statesap" link-type="outer" alias="state" >',
                        '                   <attribute name="smx_region" />',
                        '               </link-entity>',
                        '           </link-entity>',
                        '       </entity>',
                        '   </fetch>',
                    ].join('');
                    fetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
                    ret = Xrm.WebApi.retrieveMultipleRecords("smx_implementation", fetchXml);
                    var setEmailToCtrl = true;
                    break;

                case "smx_implementationproduct":
                    var fetchXml = [
                        '   <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">',
                        '       <entity name="smx_implementationproduct" >',
                        '           <filter>',
                        '               <condition attribute="smx_implementationproductid" operator="eq" value="' + regardingObjectGuid + '" />',
                        '           </filter>',
                        '           <link-entity name="smx_implementation" from="smx_implementationid" to="smx_implementationid" link-type="inner" alias="implementation" >',
                        '               <attribute name="smx_contractnumber" />',
                        '               <attribute name="smx_additionalcontactoneid" alias="implementation.smx_additionalcontactoneid" />',
                        '               <attribute name="smx_additionalcontacttwoid" alias="implementation.smx_additionalcontacttwoid" />',
                        '               <attribute name="smx_additionalcontactthreeid" alias="implementation.smx_additionalcontactthreeid" />',
                        '               <attribute name="smx_customercontactid" alias="implementation.smx_customercontactid" />',
                        '               <link-entity name="smx_address" from="smx_addressid" to="smx_instrumentshiptoid" link-type="outer" alias="address" >',
                        '                   <attribute name="smx_statesap" />',
                        '                   <attribute name="smx_name" />',
                        '                   <attribute name="smx_city" />',
                        '                   <link-entity name="smx_state" from="smx_stateid" to="smx_statesap" link-type="outer" alias="state" >',
                        '                       <attribute name="smx_region" />',
                        '                   </link-entity>',
                        '               </link-entity>',
                        '           </link-entity>',
                        '       </entity>',
                        '   </fetch>',
                    ].join('');
                    fetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
                    ret = Xrm.WebApi.retrieveMultipleRecords("smx_implementationproduct", fetchXml);
                    var setEmailToCtrl = true;
                    break;

                case "smx_equipmentpickup":
                    var fetchXml = [
                        '   <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">',
                        '       <entity name="smx_equipmentpickup" >',
                        '           <attribute name="smx_equipmentpickupid" />',
                        '           <filter>',
                        '               <condition attribute="smx_equipmentpickupid" operator="eq" value="' + regardingObjectGuid + '" />',
                        '           </filter>',
                        '           <link-entity name="smx_implementation" from="smx_implementationid" to="smx_implementationid" link-type="inner" alias="implementation" >',
                        '               <attribute name="smx_contractnumber" />',
                        '                   <link-entity name="smx_address" from="smx_addressid" to="smx_instrumentshiptoid" link-type="outer" alias="address" >',
                        '                       <attribute name="smx_statesap" />',
                        '                       <attribute name="smx_name" />',
                        '                       <attribute name="smx_city" />',
                        '                       <link-entity name="smx_state" from="smx_stateid" to="smx_statesap" link-type="outer" alias="state" >',
                        '                           <attribute name="smx_region" />',
                        '                       </link-entity>',
                        '                   </link-entity>',
                        '           </link-entity>',
                        '       </entity>',
                        '   </fetch>',
                    ].join('');
                    fetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
                    ret = Xrm.WebApi.retrieveMultipleRecords("smx_equipmentpickup", fetchXml);
                    break;

                default:
                    return;
            }

            if (ret) {
                ret.then(
                    function success(result) {
                        var subjectCtrl = formContext.getAttribute("subject");
                        if (subjectCtrl && !subjectCtrl.getValue()) {
                            var contractNumber = result.entities[0]["implementation.smx_contractnumber"] == null ? "" : result.entities[0]["implementation.smx_contractnumber"];
                            var addressName = result.entities[0]["address.smx_name"] == null ? "" : result.entities[0]["address.smx_name"];
                            var addressCity = result.entities[0]["address.smx_city"] == null ? "" : result.entities[0]["address.smx_city"];
                            var addressState = result.entities[0]["state.smx_region"] == null ? "" : result.entities[0]["state.smx_region"];
                            subjectCtrl.setValue(contractNumber + " -- " + addressName + " -- " + addressCity + " -- " + addressState);
                        }
                        if (setEmailToCtrl) {
                            var emailToCtrl = formContext.getAttribute("to");
                            if (emailToCtrl && !emailToCtrl.getValue()) {
                                var contactArray = createContactArray(result.entities[0]);  
                                if (contactArray.length > 0)
                                    emailToCtrl.setValue(contactArray);
                            }
                        }
                    },
                    function (error) {
                        console.log(error.message);
                    }
                );
            }
        }

        function createContactArray(record) {
            var contactArray = new Array();
            var indexArray = 0;

            if (record["implementation.smx_customercontactid"] != null) {
                contactArray[indexArray] = createContactReference(record["implementation.smx_customercontactid@OData.Community.Display.V1.FormattedValue"], record["implementation.smx_customercontactid"]);
                indexArray++;
            }

            if (record["implementation.smx_additionalcontactoneid"] != null) {
                contactArray[indexArray] = createContactReference(record["implementation.smx_additionalcontactoneid@OData.Community.Display.V1.FormattedValue"], record["implementation.smx_additionalcontactoneid"]);
                indexArray++;
            }

            if (record["implementation.smx_additionalcontacttwoid"] != null) {
                contactArray[indexArray] = createContactReference(record["implementation.smx_additionalcontacttwoid@OData.Community.Display.V1.FormattedValue"], record["implementation.smx_additionalcontacttwoid"]);
                indexArray++;
            }

            if (record["implementation.smx_additionalcontactthreeid"] != null) {
                contactArray[indexArray] = createContactReference(record["implementation.smx_additionalcontactthreeid@OData.Community.Display.V1.FormattedValue"], record["implementation.smx_additionalcontactthreeid"]);
            }

            return contactArray;
        }

        function createContactReference(contactName, contactId) {
            var contactReference = new Object();
            contactReference.id = contactId;
            contactReference.name = contactName;
            contactReference.entityType = "contact";
            return contactReference;
        }

        return {
            onLoad: onLoad
        };
    }());
}(this));