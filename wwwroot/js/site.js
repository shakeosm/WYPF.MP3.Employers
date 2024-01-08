$(function () {


    $('.click-and-wait-button').on("click", function () {

        $(this).addClass("disabled");
        $(this).html("<span class='spinner-border spinner-border-sm' role='status' aria-hidden='true'></span><span class='mr-2'>&nbsp;Processing...</span>")


        setTimeout(() => {
            console.log("Delayed for half second.");
        }, "5000");
    });

    $(document).ajaxSend(function () {
        $("#overlay").fadeIn(300);
    });    
    
    $("#PasswordPolicyDialogCloseButton").click(function () {
        //## we actually don't need this line of code, however- old Bootstrap3 isn't working well. So- doing it manually
        $("#passwordPolicyDialog").modal("hide");
    });


    $("#ShowPasswordPolicyModalIcon").click(function () {
        $("#passwordPolicyDialog").modal("show");
    });

    //## This button is a short-circuit for Employers to pass a Remittance file to WYPF, when they fee lazy!
    $("#passToWYPF").on("click", function () {
        
        Swal.fire({
            title: "Are you sure?",
            text: "Please confirm to submit the file to WYPF! You cannot revert this action.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#3085d6",
            cancelButtonColor: "#d33",
            confirmButtonText: "Submit!"
        }).then((result) => {
            if (result.isConfirmed) {
                SubmitForProcessingAjax();

            }
        });

    });

    function SubmitForProcessingAjax() {
        $.ajax({
            type: "GET",
            url: '/Admin/SubmitForProcessingAjax',
            data: null,
            success: function (response) {
                hideOverlaySpinner();

                if (response.isSuccess === true) {
                    Swal.fire({
                        title: "Submitted!",
                        text: response.message,
                        icon: "success"
                    });

                    window.location.href = "/Admin/Home";

                } else {
                    Swal.fire({
                        title: "Failed!",
                        text: "Operation failed to pass the remittance file to WYPF. Please contact WYPF-Admin.",
                        icon: "error"
                    });
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
    };

});


function hideOverlaySpinner() {
    setTimeout(function () {
        $("#overlay").fadeOut(300);
    }, 500);
}

                

function ShowOverlaySpinner() {
    $("#overlay").fadeIn(300);
}

