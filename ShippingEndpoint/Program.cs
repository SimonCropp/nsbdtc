using System;
using NServiceBus;

var configuration = new EndpointConfiguration("ShippingEndpoint");

// note that transport and persistence is using the business DB
configuration.ApplyCommonConfig(Connections.Shipping);

var endpointInstance = await Endpoint.Start(configuration);
Console.WriteLine("Press any to exit.");
Console.ReadKey();
await endpointInstance.Stop();