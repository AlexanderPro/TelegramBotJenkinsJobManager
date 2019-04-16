namespace TelegramBotJenkinsJobManager.Services
{
    public interface IJobQueue
    {
        void Enqueue(JobSettings settings);
        JobSettings Dequeue();
    }
}
