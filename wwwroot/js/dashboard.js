$(function () {
    $('#PendingSubmissionTable').DataTable({
        stateSave: true,
    });
    
    $('#CompletedSubmissionTable').DataTable({
        stateSave: true,
        order: [[2, 'desc']],
    });
    
    $(document).on('click', '#PendingSubmissionTable .ShowSubmissionDetailsButton', function () {
        var params = { remittanceId: $(this).closest("tr").attr("data-remittance-id") };
        var modalTitle = $(this).closest("tr").attr("data-modal-title");

        $.ajax({
            type: "GET",
            url: "/Admin/GetSubmissionDetails",
            data: params,
            success: function (response) {                              

                $("#SubmissionDetailModalTitle").text(modalTitle);
                $("#SubmissionDetailPlaceholderDiv").html(response);

                if ($("#SubmissionDetailsTable tr").length > 11) {  //## Length means total (TH+TR). If more than 11 then enable the DataTable
                    $("#SubmissionDetailsTable").DataTable({
                        order: [[4, 'desc']],   //## order by 'Score-WYPF' column
                    });
                }

                $("#SubmissionDetailPopupModal").modal("show");
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

    $(document).on('click', '#PendingSubmissionTable .score-history-button', function () {
            
        var params = { remittanceId: $(this).closest("tr").attr("data-remittance-id") };
        var modalTitle = $(this).closest("tr").attr("data-modal-title");

        $.ajax({
            type: "GET",
            url: "/Admin/GetScoreHistoryPartialView",
            data: params,
            success: function (response) {                              

                $("#SubmissionDetailModalTitle").text(modalTitle);
                $("#SubmissionDetailPlaceholderDiv").html(response);
                $("#SubmissionDetailPopupModal").modal("show");
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

    //## submit-return-button.Click()
    //submit-return-button
    //$(document).on('click', '.submit-return-button', function () {
    $('.submit-return-button').on("click", function () {
        console.log('click, .submit-return-button');

        $(this).prop('disabled', true);
        $("#overlay").fadeIn(300);  //## let this load the 'Loading-Spinner' so the user won't click on the button again- and calling the same proc - twice
        setTimeout(() => {
            console.log("Delayed for 1 second.");
        }, "500");
    });


    //###   This is to update the 'Score History' while already showing the current score in the Popup Modal
    $(document).on('click', '#UpdateScoreButton', function () {

        var params = { remittanceId: $(this).attr("data-remittance-id") };

        $.ajax({
            type: "GET",
            url: "/Admin/GetScoreHistoryPartialView",
            data: params,
            success: function (response) {

                $("#SubmissionDetailPlaceholderDiv").html(response);
                $("#SubmissionDetailPopupModal").modal("show");
                $("#ModalStatusText").removeClass("d-none");
                $("#ModalStatusText").fadeIn(500).fadeOut(3000)
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

    $(".submission-detail-modal-close-button").click(function () {
        $("#SubmissionDetailPopupModal").modal("hide");
    });



    $(document).on('click', '#PendingSubmissionTable .delete-remittance-button', function (e) {
        var remittanceId = $(this).closest("tr").attr("data-remittance-id");
        
        Swal.fire({
            title: 'Are you sure?',
            text: "You won't be able to revert this!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, delete it!'
        }).then((result) => {
            if (result.isConfirmed) {               
                DeleteRemittanceById($(this));    //## ajax call to Delete the selected Remittance record

            }
        })
    });

    function DeleteRemittanceById(caller) {
        var remittanceId = $(caller).closest("tr").attr("data-remittance-id");

        var formData = new FormData();
        formData.append('id', remittanceId);

        console.log("From function DeleteRemittanceById, remittanceId: " + remittanceId);

        $.ajax({
            type: "POST",
            url: "/Admin/DeleteRemittanceAjax",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                hideOverlaySpinner();

                $(caller).closest("tr").remove(); //## remove the row.. no need to reload the page..
                Swal.fire(
                    'Deleted!',
                    "The delete operation is successful.<br/><div class='alert alert-success mt-3'>" + response + "</div>",
                    'success'
                )

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