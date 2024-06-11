$(function () {
    
    var instance = $.fn.deviceDetector;

    $("#BrowserIdInput").val(instance.getBrowserId() + '-' + instance.getBrowserVersion());
    $("#WindowsIdInput").val( instance.getOsId() );

    

    /* Show Div to enter User Id to request a Password reset link  */
    $("#ForgottenPasswordButton").click(function() {
        $("#ForgottenPasswordDiv").removeClass("d-none");
        $("#LoginForm").slideUp(500);
    });

    $("#BackToLoginButton").click(function() {
        $("#LoginForm").slideDown(500);
        $("#ForgottenPasswordDiv").addClass("d-none");
    });

    /* Send a Password Reset Link to the user  */
    $("#SendResetRequestButton").click(function () {

        var userId = $("#ForgottenUserId").val();

        //## first remove any previous error message from the Form.. and show later if required..
        $(".validation-alert").addClass("d-none");
        $("#ForgottenPasswordDiv #ForgottenUserId").removeClass("border border-danger");

        if (userId === '') {
            $("#ForgottenPasswordDiv .validation-alert").removeClass("d-none");
            $("#ForgottenPasswordDiv #ForgottenUserId").addClass("border border-danger");
            return;
        }

        SubmitForResetRequestLink(userId);       

    });

    function SubmitForResetRequestLink(userId) {
        $.ajax({
            type: "GET",
            url: `/Login/SendResetRequestLink/${userId}`,
            data: null,
            success: function (response) {
                //hideOverlaySpinner();

                if (response.isSuccess === true) {
                    $("#SendingWaitingButton").removeClass("d-none");
                    $(this).addClass("d-none");

                    console.log("Reset link is successfully sent to the email we have for this user id.");

                    setTimeout(function () {
                        $("#SendingWaitingButton").addClass("d-none");
                        $(this).removeClass("d-none");
                    }, 2000);

                    $("#ForgottenPasswordDiv").slideUp(500);
                    $("#PasswordRequestSentConfirmationDiv").removeClass("d-none");
                    $("#PasswordRequestSentConfirmationDiv .success-message").removeClass("d-none");



                } else {
                    Swal.fire({
                        title: "Failed!",
                        text: response.message,
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