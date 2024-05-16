$(function () {

    $("#passToWYPF").remove();

    var delay = 10000; //## 10 seconds
    var maxAttempts = 200; //## this will cover 200 x 10 Seconds.. ie: 2000 seconds = 33 minutes
    var errorIcon = "<i class='fas fa-exclamation-triangle mx-2'></i>";
    var successIcon = "<i class='fas fa-check-circle mx-2'></i>";
    
    /// Description:
    /// This will make the call and come back from API.. will not wait for the result..
    /// we will soon send another request after on every 10 seconds to see the proogress.
    /// We will get the RemittanceId from Session Cache
    $("#ReturnInitialiseStartButton").click(function() {
        var thisButton = $(this);
        var apiUrl = "/Home/InitialiseProcessCallOnly_Ajax";
        var remittanceId = $("#RemittanceId").val();
        var formData = new FormData();
        formData.append('id', remittanceId);
        
        $.ajax({
            type: "POST",
            url: apiUrl,
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                    
                $("#ReturnInitialiseProgressDisplayDiv").html(response);
                $("#ReturnInitialiseProgressDisplayDiv").removeClass("d-none")

                hideOverlaySpinner();

                //## don't make a new call immediately.. wait for a while and then start calling to see the progress..                
                setTimeout(KeepCheckingProgress_ReturnInitialise(), delay);
                

            },
            failure: function (response) {
                //console.log(response.responseText);
                hideOverlaySpinner();
                $(thisButton).text("Failed!");
            },
            error: function (response) {
                //console.log(response.responseText);
                hideOverlaySpinner();
                $(thisButton).text("Failed!");
            }
        }); // End: Ajax POST
    });
    

    async function KeepCheckingProgress_ReturnInitialise() {

        var stepStatusDivName = "#Step1_StatusDiv";
       
        var i = 1;       
        var currentStatus = 'processing';

        for (var i = 1; i <= maxAttempts; i++) {

            //## First check what is the current status beofre making a new call... maybe the last call has returned a 'COMPLETE' value- therefore we don't need to make anymore call at all...
            currentStatus = $(stepStatusDivName).text().toLowerCase();

            if (currentStatus != 'processing') {   //## Status values: one of PROCESSING, COMPLETE, FAILED, INVALID

                if (currentStatus == 'failed' || currentStatus == 'invalid') {
                    Show_ReturnInitialise_Failed_Message();
                } else {
                    $("#ReturnInitialiseStartButton").html(successIcon + "Task completed!");
                    $("#PendingProcessedIcon").html(successIcon);
                    $("#PendingProcessedCount").text($("#TotalRecordsInDatabase").val());
                }

                //## Show the 'Step2' button.. need to hide the dummy button
                $("#AutoMatchInitiateButton").removeClass("d-none");
                $("#AutoMatchInitiateLink_Pending").addClass("d-none");

                return false; // breaks
            }

            var apiResult = await CheckProgress_Periodically_Ajax("#ReturnInitialiseProgressDisplayDiv", "Step1");  //## this will make a call to the .Net to API to get the current status of the progress
            if (apiResult === false) {   //## for whatever reason- APi call crashed! then only show error..
                Show_ReturnInitialise_Failed_Message();
                return false; // breaks
            }

            //### Wait for the specified delay before processing the next item
            await sleep(delay);
        }


    }


    ///## Description: This will be used for both ReturnInitialise and Auto_Match process.. to get their status periodically..
    ///## coz on both process they will be reading values from the same table
    async function CheckProgress_Periodically_Ajax(targetDivToShowProgressBar, stepName) {

        var apiUrl = "/Home/CheckProgress_Periodically_ReturnInitialise_Ajax/" + stepName;

        //console.log("CheckProgress_Periodically_Ajax for: " + targetDivToShowProgressBar);
        console.log("apiUrl: " + apiUrl);

        $.ajax({
            type: "GET",
            url: apiUrl,
            //data: formData,
            processData: false,
            contentType: false,
            success: function (response) {

                $(targetDivToShowProgressBar).html(response);
                $(targetDivToShowProgressBar).removeClass("d-none")

                hideOverlaySpinner();
                console.log("call CheckProgress_Periodically_Ajax() success... ");                

            },
            failure: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
                return false;
            },
            error: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
                return false;
            }
        }); // End: Ajax POST
    }


    //###########################################################
    //#######           AutoMatch Initiate            ###########
    //###########################################################

    /// Description:
    /// This will make the call and come back from API.. will not wait for the AutoMatch result..
    /// we will soon send another request after on every 10 seconds to see the progress.
    /// We will get the RemittanceId from Session Cache
    $("#AutoMatchInitiateButton").click(function () {
        var thisButton = $(this);
        var apiUrl = "/Home/InitialiseAutoMatchProcess_CallOnly_Ajax";
        var remittanceId = $("#RemittanceId").val();
        //var formData = new FormData();
        //formData.append('id', remittanceId);

        $.ajax({
            type: "GET",
            url: apiUrl,
            //data: formData,
            processData: false,
            contentType: false,
            success: function (response) {

                $("#AutoMatchProgressDisplayDiv").html(response);

                hideOverlaySpinner();

                //## don't make a new call immediately.. wait for a while and then start calling to see the progress..
                setTimeout(KeepCheckingProgress_AutoMatch(), delay);

            },
            failure: function (response) {
                //console.log(response.responseText);
                hideOverlaySpinner();
                $(thisButton).text("Failed!");
            },
            error: function (response) {
                //console.log(response.responseText);
                hideOverlaySpinner();
                $(thisButton).text("Failed!");
            }
        }); // End: Ajax GET
    });



    async function KeepCheckingProgress_AutoMatch() {

        var stepStatusDivName = "#Step2_StatusDiv";
        var automatchButton = "#AutoMatchInitiateButton";

        var i = 1;        
        var currentStatus = 'processing';

        for (var i = 1; i <= maxAttempts; i++) {

            currentStatus = $(stepStatusDivName).text().toLowerCase();
            //## First check what is the current status beofre making a new call... maybe the last call has returned a 'COMPLETE' value- therefore we don't need to make anymore call at all...
            if (currentStatus != 'processing') {   //## Status values: one of PROCESSING, COMPLETE, FAILED, INVALID

                if (currentStatus == 'failed' || currentStatus == 'invalid') {
                    Show_AutoMatch_Failed_Message();

                } else {
                    $(automatchButton).html(successIcon + "Task completed!");
                    await sleep(3000); //## give some time to the user to see 'Task Completed' message before hiding the progress bar

                    $("#ShowMatchingResultDiv").html($("#Step2_MatchingResult").html());
                    $("#ShowMatchingResultContainerDiv").removeClass("d-none")
                    //$("#ProcessRunDiv").slideUp(1000);

                    $('#liveToast .toast-body').text("Congratulations: All tasks completed succesfully!");
                    $('#liveToast').toast('show');
                }


                return false; // breaks
            }

            //console.log("await KeepCheckingProgress_AutoMatch()... Processing attempt: ", i);

            var apiResult = await CheckProgress_Periodically_Ajax("#AutoMatchProgressDisplayDiv", "Step2");  //## this will make a call to the .Net to API to get the current status of the progress
            if (apiResult === false) {   //## for whatever reason- APi call crashed! then only show error..
                Show_AutoMatch_Failed_Message();
                return false; // breaks
            }

            //### Wait for the specified delay before processing the next item
            await sleep(delay);
        }

    }
    
    function sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    function Show_ReturnInitialise_Failed_Message() {
        Show_Process_Failed_Message("#ReturnInitialiseStartButton");        
        $("#Task_AutoMatchDiv").slideUp(500);
    }
    function Show_AutoMatch_Failed_Message() {
        Show_Process_Failed_Message("#AutoMatchInitiateButton");        
        var errorMessage = $("#Step2_ErrorMessageDiv").text();
        $("#ShowDashboardButtonOnAjaxFail").removeClass("d-none");
        $("#AutoMatchInitiateButton").slideUp(300);   //## Hide the button on error.. no way to proceed.. so no reason to show it..
        $("#Step2ProcessFailedNotification").removeClass("d-none");


        Swal.fire(
            'Failed!',
            "Failed to execute the Auto_Match process.<br/>" + errorMessage,
            'error'
        );

        //$("#Task_AutoMatchDiv").slideUp(500);
    }


    function Show_Process_Failed_Message(buttonName) {
        $(buttonName).html(errorIcon + "Task Failed!");
        $(buttonName).removeClass('btn-primary').addClass('btn-danger');
    }


});

