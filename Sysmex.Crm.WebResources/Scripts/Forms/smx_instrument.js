/// <reference path="../Libraries/sonoma.js" />
var Xrm = window.Xrm || parent.window.Xrm;

(function (global) {
    "use strict";

    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

	global.Sysmex.Forms.smx_instrument = (function () {
		var formContext;

		function onLoad(executionContext) {
			formContext = executionContext.getFormContext();

            formContext.getControl("smx_lab").addPreSearch(filterLab);
            formContext.getControl("smx_model").addPreSearch(filterModel);
            formContext.getControl("smx_productline").addPreSearch(filterProductLine);
            formContext.getAttribute("smx_productline").addOnChange(switchViewManufacturer);
        }

        function switchViewManufacturer() {
            // Lookup Filter for manufacturer, filter by Product Line with N:N relationship
            //Needed to switch defaul views to make this work since N:N not supported filter
            var productLine = formContext.getAttribute("smx_productline").getValue();
            if (productLine == null || productLine.length <= 0) {
                return;
            }

            switch(productLine[0].name.toLowerCase()) {
                case 'chemistry/ia': //View: Active Chemistry/IA Manufacture
                    formContext.getControl("smx_manufacturer").setDefaultView("{cb607e22-5403-e711-810e-e0071b6a9211}");
                    break;
                case 'coagulation': //View: Active Coagulation Manufacture
                    formContext.getControl("smx_manufacturer").setDefaultView("{5ae0cb69-5403-e711-810e-e0071b6a9211}");
                    break;
                case 'esr': //View: Active ESR Manufacture
                    formContext.getControl("smx_manufacturer").setDefaultView("{cee7cc88-5403-e711-810e-e0071b6a9211}");
                    break;
                case 'flow cytometry': //View: Active Flow Manufacture
                    formContext.getControl("smx_manufacturer").setDefaultView("{9b74e0f3-5003-e711-810e-e0071b6a9211}");
                    break;
                case 'hematology': //View: Active Hematology Manufacture
                    formContext.getControl("smx_manufacturer").setDefaultView("{d6c351ae-5303-e711-810e-e0071b6a9211}");
                    break;
                case 'urinalysis': //View: Active Urinalysis Manufacture
                    formContext.getControl("smx_manufacturer").setDefaultView("{1f5f78d3-5303-e711-810e-e0071b6a9211}");
                    break;
                default: //View: Active Manufacturer 
                    formContext.getControl("smx_manufacturer").setDefaultView("{205AC19F-F655-4580-B894-A1D33A6FC800}");
            }
        }

        function filterModel() {
            // Add Lookup Filter to Model Lookup, filter by Product Line and Manufacture
            var productLine = formContext.getAttribute("smx_productline").getValue();
            var manufacture = formContext.getAttribute("smx_manufacturer").getValue();
            
            if (productLine == null || productLine.length <= 0 ||
                manufacture == null || manufacture.length <= 0) {
                return;
            }

            var productLineId = productLine[0].id;
            var manufactureId = manufacture[0].id;

            var fetchXml = ["<filter type='and'>",
              "<condition attribute='smx_productline' operator='eq' value='" + productLineId + "' />",
              "<condition attribute='smx_manufacturer' operator='eq' value='" + manufactureId + "' />",
              "</filter>"].join('');

            formContext.getControl("smx_model").addCustomFilter(fetchXml);
        }

        function filterLab() {
            // Add Lookup Filter to Lab Lookup, filter by selected Account
            var account = formContext.getAttribute("smx_account").getValue();

            if (account == null) {
                return;
            }

            var accountId = account[0].id;
            var fetchXml = ["<filter type='and'>",
              "<condition attribute='smx_account' operator='eq' value='" + accountId + "' />",
              "</filter>"].join('');

            formContext.getControl("smx_lab").addCustomFilter(fetchXml);
        }

        function filterProductLine() {
            // Add Lookup Filter to Product Line
            var fetchXml = ["<filter type='and'>",
                            "<condition attribute='smx_name' operator='ne' value='WAM' />",
                            "<condition attribute='smx_name' operator='ne' value='Automation' />",
                            "</filter>"].join('');

            formContext.getControl("smx_productline").addCustomFilter(fetchXml);
        }

        return {
            onLoad: onLoad
        }
    }());
}(window));

