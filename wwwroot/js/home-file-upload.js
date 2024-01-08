$(function () {

    document.getElementById('passToWYPF').style.display = 'none';
        
    $("#PayLocationList").removeAttr("multiple");
    $(".text-danger").hide();  //## hides all validation tags no page load

           
    var previousSelectedPostType = $("#PreviousSelectedPostType").val();
    if (previousSelectedPostType !== '') {
        $("input[name=SelectedPostType][value=" + previousSelectedPostType + "]").attr('checked', 'checked');

        //## scroll the page to the bottom area.. if there is a success message- user can quickly click on the "next" button, or can see the list of Errors without scrolling..
        $('html, body').animate({
            scrollTop: $("#UploadFileInput").offset().top
        }, 500);
    }

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

        ShowOverlaySpinner();
        return isFormValid;
    });


    //$("#ValidateFileButton").click(function () {
    $("#UploadFileInput").change(function (e) {

        $("#overlay").fadeIn(300);
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

                    $("#UploadFileButton").addClass("disabled");    //## file is invalid, don't allow the user to Submit this page with invalid file..

                } else {
                    $("#UploadFileButton").removeClass("disabled"); //## Valid file- OK to submit the page and procees to next step                    
                    
                    $(".text-danger").hide();
                    $('#liveToast .toast-body').text("File check is complete and ready to upload now!");
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

