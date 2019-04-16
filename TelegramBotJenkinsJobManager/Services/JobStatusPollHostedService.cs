using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Serilog;
using TelegramBotJenkinsJobManager.Extensions;

namespace TelegramBotJenkinsJobManager.Services
{
    public class JobStatusPollHostedService : IHostedService
    {
        private readonly IJobQueue _queue;
        private readonly TelegramBotClient _client;
        private readonly IJenkinsService _jenkinsService;
        private CancellationTokenSource _tokenSource;

        public JobStatusPollHostedService(IJobQueue queue, TelegramBotClient client, IJenkinsService jenkinsService)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _jenkinsService = jenkinsService ?? throw new ArgumentNullException(nameof(jenkinsService));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    var jobSettings = _queue.Dequeue();
                    if (jobSettings != null)
                    {
                        var jobStatus = (JobStatus)null;
                        do
                        {
                            jobStatus = await _jenkinsService.GetJobStatusAsync(jobSettings.JobPath);
                            if (jobStatus.Building)
                            {
                                await Task.Delay(3000);
                            }
                        } while (jobStatus.Building);
                        await _client.SendTextMessageAsync(jobSettings.ChatId, $"{jobSettings.JobDisplayName} has been finished with status {jobStatus.Status} on {jobStatus.TimeStamp.FromUnixTimeMilliseconds():dd.MM.yyyy HH:mm}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, e.Message);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _tokenSource?.Cancel();
            return Task.CompletedTask;
        }
    }
}
