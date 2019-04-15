using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TelegramBotJenkinsJobManager.Services
{
    public interface IJenkinsService
    {
        Task RunJobAsync(string jobPath, IDictionary<string, string> parameters);
        Task<JobStatus> GetJobStatusAsync(string jobPath);
        Task<IList<Tuple<string, Stream>>> GetJobArtifactsAsync(string jobPath);
    }
}
