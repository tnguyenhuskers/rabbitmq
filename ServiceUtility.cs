using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NopOrderImporter
{
    class ServiceUtility
    {
        public static Models.Order GetMessage(byte[] body)
        {
            var message = Encoding.UTF8.GetString(body);

            return JsonSerializer.Deserialize<Models.Order>(message);
        }
        public static int GetFailedCount(BasicDeliverEventArgs ea)
        {
            if (!ea.BasicProperties.IsHeadersPresent())
            {
                ea.BasicProperties.Headers = new Dictionary<string, object>();
            }

            if (!ea.BasicProperties.Headers.TryGetValue("FailedCount", out object failedCountObject))
            {
                ea.BasicProperties.Headers.Add("FailedCount", 0);
            }

            return (int) (failedCountObject ?? 0);
        }
        public static void ForwardMessage(ILogger logger, object model, IModel consumer, IModel publisher, BasicDeliverEventArgs ea, string queueName, string errorQueueName, string exchangeName, string header = "")
        {
            int failedCount = 0;

            failedCount = ServiceUtility.GetFailedCount(ea) + 1;
            ea.BasicProperties.Headers["FailedCount"] = failedCount;

            if (failedCount >= 3)
            {
                logger.Log(LogLevel.Debug, null, $"Republish message {header ?? "Unknown"} into {errorQueueName} queue");
                RePublishOrderMessage(logger, publisher, ea, errorQueueName, exchangeName);
                consumer.BasicAck(ea.DeliveryTag, false);
            }
            else
            {
                logger.Log(LogLevel.Debug, null,
                    $"Republish message {header ?? "Unknown"} {failedCount} times into {queueName} queue");
                RePublishOrderMessage(logger, publisher, ea, queueName, exchangeName);
                consumer.BasicAck(ea.DeliveryTag, false);
            }
        }
        private static void RePublishOrderMessage(ILogger logger, IModel model, BasicDeliverEventArgs ea, string routingKey, string exchangeName)
        {
            model.BasicPublish(exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: ea.BasicProperties,
                body: ea.Body);

            try
            {
                model.WaitForConfirmsOrDie(TimeSpan.FromSeconds(100));
            }
            catch (Exception exception)
            {
                logger.Log(LogLevel.Error, exception, $"Error unable to republish message");
                throw;
            }
        }

        public static void ConfigureBroker(IModel consumer, IModel publisher, string exchangeName, string queueName, string errorQueueName)
        {
            // for consumer
            consumer.ExchangeDeclare(exchange: exchangeName,
                type: "direct",
                durable: true);

            consumer.QueueDeclare(queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var propertiesForConsumer = consumer.CreateBasicProperties();
            propertiesForConsumer.Persistent = true;

            consumer.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            consumer.QueueBind(queue: queueName,
                exchange: exchangeName,
                routingKey: queueName);
            consumer.ConfirmSelect();

            // for publisher
            publisher.ExchangeDeclare(exchange: exchangeName,
                type: "direct",
                durable: true);

            publisher.QueueDeclare(queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            publisher.QueueDeclare(queue: errorQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var propertiesForPublisher = publisher.CreateBasicProperties();
            propertiesForPublisher.Persistent = true;

            publisher.QueueBind(queue: queueName,
                exchange: exchangeName,
                routingKey: queueName);

            publisher.QueueBind(queue: errorQueueName,
                exchange: exchangeName,
                routingKey: errorQueueName);

            publisher.ConfirmSelect();
        }
    }
}
