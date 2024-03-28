
$(function () {
    $('#UserActivityLogTable, #FilesNotProcessedTable, #FilesDoneListTable').DataTable({
        stateSave: true,
        searching: false,
        lengthChange: false

    });

    //show-activity-log-button
    $(".show-activity-log-button").click(function () {
        var fileName = $(this).attr("data-file-name");
        var params = { id: fileName };

        $.ajax({
            type: "GET",
            url: "/Admin/ShowUserActivityLogByAjax/" + fileName,
            //data: params,
            success: function (response) {
                hideOverlaySpinner();
                
                if (response.isSuccess === true) {
                    $("#UserActivityLogModalTitle").text(fileName);
                    $("#UserActivityLogPlaceholderDiv").html(response.message);
                    $("#UserActivityLogPopupModal").modal('show');
                    
                } else {
                    Swal.fire(
                        'Failed!',
                        "Failed to load file.<br/><div class='alert alert-warning mt-3'>" + response.message + "</div>",
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
        });

    });
    
});