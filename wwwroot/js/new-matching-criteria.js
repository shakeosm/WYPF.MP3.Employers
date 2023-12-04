//var textArea = document.getElementById("noteArea").value;
//pass that text area value to all the notes values
//document.getElementById("note").innerHTML = textArea;

//enable disable submit button on radio button click.
function validate() {
    //alert("1");
    if ($("#activeProcess:checked").val() != "") {
        $("#NonBillableReason").attr("disabled", false);
        //alert("2");
    }

    if ($("#activeProcess:checked").val() == "") {
        $("#NonBillableReason").attr("disabled", false);
        //alert("3");
    }
}
$(document).ready(function () {
    validate();
    $('input').change(validate);

    $(".matching-folder-radio-btn").click(function () {
        //## first remove the class for the previously selected RadioOption- check all tables, all rows..
        $(".person-folder-matching-table tr").removeClass("table-primary");

        //$(this).nearest("tr").addClass("table-primary");
        //## Now apply the CSS to the selected row...
        $(this).parents('tr').addClass("table-primary");
    });
});

function myFunction() {
    var x = document.getElementById("popUp");
    if (x.style.display === "none") {
        x.style.display = "block";
    } else {
        x.style.display = "none";
    }
}

$(document).ready(function () {
    $(document).on('change', 'input:radio[id^="LooseMatch"]', function (event) {
        //alert("click fired");
    });
});
function clickFunction() {
    var x = document.getElementById("btnSubmit");
    if (x.style.display === "none") {
        x.style.display = "block";
    }
}

$("#btnSubmit").click(function (e) {
    var selectedFolderOption = $('input[name="ActiveProcess"]:checked').val();
    if (selectedFolderOption == null) {
        Swal.fire({
            icon: 'error',
            title: 'No matching record...?',
            text: 'You must select a matching Folder record.',
        })

        e.preventDefault();
        return;
    }

});
