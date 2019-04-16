using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Args;
using Serilog;


namespace TelegramBotJenkinsJobManager.Services
{
    public class TelegramBotHostedService : IHostedService
    {
        private readonly TelegramBotClient _client;
        private readonly ITelegramResponseHandler _responseHandler;

        public TelegramBotHostedService(TelegramBotClient client, ITelegramResponseHandler responseHandler)
        {
            _client = client;
            _responseHandler = responseHandler;
            _client.OnMessage += OnMessageReceived;
            _client.OnCallbackQuery += OnCallbackQuery;
            _client.OnReceiveError += OnReceiveError;
            _client.OnReceiveGeneralError += OnReceiveGeneralError;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.StartReceiving();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async void OnMessageReceived(object sender, MessageEventArgs e)
        {
            await _responseHandler.HandleMessageAsync(e.Message);
        }

        private async void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            await _responseHandler.HandleCallbackQueryAsync(e.CallbackQuery);
        }

        private void OnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            Log.Error(e.ApiRequestException, $"OnReceiveError: {e.ApiRequestException.ErrorCode} - {e.ApiRequestException.Message}");
        }

        private void OnReceiveGeneralError(object sender, ReceiveGeneralErrorEventArgs e)
        {
            Log.Error(e.Exception, nameof(OnReceiveGeneralError));
        }
    }
}
