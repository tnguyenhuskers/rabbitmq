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
    public class OrderService : BackgroundService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly WorkerOptions _options;
        private NopToBaileyTransformer _transformer;

        private IModel _modelForConsumer;
        private IModel _modelForPublisher;
        private IConnection _connectionForPublisher;
        private IConnection _connectionForConsumer;

        public OrderService(ILogger<OrderService> logger, IOptions<WorkerOptions> options, NopToBaileyTransformer transformer)
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

            _connectionForConsumer = factory.CreateConnection("nopListener");
            _modelForConsumer = _connectionForConsumer.CreateModel();

            _connectionForPublisher = factory.CreateConnection("Republish");
            _modelForPublisher = _connectionForPublisher.CreateModel();

            ServiceUtility.ConfigureBroker(_modelForConsumer, _modelForPublisher, _options.ExchangeName, _options.OrderQueueName, _options.OrderErrorQueueName);

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_modelForConsumer);

            consumer.Received += OrderMessageReceived;

            _modelForConsumer.BasicConsume(
                queue: _options.OrderQueueName,
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


        private async Task OrderMessageReceived(object model, BasicDeliverEventArgs ea)
        {
            var orderId = ServiceUtility.GetMessage(ea.Body).OrderNumber;
            string orderHeader = null;
            bool success = false; 
            try
            {
                orderHeader = System.Text.Encoding.UTF8.GetString(ea.BasicProperties.Headers?["OrderID"] as byte[]);

                _logger.Log(LogLevel.Debug, $"Beginning to process order {orderId}.");

                var order = ServiceUtility.GetMessage(ea.Body);

                if (OrderHasAlreadyBeenProcessed(order))
                {
                    _logger.Log(LogLevel.Debug, $"Order {orderId} has already been processed. Skipping.");
                }
                else
                {
                    success = await _transformer.LoadDataIntoBailey(order);
                }

                if (!success)
                {
                    _logger.Log(LogLevel.Error, $"Failed to load order  {orderId ?? "Unknown"} into Bailey");
                    ServiceUtility.ForwardMessage(_logger, model, _modelForConsumer, _modelForPublisher, ea, _options.OrderQueueName, _options.OrderErrorQueueName, _options.ExchangeName, orderId);
                }
                else
                {
                    _modelForConsumer.BasicAck(ea.DeliveryTag, false);
                    _logger.Log(LogLevel.Debug, $"Finished processing order {orderId}.");
                }

            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error processing order {orderId ?? "Unknown"}");
                
                ServiceUtility.ForwardMessage(_logger, model, _modelForConsumer, _modelForPublisher, ea, _options.OrderQueueName, _options.OrderErrorQueueName, _options.ExchangeName, orderId);

                throw;
            }
        }

        private bool OrderHasAlreadyBeenProcessed(Models.Order order)
        {
            SqlConnection conn = new SqlConnection(_options.BaileyConnectionString);
            StringBuilder queryString = new StringBuilder();
            queryString.AppendFormat(
                "Select SourceSystemNumber From StepInsertionLog Where SourceSystemNumber = '{0}' and WorkItemId != 0",
                order.OrderNumber.ToString());
            SqlCommand command = new SqlCommand(queryString.ToString(), conn);
            command.Connection.Open();
            var result = command.ExecuteReader();

            return result.HasRows;
        }
    }
}