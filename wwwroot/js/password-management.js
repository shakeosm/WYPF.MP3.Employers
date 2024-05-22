$(function () {

    $("#ShowHidePasswordButton").click(function () {

        ShowHidePassword("Password");

    });

    $("#UpdatePasswordForm").on("submit", function (event) {
        var strengthMeterText = $("#PasswordStrengthText").text().toLowerCase();
        if (strengthMeterText.indexOf('strong') < 0) {
            console.log("Strength: " + strengthMeterText + " >> " + strengthMeterText.indexOf('strong'))
            Swal.fire(
                'Invalid Password!',
                "The new password must be at least 'Strong' category. Please try another password.",
                'error'
            );

            event.preventDefault();
            return false;
        }
    });

    //LoadMorePasswordSuggestionButton
    $("#LoadMorePasswordSuggestionButton").click(function () {
        LoadMorePasswordSuggestionButton("#SuggestedPasswordDiv");
    });

    function LoadMorePasswordSuggestionButton(targetDivName) {
        $.ajax({
            type: "GET",
            url: "/Profile/LoadSuggestedPassword/",
            success: function (response) {
                $(targetDivName).html(response);

                hideOverlaySpinner();
            },
            failure: function (response) {
                $(targetDivName).html(response);
                console.log(response.responseText);
                hideOverlaySpinner();
            },
            error: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            }
        }); //## end of Ajax
    }

    ///### Description: On Focus out event- check the password strength.
    $("#Password").blur(function () {
        if ($(this).val().length < 12) {
            $("#PasswordStrengthText").text("Weak");
            $("#PasswordStrengthText").next("div").remove();    // delete the progress bar.. whatever was there
        }
        else if ($(this).val() != "") {
            var apiUrl = $(this).attr("data-api-url");
            CheckPasswordStrength($(this).val(), apiUrl, "#PasswordStrengthMeter");
        } 

    });


    function CheckPasswordStrength(passwordToCheck, apiUrl, targetDivName) {        

        $.ajax({
            type: "GET",
            url: `${apiUrl}/${passwordToCheck}`,
            success: function (response) {

                $(targetDivName).html(response);
                //$(targetDivName).removeClass();
                //$(targetDivName).addClass("mx-2");
                hideOverlaySpinner();
            },
            failure: function (response) {
                $(targetDivName).html(response);
                console.log(response.responseText);
                hideOverlaySpinner();
            },
            error: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            }
        }); //## end of Ajax
    }

});