$(function () {
    $('#PendingSubmissionTable').DataTable();
    
    $('#CompletedSubmissionTable').DataTable({
        order: [[2, 'desc']],
    });
    

    $("#PendingSubmissionTable .ShowSubmissionDetailsButton").click(function () {
        
        var params = { remittanceId: $(this).attr("data-remittance-id") };
        var modalTitle = $(this).attr("data-modal-title");

        $.ajax({
            type: "GET",
            url: "/Admin/GetSubmissionDetails",
            data: params,
            success: function (response) {                              

                $("#SubmissionDetailModalTitle").text(modalTitle);
                $("#SubmissionDetailPlaceholderDiv").html(response);
                $("#SubmissionDetailPopupModal").modal("show");
            },
            failure: function (response) {
                console.log(response.responseText);
            },
            error: function (response) {
                console.log(response.responseText);
            }
        });

    });

    $(".submission-detail-modal-close-button").click(function () {
        $("#SubmissionDetailPopupModal").modal("hide");
    });

});