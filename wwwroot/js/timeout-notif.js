$(function () {

    //## warn the user after 2 mins- that their session will expire soon
    setTimeout(function () {
        ShowExpireyAlert()
        }, 30 * 1000);

    //## due to no activity for last 3 mins- log out the user..
    //setTimeout(function () {
    //    window.location = "/Login/SessionExpired";
    //}, 120 * 1000);


    function ShowExpireyAlert() {
        let timerInterval;
        Swal.fire({
            title: "Session expiry alert!",
            html: "Your session will be logged out in <b class='text-danger'></b> seconds.<br/><span class='text-primary h5 mt-3'>Click anywhere in the page to continue your current session.</span>",
            timer: 30000,   
            timerProgressBar: true,
            didOpen: () => {
                Swal.showLoading();
                const timer = Swal.getPopup().querySelector("b");
                timerInterval = setInterval(() => {
                    timer.textContent = `${Swal.getTimerLeft()/1000}`;
                }, 1000);
            },
            willClose: () => {
                clearInterval(timerInterval);
            }
        }).then((result) => {
            /* Read more about handling dismissals below */
            if (result.dismiss === Swal.DismissReason.timer) {
                console.log("I was closed by the timer");
            }
        });

    };



});