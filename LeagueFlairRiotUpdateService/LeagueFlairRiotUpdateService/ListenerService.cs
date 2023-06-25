using Azure.Messaging.ServiceBus;
using LeagueFlairRiotUpdateService.Handlers;
using LeagueFlairRiotUpdateService.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LeagueFlairRiotUpdateService
{
    public class ListenerService : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IConfiguration _config;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _processor;
        //private ServiceBusSender _serviceBusSender;
        private readonly IEnumerable<IEventHandler> _eventHandlers;
        //private readonly TelemetryClient _telemetry;

        public ListenerService(
            IHostApplicationLifetime appLifetime,
            IConfiguration config,
            IEnumerable<IEventHandler> eventHandlers,
            ServiceBusClient client,
            //TelemetryClient telemetry,
            StorageHelper storageFactory)
        {
            _appLifetime = appLifetime;
            _config = config;
            _client = client;
            _eventHandlers = eventHandlers;
            //_telemetry = telemetry;
            _processor = storageFactory.GetServiceBusProcessor();
        }

        // handle any errors when receiving messages
        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            //_telemetry.TrackEvent("OmniBotService.MessageException",
            //    new Dictionary<string, string>()
            //    {
            //        { "exception", args.Exception.ToString() }
            //    }, null);
            return Task.CompletedTask;
        }

        public async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            //string messageType = args.Message.ApplicationProperties["X-MsgName"].ToString();
            //string version = args.Message.ApplicationProperties["X-MsgTypeVersion"].ToString();
            Console.WriteLine($"Received: {body}");

            _eventHandlers.Where(e => e.CanHandle(args.Message))
                .ToList()
                .ForEach(x => {
                    Console.WriteLine(x.HandlerName);
                    x.Handle(args.Message);
                });

            Console.WriteLine("Done");

            // complete the message. messages is deleted from the queue. 
            await args.CompleteMessageAsync(args.Message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000); //delay to make sure other services have started

                    string topicName = _config.GetValue<string>("ServiceBusTopicName");
                    string subscriptionName = _config.GetValue<string>("ServiceBusSubscriptionName");
                    string listenTopicName = _config.GetValue<string>("ServiceBusListenTopicName");

                    try
                    {
                        Console.WriteLine("Starting Listener");
                        Console.WriteLine($"ServiceBusTopicName {topicName}");
                        Console.WriteLine($"ServiceBusListenTopicName {listenTopicName}");
                        Console.WriteLine($"ServiceBusSubscriptionName {subscriptionName}");


                        //_telemetry.TrackEvent("OmniBotService.ServiceStart",
                        //    new Dictionary<string, string>()
                        //    {
                        //        { "topicName", topicName },
                        //        { "subscriptionName", subscriptionName }
                        //    }, null);

                        // add handler to process messages
                        _processor.ProcessMessageAsync += MessageHandler;

                        // add handler to process any errors
                        _processor.ProcessErrorAsync += ErrorHandler;

                        // start processing 
                        await _processor.StartProcessingAsync();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unhandled exception:\n{ex.Message}");
                        //_telemetry.TrackEvent("OmniBotService.ServiceException",
                        //    new Dictionary<string, string>()
                        //    {
                        //        { "message", ex.Message }
                        //    }, null);
                    }
                });
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () => {
                // stop processing 
                Console.WriteLine("\nStopping the receiver...");
                await _processor.StopProcessingAsync();
                Console.WriteLine("Stopped receiving messages, disposing objects");
                await _processor.DisposeAsync();
                await _client.DisposeAsync();
                //Console.WriteLine("\nflushing telemetry");
                //_telemetry.Flush();

                return Task.CompletedTask;
            });
        }
    }
}
