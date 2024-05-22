$(function () {
    
    var instance = $.fn.deviceDetector;

    $("#BrowserIdInput").val(instance.getBrowserId() + '-' + instance.getBrowserVersion());
    $("#WindowsIdInput").val( instance.getOsId() );


});