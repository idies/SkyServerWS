<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="Sciserver_webService._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Skyserver Web Service</title>
</head>
<body>
    <h1>SkyServerWS information page</h1>
    Contact: Manuchehr Taghizadeh-Popp, Johns Hopkins University. Email: mtaghiza [at] jhu.edu
    <br />
    Github: <a href="https://github.com/SciServer/SkyServerWS" target="_blank">https://github.com/SciServer/SkyServerWS</a>
    
    <br />
    <br />
    <h3>Website Traffic
    <iframe class="bgtext" id="if" frameBorder="0" src="<%=urlbase%>/SearchTools/UsageHistory?format=html" >
    </iframe>

    <br />
    <br />
    <h3>REST API Examples:</h3>
    <form id="form1" runat="server">
    <div>
       <%-- <asp:Button ID="cmdCreate" OnClick="cmdCreate_Click" runat="server"
     Text="Create" />--%>
    <br />
     <asp:Table ID="tbl" runat="server"/>
    </div>
    </form>
</body>
<style>
    #if {
        min-width: 100vw;
        min-height: 20rem;
        overflow-y: auto;
        background:url('data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg"><text x="5%" y="15%" font-size="20" fill="red">Loading in 20sec, please wait..</text></svg>');
    }

</style>


</html>
