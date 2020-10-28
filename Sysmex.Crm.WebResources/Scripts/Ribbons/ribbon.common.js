/// <reference path="../../Scripts/Intellisense/XrmPage-vsdoc.js" />
/// <reference path="../../Scripts/Libraries/sonoma.js" />

(function (global) {
    "use strict";
    global.Sysmex = global.Sysmex || {};
    global.Sysmex.common = (function () {
        var formContext;

        function isMobile(primaryControl) {

            var clientContext = Xrm.Utility.getGlobalContext().client;
            if (clientContext.getClient() === 'Mobile') {
                return false;
            }
            return true;
        }


        return {
            isMobile: isMobile
        };

    }());
}(this));