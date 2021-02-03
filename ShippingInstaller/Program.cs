await SqlHelper.EnsureDatabaseExists(Connections.Shipping);
await SqlHelper.EnsureDatabaseExists(Connections.NServiceBus);

await using var shippingConnection = await Connections.OpenShipping();
await using var nsbConnection = await Connections.OpenNServiceBus();

await QueueInstaller.Install("ShippingEndpoint", nsbConnection);
await PersistenceInstaller.Install("ShippingEndpoint", Connections.NServiceBus);
await SynonymInstaller.Install("ShippingEndpoint", nsbConnection, shippingConnection);