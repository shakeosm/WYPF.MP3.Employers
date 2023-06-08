$(function () {
    
    
    $("#PasswordPolicyDialogCloseButton").click(function () {
        //## we actually don't need this line of code, however- old Bootstrap3 isn't working well. So- doing it manually
        $("#passwordPolicyDialog").modal("hide");
    });

    $("#UploadFileInput").change(function (e) {
        const filess = e.target.files;
        $("#UploadFileButton").prop('disabled', filess.length < 1);

        
    });

    $("#UpdatePasswordForm #ShowPasswordPolicyModalIcon").click(function() {
        $("#passwordPolicyDialog").modal("show");
    });

    $("#UploadFileForm #PayLocationList").removeAttr("multiple");
    
    $("#UploadFileForm .text-danger").hide();  //## hides all validation tags no page load
    $("#UploadFileForm #UploadFileButton").click(function () {
        var isFormValid = true;
        $("#UploadFileForm .text-danger").hide();  //## hides all before applying validation.. and show them as required...

        if ($('input[name=SelectedMonth]:checked').val() == null) {
            $("#SelectedMonthValidationLabel").show();
            isFormValid = false;
        }

        if ($('input[name=SelectedYear]:checked').val() == null) {
            $("#SelectedYearValidationLabel").show();
            isFormValid = false;
        }

        if ($('input[name=SelectedPostType]:checked').val() == null) {
            $("#SelectedPostTypeValidationLabel").show();
            isFormValid = false;
        }

        if ($("#UploadFileInput").val() == '') {
            $("#CustomerFileValidationLabel").show();
            isFormValid = false;
        }

        return isFormValid;
    });


});

