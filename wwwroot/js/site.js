$(function () {
    
    
    $("#PasswordPolicyDialogCloseButton").click(function () {
        //## we actually don't need this line of code, however- old Bootstrap3 isn't working well. So- doing it manually
        $("#passwordPolicyDialog").modal("hide");
    });


    $("#ShowPasswordPolicyModalIcon").click(function () {
        $("#passwordPolicyDialog").modal("show");
    });

});

