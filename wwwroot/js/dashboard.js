$(function () {
    $('#PendingSubmissionTable').DataTable();

    $(".ShowSubmissionDetailsButton").click(function () {
        
        var params = { remittanceId: $(this).attr("data-remittance-id") };
        var modalTitle = $(this).attr("data-modal-title");

        //console.log("modalTitle: " + modalTitle);

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

        //$.get('/Admin/GetSubmissionDetails/', params, onSuccessShowSubmissionDetail);//, datatype);
    });

    $(".submission-detail-modal-close-button").click(function () {
        $("#SubmissionDetailPopupModal").modal("hide");
    });

    function onSuccessShowSubmissionDetail(response, status) {
        // do something here 
        console.log("response: " + response);
        $("#SubmissionDetailPlaceholderDiv").html(data);
    }
});