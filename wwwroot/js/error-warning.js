$(function () {

    //## if it has 11+ Rows.. (1 TH + 10 TR)
    if ($("#ErrorsAndWarningsListTable tr").length > 11) {
        $('#ErrorsAndWarningsListTable').DataTable({
            stateSave: true,
        });
    }

    //## This will collapse the current Div- (showing list of Errors in each category) and show the List of all Main Errors/Warnings
    //$(document).on('click', '#BackToErrorsAndWarningsListButton', function (event) {
    //    event.preventDefault();
    //    $("#AlertListModalContentsWrapper").slideUp(300);           //## Child table - will Collapse
    //    $("#ErrorsAndWarningsListTable_wrapper").slideDown(300);    //## Parent Table - will show
    //    $("#AlertListModalContentsWrapper").addClass("d-none");     //## Child table
       
    //    ScrollToPageBottom();
    //});


    //## This will collapse the "Main Errors/Warnings"" Div- and show child items in each Error/Warnings
    //$(".view-alert-list-button").click(function (event) {
    $(document).on('click', '.view-alert-list-button', function (event) {

        event.preventDefault();

        var targetParentCounterDivIdName = "#AlertGroupAlertCounter_" + $(this).data("groupId"); //## This will store the id of the counter div of this Error group- on 'View' button click
        $("#ParentGroupCounterDivId").text(targetParentCounterDivIdName);   //## keep this TargetId here- we will update the values while clearing the Error/Warnings

        var remittanceId = $(this).data("remittanceId");
        var alertType = $(this).data("alertType");

        //console.log("remittanceId; " + remittanceId + ", alertType: " + alertType);

        var params = { remittanceId: remittanceId, alertType: alertType };

        $.ajax({
            type: "GET",
            url: "/ErrorWarning/AlertListByAjax",
            data: params,
            success: function (response) {                
                $("#AlertListModalContentsArea").html(response);    

                //## Enable the DataTable feature- if only more than 11 Rows.. (1TH + 10 TR)
                if ($('#WarningListTable tr').length > 11) {
                    $('#WarningListTable').DataTable({
                        stateSave: true,
                    });
                }

                $("#AlertListModal").modal("show");
                //$("#AlertListModalContentsWrapper").slideDown(300);         //## Child table - will show
                //$("#ErrorsAndWarningsListTable_wrapper").slideUp(300);    //## Parent Table - will Collapse
                //$("#AlertListModalContentsWrapper").removeClass("d-none");//## Child table

                hideOverlaySpinner();
            },
            failure: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            },
            error: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            }
        });

    });

    //## This is for the child items inside the Error/Warning group. Each group may have 1+ errors. To view them individually- we click on this button 
    //##    which will load the details in another window...
    $(document).on('click', '.view-alert-details-button', function () {
        
        $(this).closest("tr").addClass("error-item-viewed");
        var currentTotal = $("#AlertSubListAlertCounter").text();
        if (currentTotal !== "0") {
            var newTotal = $("#AlertSubListAlertCounter").text() - 1;
            $("#AlertSubListAlertCounter").text(newTotal);

            //## now update the parent counter div..
            var targetParentCounterDiv = $("#ParentGroupCounterDivId").text();
            $(targetParentCounterDiv).text(newTotal);
        }
       
    });
});

function ScrollToPageBottom() {
    setTimeout(function () {
        $("html, body").animate({ scrollTop: $(document).height() }, 200);
    }, 300);

};