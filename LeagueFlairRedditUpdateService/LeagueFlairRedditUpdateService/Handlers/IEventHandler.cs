using Azure.Messaging.ServiceBus;

namespace LeagueFlairRedditUpdateService.Handlers
{
    public interface IEventHandler
    {
        string HandlerName { get; }
        bool CanHandle(ServiceBusReceivedMessage message);
        Task Handle(ServiceBusReceivedMessage message);
    }
}
