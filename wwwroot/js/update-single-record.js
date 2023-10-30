
$(function () {
    var warningList = $("#warningListDiv").text().trim();
    var warningListItems = warningList.split(",")

    $.each(warningListItems, function (key, value) {
        $("." + value).addClass("alert alert-danger");
    });

    $(".acknowledge-alert-item").click(function () {
        var alertId = $(this).data("alertId");
        
        ApproveWarning($(this), alertId);        

    }); //## End: 'acknowledge-alert-item").click()

    function ApproveWarning(caller, alertId) {
        var formData = new FormData();
        formData.append('alertId', alertId);

        $.ajax({
            type: "POST",
            url: "/SummaryNManualM/WarningAcknowledge",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                hideOverlaySpinner();

                if (response.isSuccess === true) {
                    $(caller).closest("tr").addClass("alert-acknowledged");
                    $(caller).closest("td").find(".acknowledged-and-clear").removeClass("d-none");
                    $(caller).remove();

                    $('#liveToast .toast-body').text("The item is successfully acknowledged. Message: " + response.message);
                    $('#liveToast').toast('show');
                } else {
                    Swal.fire(
                        'Failed!',
                        "Failed to acknowledge the warning.<br/><div class='alert alert-warning mt-3'>" + response.message + "</div>",
                        'error'
                    );
                }
            },
            failure: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            },
            error: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            }
        }); // End: Ajax POST

    }
});