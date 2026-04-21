using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;

namespace OrderManagement.Infrastructure.Messaging;

// Small wrapper so we can create administration client only when ASB is configured.
public sealed class ServiceBusAdministrationClientWrapper
{
    public ServiceBusAdministrationClient? Client { get; }

    public ServiceBusAdministrationClientWrapper(IConfiguration configuration)
    {
        var cs = configuration["AZURE_SERVICE_BUS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(cs))
            Client = new ServiceBusAdministrationClient(cs);
    }
}

