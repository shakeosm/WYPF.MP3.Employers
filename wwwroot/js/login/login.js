$(function () {
    
    var instance = $.fn.deviceDetector;

    $("#BrowserIdInput").val(instance.getBrowserId() + '-' + instance.getBrowserVersion());
    $("#WindowsIdInput").val( instance.getOsId() );

    $("#SendResetRequestButton").click(function () {        
        $("#PasswordRequestSentConfirmationDiv").removeClass("d-none");
        $("#ForgottenPasswordRequestDiv").slideUp(500);

    });


    $("#ForgottenPasswordButton").click(function() {
        $("#ForgottenPasswordDiv").removeClass("d-none");
        $("#LoginForm").slideUp(500);
    });
});