namespace LibraryPro.Web.Services
{
    public class EmailQueue
    {
        private readonly Queue<EmailMessage> _queue = new Queue<EmailMessage>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void Enqueue(EmailMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            lock (_queue)
            {
                _queue.Enqueue(message);
            }
            _signal.Release();
        }

        public async Task<EmailMessage?> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    return _queue.Dequeue();
                }
            }
            
            return null;
        }

        public int Count
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count;
                }
            }
        }
    }

    public class EmailMessage
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? TextBody { get; set; }
        public string EmailType { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
