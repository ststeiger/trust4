function DoRequest(request, func)
{
    var client = new XMLHttpRequest();
    client.open("GET", "/autoconfig/ajax/" + request, true);
    client.onreadystatechange = function() { func(client.readyState, client.responseText); };
    client.send(null);
}

function StartAutoConfig()
{
    document.getElementById("address").className = "working";
    DoRequest("address", ACStage1Result);
}

function ACFailed(stage)
{
    document.getElementById(stage).className = "failed";
    document.getElementById("confstatus").innerHTML = "<strong style='color: #F00;'>Trust4 was unable to automatically configure your node.  Check the output window for more information.</strong>";
}

function ACNext(stage, next, func)
{
    document.getElementById(stage).className = "success";
    document.getElementById(next).className = "working";
    DoRequest(next, func);
}

function ACStage1Result(state, text)
{
    if (state == 4)
    {
        if (text == "success")
            ACNext("address", "ports", ACStage2Result);
        else
            ACFailed("address");
    }
}

function ACStage2Result(state, text)
{
    if (state == 4)
    {
        if (text == "success")
            ACNext("ports", "routing", ACStage3Result);
        else
            ACFailed("ports");
    }
}

function ACStage3Result(state, text)
{
    if (state == 4)
    {
        if (text == "success")
            ACNext("routing", "start", ACStage4Result);
        else
            ACFailed("routing");
    }
}

function ACStage4Result(state, text)
{
    if (state == 4)
    {
        if (text == "success")
            ACNext("start", "peers", ACStageFinal);
        else
            ACFailed("start");
    }
}

function ACStageFinal(state, text)
{
    if (state == 4)
    {
        if (text == "success")
        {
            document.getElementById("peers").className = "success";
            location.href = "/";
        }
        else
            ACFailed("start");
    }
}