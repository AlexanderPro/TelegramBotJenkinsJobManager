using System.Collections.Concurrent;

namespace TelegramBotJenkinsJobManager.Services
{
    public class JobQueue : IJobQueue
    {
        private readonly BlockingCollection<JobSettings> _queue;

        public JobQueue()
        {
            _queue = new BlockingCollection<JobSettings>();
        }

        public void Enqueue(JobSettings settings)
        {
            _queue.Add(settings);
        }

        public JobSettings Dequeue()
        {
            return _queue.Take();
        }
    }
}
