$(function () {

    document.getElementById('passToWYPF').style.display = 'none';
        
    $("#PayLocationList").removeAttr("multiple");
    $(".text-danger").hide();  //## hides all validation tags no page load

    //$("#UploadFileInput").change(function (e) {
    //    const filess = e.target.files;
    //    $("#UploadFileButton").prop('disabled', filess.length < 1);
    //});

        
    

    $("#UploadFileButton").click(function () {
        var isFormValid = true;
        $(".text-danger").hide();  //## hides all before applying validation.. and show them as required...

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


    //$("#ValidateFileButton").click(function () {
    $("#UploadFileInput").change(function (e) {

        //## first clear any previous error
        $(".validation-message-div").removeClass("alert alert-danger"); //## there are 2 Error display divs.. so- clear both in one go..
        $(".validation-message-div").text("");
        $("#CustomerFileValidationLabel").hide();

        var customerFile = $('#UploadFileInput')[0].files[0];
        
        var formData = new FormData();
        formData.append('formFile', customerFile);

        $.ajax({
            type: "POST",
            url: "/Home/ValidateFile",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                console.log("ajax result: " + response);
                if (response !== 'success') {
                    $("#FileValidationCheckResult").addClass("alert alert-danger");
                    $("#FileValidationCheckResult").html(response);

                    $("#UploadFileButton").prop("disabled", true);  //## file is invalid, don't allow the user to Submit this page with invalid file..
                } else {
                    $("#UploadFileButton").prop("disabled", false); //## Valid file- OK to submit the page and procees to next step                    
                    $('#liveToast').toast('show');  //## show the user a Bootstrap Toast message about the success
                }

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

    function hideOverlaySpinner() {
        setTimeout(function () {
            $("#overlay").fadeOut(300);
        }, 500);
    }


});

