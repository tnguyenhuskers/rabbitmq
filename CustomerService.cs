using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NopOrderImporter
{
    public class CustomerService : BackgroundService
    {
        private readonly ILogger<CustomerService> _logger;
        private readonly WorkerOptions _options;
        private NopToBaileyTransformer _transformer;

        private IModel _modelForConsumer;
        private IModel _modelForPublisher;
        private IConnection _connectionForPublisher;
        private IConnection _connectionForConsumer;

        public CustomerService(ILogger<CustomerService> logger, IOptions<WorkerOptions> options, NopToBaileyTransformer transformer)
        {
            _logger = logger;
            _options = options.Value;
            _transformer = transformer;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                Port = _options.Port,
                DispatchConsumersAsync = _options.DispatchConsumersAsync
            };

            _connectionForConsumer = factory.CreateConnection("CustomerConsumer");
            _modelForConsumer = _connectionForConsumer.CreateModel(); 

            _connectionForPublisher = factory.CreateConnection("CustomerPublisher");
            _modelForPublisher = _connectionForPublisher.CreateModel();

            ServiceUtility.ConfigureBroker(_modelForConsumer, _modelForPublisher, _options.ExchangeName, _options.CustomerQueueName, _options.CustomerErrorQueueName);

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_modelForConsumer);

            consumer.Received += CustomerMessageReceived;

            _modelForConsumer.BasicConsume(
                queue: _options.CustomerQueueName,
                autoAck: false,
                consumer: consumer);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _modelForConsumer.Dispose();
            _connectionForConsumer.Dispose();
            _modelForPublisher.Dispose();
            _connectionForPublisher.Dispose();
            await base.StopAsync(cancellationToken);
        }

        private async Task CustomerMessageReceived(object model, BasicDeliverEventArgs ea)
        {
            bool success = false; 
            try
            {
                _logger.Log(LogLevel.Debug, $"Beginning to process customer information.");

                var body = ServiceUtility.GetMessage(ea.Body);

                success = await _transformer.LoadCustomerIntoBailey(body);

                if (!success)
                {
                    _logger.Log(LogLevel.Error, $"Failed to load customer information into Bailey");
                    ServiceUtility.ForwardMessage(_logger, model, _modelForConsumer, _modelForPublisher, ea, _options.CustomerQueueName, _options.CustomerErrorQueueName, _options.ExchangeName);
                }
                else
                {
                    _modelForConsumer.BasicAck(ea.DeliveryTag, false);
                    _logger.Log(LogLevel.Debug, $"Finished processing customer information.");
                }

            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error processing customer information");
                ServiceUtility.ForwardMessage(_logger, model, _modelForConsumer, _modelForPublisher, ea, _options.CustomerQueueName, _options.CustomerErrorQueueName, _options.ExchangeName);
                throw;
            }
        }
    }
}