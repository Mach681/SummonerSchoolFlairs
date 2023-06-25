using Azure.Messaging.ServiceBus;

namespace LeagueFlairRiotUpdateService.Handlers
{
    public interface IEventHandler
    {
        string HandlerName { get; }
        bool CanHandle(ServiceBusReceivedMessage message);
        Task Handle(ServiceBusReceivedMessage message);
    }
}
