$(function () {

    $('#WarningListTable').DataTable({
        stateSave: true
    });

    if ($("#UpdateStatusAlertDiv").length) {
        setTimeout(function () {
            $("#UpdateStatusAlertDiv").slideUp(500).remove();
        }, 4000);

    }

    $(document).on('click', '.reset-record-button', function () {

        Swal.fire({
            title: 'Are you sure?',
            text: "You won't be able to revert this!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, reset it!'
        }).then((result) => {
            if (result.isConfirmed) {

                var recordId = $(this).attr("data-id");

                var formData = new FormData();
                formData.append('id', recordId);
                console.log("reset-record-button.click(), recordId: " + recordId);

                ProceedToReset(formData, $(this));
            }
        });


    });

    function ProceedToReset(formData, callerTD) {

        $.ajax({
            type: "POST",
            url: "/ErrorWarning/ResetRecord",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                hideOverlaySpinner();
                console.log("response: " + response);
                if (response === 'success') {

                    $(callerTD).closest("tr").removeClass("table-success");
                    $(callerTD).closest("td").find(".view-button").removeClass("d-none");
                    $(callerTD).slideUp();

                    $('#liveToast .toast-body').text("The changes are reset to original state.");
                    $('#liveToast').toast('show');  //## show the user a Bootstrap Toast message about the success
                    var totalAlerts = $("#TotalErrorCountSpan").text();
                    $("#TotalErrorCountSpan").text(parseInt(totalAlerts) + 1);

                } else {
                    Swal.fire(
                        'Reset!',
                        "Failed to reset. <br/>" + response,
                        'Fail'
                    )
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
        });
    }

});