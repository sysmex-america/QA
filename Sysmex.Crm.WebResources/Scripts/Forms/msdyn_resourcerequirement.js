var Xrm = window.Xrm || parent.Xrm;

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};

    global.Sysmex.Forms.msdyn_resourcerequirement = (function () {
        var formContext;

        function onLoad(executionContext) {
            formContext = executionContext.getFormContext();

            if (formContext.ui.getFormType() == 1) {//create mode
                console.log(formContext.getAttribute("msdyn_fromdate").getValue());
                if (formContext.getAttribute("msdyn_fromdate") && formContext.getAttribute("msdyn_fromdate").getValue() && formContext.getAttribute("msdyn_fromdate").getValue().getHours() !== 0) {
                    var date = formContext.getAttribute("msdyn_fromdate").getValue();
                    var newDate = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 2);
                    formContext.getAttribute("msdyn_fromdate").setValue(newDate);
                }
            }
        }

        return {
            onLoad: onLoad
        };
    }());
})(this);