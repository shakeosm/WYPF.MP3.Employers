$(function () {

    $(document).on('click', '#TupeRecordsContainer .tupe-member-record-show-button', function (e) {
        var alreadyLoaded = $(this).data("loaded-already");
        if (alreadyLoaded === 'true') {
            //## do nothing... 
            console.log("data was loaded previously.. ");
            return;
        }

        $(this).data("loaded-already",'true');
        var locationCode = $(this).data("paylocation-code");
        var tupeType = $(this).data("tupe-type");
        var tupeDate = $(this).data("tupe-date");
        var targetDiv = $(this).data("target-body");

        TupePayLocations_ShowAllMembers(locationCode, tupeDate, tupeType, targetDiv);

        //function TupePayLocations_ShowAllMembers(payLocationCode, tupedate, tupeType, targetDiv) {

        //axios
        //    .post(url, payload)
        //    .then((response) => console.log(response))
        //    .catch((error) => console.error(error));

        // Send a POST request
        //axios({
        //    method: 'post',
        //    url: '/user/12345',
        //    data: {
        //        firstName: 'Fred',
        //        lastName: 'Flintstone'
        //    }
        //})
        //    .then((response) => console.log(response))
        //    .catch((error) => console.error(error));

    });


    //######## Description: This even will be used by 3 buttons- "None / All / Selected Only"
    //######## We need to acknowledge all user response- All and None are easy. 
    //######## If 'Selected' then we will have to build a list of selected Checkbox values and pass it onto C# Api
    $(document).on('click', '#TupeRecordsContainer .tupe-member-acknowledge-in', function (e) {
        var thisTableId = '#' + $(this).parent().find(".tupe-table-name").val();
        var ackType = $(this).data("tupe-ack-type");

        var selectedValues = "";
        var paylocationCode = $(this).parent().find(".tupe-paylocation-code").val();
        var tupeDate = $(this).parent().find(".tupe-date").val();

        console.log("ackType: " + ackType + " >> thisTable: " + thisTableId + " >> paylocationCode: " + paylocationCode + " >> tupeDate: " + tupeDate + " >> selectedValues: " + selectedValues);

        if (ackType === 'selected') {
            $(thisTableId + ' .confirm-tupe-member-check').each(function () {
                selectedValues += (this.checked ? $(this).val() + "," : "");

            });

            if (selectedValues.length < 5) {
                Swal.fire({
                    title: "Error!",
                    text: "You must select at least one member record.",
                    icon: "error"
                });

                return;
            }

        } else if (ackType === 'all') {
            //## when clicked ALL
            $(thisTableId + ' .confirm-tupe-member-check').prop("checked", true);
            $(thisTableId + ' .confirm-tupe-member-check').each(function () {
                selectedValues += $(this).val() + ",";
            });
            //$(this).parent().parent().find(".show-success-message").removeClass("d-none");
            //$(this).parent().remove();

        } else {
            //## when clicked NONE- NO Need to call API.. just remove the Table and show success message.. no action is required..
            $(this).parent().parent().find(".show-success-message").removeClass("d-none");
            $(this).parent().remove();
            $(thisTableId).slideUp();

            return;
        }

        //## now disable all the checkboxes
        $(thisTableId + ' .confirm-tupe-member-check').prop("disabled", true);
        
        //## build a dataObject - comma separated with all required values, eg: locationCode + Date + TupeType + selectedMembers
        //var paramValues = paylocationCode + ";" + tupeDate + ";IN;" + ackType + ";" + selectedValues;
        TupePayLocations_Create_Alert(paylocationCode, tupeDate, "IN", ackType, selectedValues, thisTableId, $(this));
    });


    $(document).on('click', '#TupeRecordsContainer .confirm-tupe-button', function (e) {
        var tupeType = $(this).data("tupe-type");

        Swal.fire({
            title: 'Confirm Tupe ' + tupeType,
            text: "Please confirm - the selected PayLocations and Dates have valid Tupe member records.",
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, update!'
        }).then((result) => {
            if (result.isConfirmed) {
                var tableElementName = "#tupeTable_" + tupeType + " .tupe-item-check";
                //var rows = $("#tupeTable_" + tupeType + " tr").length;

                var dataObject = "";
                var rowItem = "";
                //for (var i = 1; i <= rows; i++) {
                $(tableElementName).each(function (i, obj) {
                    //var checkItem = $("#tupeTable_" + tupeType).find('tr').eq(i).find(".tupe-item-check");

                    var paylocationCode = $(this).data("paylocation-code");
                    var tupeDate = $(this).data("tupe-date");
                    var isTupe = $(this).is(":checked");
                    rowItem = paylocationCode + "," + tupeDate + "," + isTupe + "," + tupeType;

                    //console.log("Processing=> paylocationCode: " + paylocationCode + ", tupeDate: " + tupeDate + ", isTupe: " + isTupe);
                    dataObject = dataObject + rowItem + ";";
                    
                });         

                TupePayLocations_Create_Alert(dataObject, tableElementName, this);                

                $('#liveToast .toast-body').text("Tupe selection is successfully saved.");
                $('#liveToast').toast('show');


            }
        })
    });

    //function TupePayLocations_Create_Alert(paylocationCode, tupeDate, tupeType, isTupe, sourceTableElementName) {
    function TupePayLocations_Create_Alert(paylocationCode, tupeDate, tupeType, ackType, selectedValues, sourceTableElementName, callerBtn) {
        var apiUrl = "/home/tupe-paylocation-alert";
        var formData = new FormData();
        formData.append('locationCode', paylocationCode);
        formData.append('tupeDate', tupeDate);
        formData.append('tupeType', tupeType);
        formData.append('acknowledgementType', ackType);
        formData.append('recordIdList', selectedValues); 
        
        $.ajax({
            type: "POST",
            url: apiUrl,
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                hideOverlaySpinner();

                if (response.isSuccess === true) {
                    console.log(response.message);
                    //$(sourceTableElementName).closest("td").addClass("bg-success");
                    $(sourceTableElementName).attr("disabled", true);

                    //$(callerBtn).parent().find(".show-success-message").removeClass("d-none");
                    //$(callerBtn).addClass("d-none");

                    $(callerBtn).parent().parent().find(".show-success-message").removeClass("d-none");
                    $(callerBtn).parent().remove();

                } else {
                    console.log("Error: Failed to creat Alert for: " + paylocationCode + " / " + tupeDate + " / " + tupeType + " / " + isTupe);
                }
            },
            failure: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            },
            error: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            }
        }); // End: Ajax POST -> TupePayLocations_Create_Alert()
    }


    function TupePayLocations_ShowAllMembers(payLocationCode, tupedate, tupeType, targetDiv) {
        var apiUrl = "/home/tupe-paylocation-show-members";
        var formData = new FormData();
        formData.append('payLocationCode', payLocationCode);
        formData.append('tupeType', tupeType);
        formData.append('tupeDate', tupedate);

        $.ajax({
            type: "POST",
            url: apiUrl,
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                hideOverlaySpinner();
                $("#" + targetDiv).html(response);
            },
            failure: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            },
            error: function (response) {
                console.log(response.responseText);
                hideOverlaySpinner();
            }
        }); // End: Ajax POST -> TupePayLocations_Create_Alert()
    }

});