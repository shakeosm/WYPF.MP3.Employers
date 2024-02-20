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
            additionalTotal = parseFloat(additionalTotal).toFixed(2);
            
            $('#DecifitTotalLblValue').text(additionalTotal);

            var employersEmployeeTotalValue = $("#EmployersEmployeeTotalValueRawFormat").val();

            var grandTotal = parseFloat(employersEmployeeTotalValue) + parseFloat(additionalTotal);
            
            if (additionalTotal < 1) {
                $('#GrandTotalValue').text(employersEmployeeTotalValue);
            } else {
                $('#GrandTotalValue').text(new Intl.NumberFormat('en-US').format(grandTotal) );
            }

            console.log("additionalTotal: " + additionalTotal + ",    employersEmployeeTotalValue: " + employersEmployeeTotalValue + ",      grandTotal: " + grandTotal);

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
