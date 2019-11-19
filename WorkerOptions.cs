namespace NopOrderImporter
{
    public class WorkerOptions
    {
        public string BaileyConnectionString { get; set; }
        public string OrderQueueName { get; set; }
        public string CustomerQueueName { get; set; }
        public string CustomerErrorQueueName { get; set; }
        public string OrderErrorQueueName { get; set; }
        public string ExchangeName { get; set; }
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public bool DispatchConsumersAsync { get; set; }
    }
}