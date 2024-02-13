$(function () {

    document.getElementById('passToWYPF').style.display = 'none';
   
    $("#AutoMatchInitiateLink").click(function myfunction() {
        $.ajax({
            type: "GET",
            url: "/Home/InitialiseAutoMatchProcessByAjax",
            data: null,
            processData: false,
            contentType: false,
            success: function (response) {
                hideOverlaySpinner();

                if (response.isSuccess === true) {
                    $("#ShowMatchingResultDiv").html(response.message);
                    $("#ShowMatchingResultContainerDiv").removeClass("d-none")
                    $("#ProcessRunDiv").slideUp(400);

                    $('#liveToast .toast-body').text("Congratulations: All tasks completed succesfully!");
                    $('#liveToast').toast('show');
                } else {
                    Swal.fire(
                        'Failed!',
                        "Failed to execute the Auto_Match process.<br/><div class='alert alert-warning mt-3'>" + response.message + "</div>",
                        'error'
                    );

                    $("#ShowDashboardButtonOnAjaxFail").removeClass("d-none");
                    $(this).slideUp(300);   //## Hide the button on error.. no way to proceed.. so no reason to show it..
                    $("#ShowMatchingResultContainerDiv").slideUp(300);
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
    });

    
    

    function hideOverlaySpinner() {
        setTimeout(function () {
            $("#overlay").fadeOut(300);
        }, 500);
    }


});

