$(function () {
    

    //$("#ShowPasswordPolicyModalButton").click(function() {
    //    $("#passwordPolicyDialog").modal("show");
    //});
    

    $("#SubmitFormButton").on("click", function (event) {

        event.preventDefault();

        var form = $("#RegisterUserForm");
        form.validate();
        if (form.valid()) {
            $("#LoadingSpinnerDiv").removeClass("d-none");
            $(this).addClass("disabled");

            //## add a delay.. then POST the form...
            console.log(".. adding a 2 sec delay...");
            setTimeout(
                function () {
                    PostRegisterUserForm();
                }, 2000);

        } else {
            alert("Error -> form.valid()");
            return;
        }

    });
});


function PostRegisterUserForm()
{
    var userId = $("#UserId").val();
    var password = $("#Password").val();
    
    var formData = new FormData();
    formData.append('UserId', userId);
    formData.append('Password', password);

    $.ajax({
        type: "POST",
        url: "/Login/RegisterUserWithNewPassword",
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            $("#SubmitFormButton").removeClass("disabled");
            console.log("ajax result: " + response);
            if (response.isSuccess === true) {
                //## show success Div
                $("#RegistrationSuccessMessageDiv").html(response.message);
                $("#UserRegistrationSuccessDiv").removeClass("d-none");
                //## hide Password change div...
                $("#UserRegistrationDiv").slideUp(300);
            } else {
                Swal.fire(
                    'Failed!',
                    "Failed to Register user.<br/><div class='alert alert-warning mt-3'>" + response.message + "</div>",
                    'error'
                );     

                $("#LoadingSpinnerDiv").addClass("d-none");
            }

        },
        failure: function (response) {
            console.log(response.responseText);
        },
        error: function (response) {
            console.log(response.responseText);
        }
    });

}