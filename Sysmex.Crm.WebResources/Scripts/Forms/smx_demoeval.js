/// <reference path="../Libraries/sonoma.js" />
var Xrm = window.Xrm || parent.window.Xrm;

(function (global) {
    'use strict';

    global.Sysmex = global.Sysmex || {};
    global.Sysmex.Forms = global.Sysmex.Forms || {};
    global.Sysmex.Forms.smx_demoeval = (function () {
        var ApproverFieldSet = [
            'smx_azd',
            'smx_nsd',
            'smx_rsd',
            'smx_tsg',
            'smx_svcops'
        ];

        var ClearApproverFieldSet = [
            'smx_rsdapproval',
            'smx_rsdapprover',
            'smx_rsdapprovaldate',
            'smx_tsgapproval',
            'smx_tsgapprover',
            'smx_tsgapprovaldate',
            'smx_svcopsapproval',
            'smx_svcopsapprover',
            'smx_svcopsapprovaldate',
        ]

        var StatusReasonCodes = {
            "Pending Extension": 180700002
		};

		var formContext;

        function attachEvents() {
            formContext.data.entity.addOnSave(populateApprovers);
            formContext.data.entity.addOnSave(setPendingExtension);
            formContext.data.entity.addOnSave(checkPendingExtension);

            var requestedDeliveryDate = formContext.getAttribute('smx_reqdelivinstall');
            if (requestedDeliveryDate) {
                requestedDeliveryDate.addOnChange(checkDeliveryDate);
            }
        }

		function onLoad(executionContext) {
			formContext = executionContext.getFormContext();

            attachEvents();
            disableApprovalFields();
            formContext.getControl("smx_contact").addPreSearch(filterContactLookup);
        }

        function disableApprovalFields() {
            for (var i = 0; i < ApproverFieldSet.length; i++) {
                var approvalField = ApproverFieldSet[i] + 'approval';
                var approvalAttribute = formContext.getAttribute(approvalField);

                // Confirm that attribute exists and is checked
                if (approvalAttribute && approvalAttribute.getValue()) {
                    var approvalControl = formContext.getControl(approvalField);
                    approvalControl.setDisabled(true);
                }
            }
        }

        function populateApprovers() {
            for (var i = 0; i < ApproverFieldSet.length; i++) {
                var approvalField = ApproverFieldSet[i] + 'approval';
                var approvalAttribute = formContext.getAttribute(approvalField);

                // Confirm that attribute exists, was changed, and is checked
                if (approvalAttribute && approvalAttribute.getIsDirty() && approvalAttribute.getValue()) {
                    setApprover(ApproverFieldSet[i] + 'approver');
                    setApprovalDate(ApproverFieldSet[i] + 'approvaldate');

                    var approvalControl = formContext.getControl(approvalField);
                    approvalControl.setDisabled(true);
                }
            }
        }

        function setApprover(field) {
            var approverAttribute = formContext.getAttribute(field);
            if (!approverAttribute) {
                return;
            }

            var userId = formContext.context.getUserId();
            var userName = formContext.context.getUserName();

            if (userId == null || userName == null) {
                return;
            }

            approverAttribute.setValue(
                [
                    {
                        id: userId,
                        name: userName,
                        entityType: 'systemuser'
                    }
                ]
            );
        }

        function setApprovalDate(field) {
            var approvalDateAttribute = formContext.getAttribute(field);
            if (!approvalDateAttribute) {
                return;
            }

            approvalDateAttribute.setValue(new Date());
        }

        function setPendingExtension() {
            var statusReason = formContext.getAttribute('statuscode');
            if (!statusReason) {
                return;
            }

            if (statusReason.getValue() === StatusReasonCodes["Pending Extension"]) {
                return;
            }

            var extendedPickUpDate = formContext.getAttribute('smx_extendedpickupdate');
            if (!extendedPickUpDate) {
                return;
            }

            if (extendedPickUpDate.getValue() != null) {
                statusReason.setValue(StatusReasonCodes["Pending Extension"]);
            }
        }

        function checkPendingExtension() {
            var statusReason = formContext.getAttribute('statuscode');
            if (!statusReason) {
                return;
            }

            var extendedPickUpDate = formContext.getAttribute('smx_extendedpickupdate');
            if (!extendedPickUpDate) {
                return;
            }

            if (statusReason.getValue() === StatusReasonCodes["Pending Extension"] && extendedPickUpDate.getValue() != null) {
                clearApproverFields();
            }
        }

        function clearApproverFields() {
            for (var i = 0; i < ClearApproverFieldSet.length; i++) {
                var attribute = formContext.getAttribute(ClearApproverFieldSet[i]);
                if (attribute) {
                    attribute.setValue(null);
                    formContext.getControl(ClearApproverFieldSet[i]).setDisabled(false);
                }
            }
        }

        function checkDeliveryDate() {
            var requestedDeliveryDate = formContext.getAttribute('smx_reqdelivinstall').getValue();
            if (requestedDeliveryDate == null) {
                return;
            }

            var dayOfWeek = requestedDeliveryDate.getDay();
            var reqdelivinstall = requestedDeliveryDate.setHours(0, 0, 0, 0);
            var today = new Date();
            today.setHours(0, 0, 0, 0);
            if ((dayOfWeek !== 2 && dayOfWeek !== 3 && dayOfWeek !== 4) || reqdelivinstall <= today) {
                Xrm.Navigation.openAlertDialog({text: 'Requested Delivery Date must be a Tuesday, Wednesday, or Thursday and in the future.'});
                formContext.getAttribute('smx_reqdelivinstall').setValue(null);
            }
        }

        function filterContactLookup() {
            var account = formContext.getAttribute("smx_account").getValue();

            if (account == null || account.length <= 0) {
                return;
            }

            var accountId = account[0].id;
            var fetchXml = [
                "<filter type='and'>",
                    "<condition attribute='parentcustomerid' operator='eq' value='" + accountId + "' />",
                    "<condition attribute='emailaddress1' operator='not-null' />",
                    "<condition attribute='telephone1' operator='not-null' />",
                "</filter>"
            ].join("");

            formContext.getControl("smx_contact").addCustomFilter(fetchXml);
        }

        return {
            onLoad: onLoad
        }
    }());
}(window));

