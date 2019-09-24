/// <reference path="../../Scripts/Libraries/jquery.js" />
/// <reference path="../../Scripts/Libraries/sonoma.js" />

var dialog = window.dialog || (function () {
    var dialogArguments;
    var dialogInterval;
    var globalXrm;

    var SelectProduct = {
        Existing: false,
        WriteIn: true
    };

    function handleFinish() {
        var lostReason = $('#reasonForLosing').val(),
            status = $('input:radio[name="product-method"]:checked').val();

        if (status === 'lostProducts' && lostReason === '') {
            alert("Please enter a value for the lost reason.");
            return;
        }

        var flaggedProducts = retrieveFlaggedProducts();
        var allProudctsSelected = calculateAllProductsSelected();

        var parameters = {
            Target: new Sonoma.OrgService.EntityReference(dialogArguments.entityId, dialogArguments.entityName),
            ProductString: flaggedProducts,
            AllProductsSelected: allProudctsSelected,
            Reason: lostReason,
            Status: status
        };

        displayLoadingAnimation(true);
        executeCustomAction('smx_SplitOpportunity', parameters);

        if (status === 'sendToContract' && allProudctsSelected) {
            globalXrm = parent.window.top.SharedXrmContext;
            updateStage();
        }
        else {
            displayLoadingAnimation(false);
            closeWindow();
        }
    }

    // Updates the business process flow stage
    function updateStage() {
        var currentStage = globalXrm.Page.data.process.getSelectedStage();
        if (currentStage.getName() === 'Final Paperwork') {
            displayLoadingAnimation(false);
            closeWindow();
            globalXrm.Page.data.refresh();
            return;
        }

        globalXrm.Page.data.process.moveNext(updateStage);
    }

    // Executes the Action Process and handles the result
    function executeCustomAction(requestName, parameters) {
        var result = Sonoma.OrgService.executeActionSync(requestName, parameters);

        if (result.Success) {
            if (result.Value.ClonedEntity !== '') {
                displayLoadingAnimation(false);
                Mscrm.Utilities.setReturnValue({
                    entityId: result.Value.ClonedEntity.Id,
                    status: parameters.Status,
                    cloned: true
                });
            }
        } else {
            console.error(result.Value);
            displayLoadingAnimation(false);
            closeWindow();
        }
    }

    // Parses the data query string, in order to pull out the entity id and entity logical name
    function parseQueryString(query) {
        var result = {};

        if (typeof query == "undefined" || query == null) {
            return result;
        }

        var queryparts = query.split("&");
        for (var i = 0; i < queryparts.length; i++) {
            var params = queryparts[i].split("=");
            result[params[0]] = params.length > 1 ? params[1] : null;
        }

        return result;
    }

    // Toggles the visibility of the loading animation overlay
    function displayLoadingAnimation(active) {
        $('#overlay').toggle(active);
        $('#loaderContainer').toggle(active);
    }

    // Attaches listeners to each button on the dialog
    function setUpButtons() {
        $("#sendToContract").click(function() {
            $("#lost-reason-description").toggle(false);
            $('#reasonForLosing').val(null);
        });
        $("#lostProducts").click(function() {
            $("#lost-reason-description").toggle(true);
        });
        $("#cancel").click(function () {
            closeWindow();
        });
        $("#finishButton").click(handleFinish);
    }

    // Builds html checkboxes and labels for all associated opportunity products
    function setUpProducts() {
        var opportunityProducts = retrieveOpportunityProducts();
        var xrmDialogContent = $('#product-list');

        for (var i = 0; i < opportunityProducts.length; i++) {
            var displayName;

            if (opportunityProducts[i].isproductoverridden.Value === SelectProduct.Existing) {
                displayName = opportunityProducts[i].productid.Name;
            }
            else if (opportunityProducts[i].isproductoverridden.Value === SelectProduct.WriteIn) {
                displayName = opportunityProducts[i].productdescription;
            }
            else {
                continue;
            }

            xrmDialogContent.append([
                '<div class="product-container">',
                    '<label for="', opportunityProducts[i].opportunityproductid.Value, '">',
                        '<input type="checkbox" value="', opportunityProducts[i].opportunityproductid.Value, '">',displayName,
                     '</label>',
                '</div>'
            ].join(''));
        }
    }

    // Returns all associated opportunity products with the current opportunity
    function retrieveOpportunityProducts() {
        var fetchXml = [
                '<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">',
                    '<entity name="opportunityproduct">',
                        '<attribute name="productid" />',
                        '<attribute name="productdescription" />',
                        '<attribute name="priceperunit" />',
                        '<attribute name="quantity" />',
                        '<attribute name="extendedamount" />',
                        '<attribute name="opportunityproductid" />',
                        '<attribute name="isproductoverridden" />',
                        '<order attribute="productid" descending="false" />',
                        '<link-entity name="opportunity" from="opportunityid" to="opportunityid" alias="ac">',
                            '<filter type="and">',
                                '<condition attribute="opportunityid" operator="eq" value="',dialogArguments.entityId,'" />',
                            '</filter>',
                        '</link-entity>',
                    '</entity>',
                '</fetch>'].join('');

        var result = Sonoma.OrgService.retrieveMultipleSync(fetchXml);

        if (result.Success) {
            return result.Value.Entities;
        } else {
            alert(result.Value);
        }
    }

    // Returns a comma-separated string containing the ids of all selected opportunity products
    function retrieveFlaggedProducts() {
        var productIds = '';
        var count = 0;

        $('input[type="checkbox"]').each(function() {
            if (this.checked) {
                if (count > 0) {
                    productIds = productIds.concat(',');
                }
                productIds = productIds.concat(this.value);
                count++;
            }
        });

        return productIds;
    }

    // Returns a boolean based on whether all opportunity product checkboxes were selected or not
    function calculateAllProductsSelected() {
        var totalProductCount = $('#product-list').find('input[type="checkbox"]').length;      
        var totalProductsSelected = $('#product-list').find('input[type="checkbox"]:checked').length;;

        return totalProductCount === totalProductsSelected;
    }

    return {
        executeOnLoad: function () {
            dialogArguments = parseQueryString(GetGlobalContext().getQueryStringParameters()['Data']);
            setUpButtons();
            setUpProducts();
        },
        handleFinish: handleFinish
    };
}());

$(document).ready(dialog.executeOnLoad);