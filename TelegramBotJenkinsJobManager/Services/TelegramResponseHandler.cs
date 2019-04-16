using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InputFiles;
using TelegramBotJenkinsJobManager.Extensions;
using Serilog;

namespace TelegramBotJenkinsJobManager.Services
{
    public class TelegramResponseHandler : ITelegramResponseHandler
    {
        private readonly TelegramBotClient _client;
        private readonly IList<MenuItem> _menu;
        private readonly IJenkinsService _jenkinsService;
        private readonly string _jenkinsProtocol;
        private readonly string _jenkinsFqdn;
        private readonly IList<long> _allowedChatIds;
        private readonly IJobQueue _jobQueue;

        public TelegramResponseHandler(TelegramBotClient client, IJenkinsService jenkinsService, IJobQueue jobQueue, IList<MenuItem> menu, IList<long> allowedChatIds, string jenkinsProtocol, string jenkinsFqdn)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _jenkinsService = jenkinsService ?? throw new ArgumentNullException(nameof(jenkinsService));
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _jenkinsProtocol = jenkinsProtocol ?? throw new ArgumentNullException(nameof(jenkinsProtocol));
            _jenkinsFqdn = jenkinsFqdn ?? throw new ArgumentNullException(nameof(jenkinsFqdn));
            _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
            _allowedChatIds = allowedChatIds;
        }

        public async Task HandleMessageAsync(Message message)
        {
            if (message?.Type == MessageType.Text)
            {
                if (_allowedChatIds != null && _allowedChatIds.Any() && !_allowedChatIds.Contains(message.Chat.Id))
                {
                    return;
                }
                var botName = message.Text.StartsWith('/') ? (await GetUserAsync())?.Username ?? "" : "";
                switch (message.Text)
                {
                    case var s when message.Text.Equals("/start", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals($"/start@{botName}", StringComparison.InvariantCultureIgnoreCase):
                        {
                            try
                            {
                                var usage = @"
Usage:
/run    - start a job
/status - get a job status
/get    - download the latest job artifacts
/goto   - navigate to a job page";
                                await _client.SendTextMessageAsync(message.Chat.Id, usage);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "/start handler");
                            }
                        }
                        break;

                    case var s when message.Text.Equals("/run", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals($"/run@{botName}", StringComparison.InvariantCultureIgnoreCase):
                        {
                            try
                            {
                                await _client.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: "Run",
                                    parseMode: ParseMode.Html,
                                    disableWebPagePreview: false,
                                    replyMarkup: new InlineKeyboardMarkup(_menu.GroupBy(x => x.Row, y => y).OrderBy(x => x.Key).Select(x => x.Select(y => InlineKeyboardButton.WithCallbackData(y.DisplayName, y.RunName)))));
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "/run handler");
                            }
                        }
                        break;

                    case var s when message.Text.Equals("/status", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals($"/status@{botName}", StringComparison.InvariantCultureIgnoreCase):
                        {
                            try
                            {
                                await _client.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: "Status",
                                    parseMode: ParseMode.Html,
                                    disableWebPagePreview: false,
                                    replyMarkup: new InlineKeyboardMarkup(_menu.GroupBy(x => x.Row, y => y).OrderBy(x => x.Key).Select(x => x.Select(y => InlineKeyboardButton.WithCallbackData(y.DisplayName, y.StatusName)))));
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "/status handler");
                            }
                        }
                        break;

                    case var s when message.Text.Equals("/get", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals($"/get@{botName}", StringComparison.InvariantCultureIgnoreCase):
                        {
                            try
                            {
                                await _client.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: "Get Artifacts",
                                    parseMode: ParseMode.Html,
                                    disableWebPagePreview: false,
                                    replyMarkup: new InlineKeyboardMarkup(_menu.GroupBy(x => x.Row, y => y).OrderBy(x => x.Key).Select(x => x.Select(y => InlineKeyboardButton.WithCallbackData(y.DisplayName, y.ArtifactName)))));
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "/get handler");
                            }
                        }
                        break;

                    case var s when message.Text.Equals("/goto", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals($"/goto@{botName}", StringComparison.InvariantCultureIgnoreCase):
                        {
                            try
                            {
                                var gotoList = string.Join(Environment.NewLine, _menu.Select(x => $"{x.DisplayName} - {_jenkinsProtocol}://{_jenkinsFqdn}/{x.Path}"));
                                await _client.SendTextMessageAsync(message.Chat.Id, gotoList);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "/goto handler");
                            }
                        }
                        break;
                }
            }
        }

        public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            if (callbackQuery?.Data != null)
            {
                switch (callbackQuery.Data)
                {
                    case var s when _menu.Select(x => x.RunName).Contains(callbackQuery.Data):
                        {
                            var result = true;
                            var menuItem = _menu.FirstOrDefault(x => x.RunName == callbackQuery.Data);
                            try
                            {
                                await _jenkinsService.RunJobAsync(menuItem.Path, menuItem.Parameters.ToDictionary(x => x.Name, y => y.Value));
                            }
                            catch (Exception ex)
                            {
                                result = false;
                                Log.Error(ex, "Run handler");
                            }

                            try
                            {
                                var message = result ? $"{menuItem.DisplayName} has been enqueued successfully" : $"Failed to enqueue {menuItem.DisplayName}";
                                await _client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, message);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Run handler. Send text.");
                            }

                            if (result && menuItem.NotifyWhenBuildIsFinished)
                            {
                                _jobQueue.Enqueue(new JobSettings { ChatId = callbackQuery.Message.Chat.Id, JobPath = menuItem.Path, JobDisplayName = menuItem.DisplayName });
                            }
                        }
                        break;

                    case var s when _menu.Select(x => x.StatusName).Contains(callbackQuery.Data):
                        {
                            var status = (JobStatus)null;
                            var menuItem = _menu.FirstOrDefault(x => x.StatusName == callbackQuery.Data);
                            try
                            {
                                status = await _jenkinsService.GetJobStatusAsync(menuItem.Path);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Status handler");
                            }

                            try
                            {
                                var message = status != null ? $"{menuItem.DisplayName} is {status.Status} on {status.TimeStamp.FromUnixTimeMilliseconds():dd.MM.yyyy HH:mm}" : $"Failed to get a status of the job";
                                await _client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, message);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Status handler. Send text.");
                            }
                        }
                        break;

                    case var s when _menu.Select(x => x.ArtifactName).Contains(callbackQuery.Data):
                        {
                            var result = true;
                            var menuItem = _menu.FirstOrDefault(x => x.ArtifactName == callbackQuery.Data);
                            try
                            {
                                var artifacts = await _jenkinsService.GetJobArtifactsAsync(menuItem.Path);
                                foreach (var artifact in artifacts)
                                {
                                    await _client.SendDocumentAsync(callbackQuery.Message.Chat.Id, new InputOnlineFile(artifact.Item2, artifact.Item1));
                                }
                                artifacts.ToList().ForEach(x => x.Item2.Dispose());
                            }
                            catch (Exception ex)
                            {
                                result = false;
                                Log.Error(ex, "Artifact handler");
                            }

                            try
                            {
                                if (!result)
                                {
                                    await _client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Failed to download artifacts");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Artifact handler. Send text.");
                            }
                        }
                        break;
                }
            }
        }

        private async Task<User> GetUserAsync()
        {
            try
            {
                return await _client.GetMeAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                return await Task.FromResult<User>(null);
            }
        }
    }
}
