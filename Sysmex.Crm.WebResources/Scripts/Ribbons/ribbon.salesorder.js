(function (global) {
    "use strict";
    global.SMX = global.SMX || {};
    global.SMX.salesorderRibbon = (function () {
        function createContract(control, id) {
            var formContext = control.getFormContext();

            Xrm.Utility.showProgressIndicator('Creating Contract...');

            Xrm.WebApi.execute({
                Input: id,
                getMetadata: function () {
                    return {
                        boundParameter: null,
                        parameterTypes: {
                            Input: {
                                typeName: 'Edm.String',
                                structuralProperty: 1
                            }
                        },
                        operationType: 0,
                        operationName: 'smx_CreateContract'
                    }
                }
            }).then(function (response) {
                var responseText;

                if (response.responseText) {
                    responseText = JSON.parse(response.responseText);
                }

                if (responseText && responseText.Output) {
                    Xrm.Utility.closeProgressIndicator();

                    formContext.ui.setFormNotification('Contract successfully created.', 'INFO', 'contractCreated');

                    setTimeout(function () {
                        formContext.ui.clearFormNotification('contractCreated');
                    }, 5000);
                } else {
                    Xrm.Utility.closeProgressIndicator();
                    Xrm.Navigation.openAlertDialog({ text: 'No contract number was returned.' });
                }
            }, function (err) {
                Xrm.Utility.closeProgressIndicator();
                Xrm.Navigation.openErrorDialog({
                    errorCode: err.errorCode,
                    details: err.message,
                    message: err.message
                });
            });
        }

        return {
            createContract: createContract
        };
    }());
}(this));