$(function () {
    
    $(".additional-payment-input").keyup(function (e) {
        if ($.isNumeric($(this).val()) == false) {
            $(this).val("0");
        } else {
            var deficit = parseFloat($('#Deficit').val());
            var yearEndBalance = parseFloat($('#YearEndBalance').val());
            var unfundedBenefits = parseFloat($('#FundedBenefits').val());
            var Miscellaneous = parseFloat($('#Miscellaneous').val());

            var additionalTotal = deficit + yearEndBalance + unfundedBenefits + Miscellaneous;
            additionalTotal = additionalTotal.toFixed(2);
            
            $('#DecifitTotalLblValue').text('£ ' + additionalTotal);

            var employersEmployeeTotalValue = $("#EmployersEmployeeTotalValue").text(); //## this is <h4> item
            var empVal = employersEmployeeTotalValue.replace('£', '').replace(',', '');

            var grandTotal = parseFloat(empVal) + parseFloat(additionalTotal);
            grandTotal = grandTotal.toFixed(2);
            $('#GrandTotalValue').text('£ ' + grandTotal);

        }
    });
    

    $("#SubmitNextButton").click(function (e) {
        if ($("#ChequeDateTBox").val() === '') {
            
            $("#ChequeDateTBox").parent().addClass("alert alert-danger");

            Swal.fire({
                icon: "error",
                title: "Date missing!",
                text: "You must select a date for Cheque payment.",
            });

            return false;
        }

        ShowOverlaySpinner();
    });
});
