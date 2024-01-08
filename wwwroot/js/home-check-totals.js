$(function () {

    var textBox1 = $('#Deficit').keyup(UpdateTotal);
    var textBox2 = $('#YearEndBalance').keyup(UpdateTotal);
    var textBox3 = $('#FundedBenefits').keyup(UpdateTotal);
    var textBox4 = $('#Miscellaneous').keyup(UpdateTotal);
    var gtotal = $('#GrandTotalValue').text();

    function validate(key) {
        //getting key code of pressed key
        var a1, a2, a3, a4, total, gtotal, pp;
        var keycode = (key.which) ? key.which : key.keyCode;
        var phn = document.getElementById('Deficit');
        var phn1 = document.getElementById('YearEndBalance');
        var phn2 = document.getElementById('FundedBenefits');
        var phn3 = document.getElementById('Miscellaneous');

        //comparing pressed keycodes
        if (keycode == 45 && (phn.value == "" || phn1.value == "" || phn2.value == "" || phn3.value == "")) {

            return true;
        }
        if (!(keycode == 8 || keycode == 46) && (keycode < 48 || keycode > 57)) {

            return false;
        }
    }

    //var employersEmployeeTotalValue = $('#EmployersEmployeeTotalValue').val();

    var employersEmployeeTotalValue = document.getElementById("EmployersEmployeeTotalValue").innerText;
    var empVal = employersEmployeeTotalValue.replace('£', '');
    //var employersEmployeeTotalValue_int = parseInt(empVal, 10);
    var gt = gtotal.replace('£', '');


    function UpdateTotal() {
        var value1 = textBox1.val();
        var value2 = textBox2.val();
        var value3 = textBox3.val();
        var value4 = textBox4.val();
        var sum = add(value1, value2, value3, value4);
        var sum = sum.toFixed(2);

        //$('input:label[id$=DedifitTotalLblValue]').val(sum);
        $('#DecifitTotalLblValue').text('£' + sum);
        //var myTotal = parseFloat(empVal) + parseFloat(sum) + parseFloat(gt);
        var myTotal = parseFloat(empVal) + parseFloat(sum);
        myTotal = myTotal.toFixed(2);
        $('#GrandTotalValue').text('£' + myTotal);
    }

    function add() {
        var sum = 0;
        for (var i = 0, j = arguments.length; i < j; i++) {

            if (IsNumeric(arguments[i])) {

                sum += parseFloat(arguments[i]);
            }
        }
        return sum;
    }

    function IsNumeric(input) {

        return (input - 0) == input && input.length > 0;

    }

    $("#SubmitNextButton").click(function () {
        ShowOverlaySpinner();
    });
});
