(function (global, Xrm) {
    "use strict";
    global.EY = global.EY || {};
    global.EY.ManagedServices = global.EY.ManagedServices || {};
    global.EY.ManagedServices.Controls = global.EY.ManagedServices.Controls || {};

    global.EY.ManagedServices.Controls.CustomActionHelper = (function () {
        function executeCustomAction(actionName, inputParameters, successCallback, errorCallback) {
            var api = new WebAPI('9.0');
            api.post(actionName, inputParameters).then(successCallback, errorCallback);
        }

        return {
            executeCustomAction: executeCustomAction
        };
    }());

}(this, Xrm));