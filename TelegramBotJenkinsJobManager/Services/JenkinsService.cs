using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using Newtonsoft.Json;
using TelegramBotJenkinsJobManager.Extensions;

namespace TelegramBotJenkinsJobManager.Services
{
    public class JenkinsService : IJenkinsService
    {
        private readonly string _protocol;
        private readonly string _fqdn;
        private readonly string _userName;
        private readonly string _token;

        public JenkinsService(string protocol, string fqdn, string userName, string token)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _fqdn = fqdn ?? throw new ArgumentNullException(nameof(fqdn));
            _userName = userName ?? throw new ArgumentNullException(nameof(userName));
            _token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public async Task RunJobAsync(string jobPath, IDictionary<string, string> parameters)
        {
            using (var httpClient = new HttpClient())
            {
                var userNameTokenByteArray = Encoding.UTF8.GetBytes($"{_userName}:{_token}");
                var userNameTokenBase64 = Convert.ToBase64String(userNameTokenByteArray);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userNameTokenBase64);
                var url = $"{_protocol}://{_fqdn}/crumbIssuer/api/xml?xpath=concat(//crumbRequestField,\":\",//crumb)";
                var responseMessage = await httpClient.GetAsync(url);
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                httpClient.DefaultRequestHeaders.Add("Jenkins-Crumb", responseContent.Split(':')[1]);
                url = $"{_protocol}://{_fqdn}/{jobPath}/build?delay=0";
                var formParameters = new Dictionary<string, string>();
                if (parameters != null && parameters.Any())
                {
                    var jsonParameters = new { parameter = parameters.Select(x => new { name = x.Key, value = x.Value }).ToList() };
                    formParameters.Add("json", JsonConvert.SerializeObject(jsonParameters));
                }
                responseMessage = await httpClient.PostAsync(url, new FormUrlEncodedContent(formParameters));
                responseContent = await responseMessage.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    throw new Exception(responseContent);
                }
            }
        }

        public async Task<Tuple<DateTime, string>> GetJobStatusAsync(string jobPath)
        {
            using (var httpClient = new HttpClient())
            {
                var userNameTokenByteArray = Encoding.UTF8.GetBytes($"{_userName}:{_token}");
                var userNameTokenBase64 = Convert.ToBase64String(userNameTokenByteArray);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userNameTokenBase64);
                var url = $"{_protocol}://{_fqdn}/crumbIssuer/api/xml?xpath=concat(//crumbRequestField,\":\",//crumb)";
                var responseMessage = await httpClient.GetAsync(url);
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                httpClient.DefaultRequestHeaders.Add("Jenkins-Crumb", responseContent.Split(':')[1]);
                url = $"{_protocol}://{_fqdn}/{jobPath}/lastBuild/api/json?pretty=true";
                responseMessage = await httpClient.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>()));
                responseContent = await responseMessage.Content.ReadAsStringAsync();
                var lastBuild = JsonConvert.DeserializeObject<LastBuild>(responseContent);
                if (lastBuild == null)
                {
                    throw new Exception(responseContent);
                }
                var result = lastBuild.result == null ? "UNKNOWN" : lastBuild.result;
                return new Tuple<DateTime, string>(lastBuild.timestamp.FromUnixTimeMilliseconds(), result);
            }
        }

        public async Task<IList<Tuple<string, Stream>>> GetJobArtifactsAsync(string jobPath)
        {
            using (var httpClient = new HttpClient())
            {
                var userNameTokenByteArray = Encoding.UTF8.GetBytes($"{_userName}:{_token}");
                var userNameTokenBase64 = Convert.ToBase64String(userNameTokenByteArray);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userNameTokenBase64);
                var url = $"{_protocol}://{_fqdn}/crumbIssuer/api/xml?xpath=concat(//crumbRequestField,\":\",//crumb)";
                var responseMessage = await httpClient.GetAsync(url);
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                httpClient.DefaultRequestHeaders.Add("Jenkins-Crumb", responseContent.Split(':')[1]);
                url = $"{_protocol}://{_fqdn}/{jobPath}/lastSuccessfulBuild/api/json";
                responseMessage = await httpClient.GetAsync(url);
                responseContent = await responseMessage.Content.ReadAsStringAsync();
                var lastBuild = JsonConvert.DeserializeObject<LastSuccessfulBuild>(responseContent);
                if (lastBuild == null || lastBuild.artifacts == null)
                {
                    throw new Exception(responseContent);
                }
                var result = new List<Tuple<string, Stream>>();
                foreach (var artifact in lastBuild.artifacts)
                {
                    url = $"{_protocol}://{_fqdn}/{jobPath}/lastSuccessfulBuild/artifact/{artifact.relativePath}";
                    responseMessage = await httpClient.GetAsync(url);
                    var stream = await responseMessage.Content.ReadAsStreamAsync();
                    result.Add(new Tuple<string, Stream>(artifact.fileName, stream));
                }
                return result;
            }
        }

        private class LastBuild
        {
            public string result;
            public long timestamp;
        }

        private class LastSuccessfulBuild
        {
            public IList<Artifact> artifacts;
        }

        private class Artifact
        {
            public string fileName;
            public string relativePath;
        }
    }
}
