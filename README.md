# CosmosDbPoC 
Repository Pattern implementation in C# for CosmosDb. <br /><br />
This solution doesn't have a UI or API implementation. Only the repository and test libraries are present.<br />
<ul>
  <li>CosmosDbPoC.Data - Repository pattern implementation.</li>
  <li>CosmosDbPoC.Tests - Integration tests.</li>
</ul>
<b><i>Testes are intentionally not mocked.</i></b>
<br /><br />
<b>Requirements:</b> Valid CosmosDb instance in Azure to establish a connection.<br /><br />
<b>Update appsettings.json in CosmosDbPoC.Tests</b>
<br />
It's necessary to update the following properties inside the appsettings.json:<br />
<i>Host - Add the URL to your database</i><br />
<i>PrimaryKey - Add the PrimaryKey to authenticate with your database</i>
