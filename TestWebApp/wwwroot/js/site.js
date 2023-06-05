// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function onSendClick() {
    console.log("onSendClick function called")

    var sendText = document.getElementById("txt-send").value
    var data = escape(sendText)
    console.log("invoking native method with data: " + data)
    //var request = {
    //    version: 1,
    //    payload: data
    //}
    //invokeNative(request)
    invokeNative(data)
}