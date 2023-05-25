$(function () {
    document.getElementById('selectAll').onclick = function () {
        $("#selectAll").change(function () {
            $("input:checkbox").prop('checked', $(this).prop("checked"));
        });
    };

    document.getElementById('passToWYPF').style.display = 'block';

    if ($("#acknowledgeButton").length < 1) {
        $("#selectAllTH").remove();
    }

});