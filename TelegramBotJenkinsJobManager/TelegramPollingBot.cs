using Telegram.Bot;
using Telegram.Bot.Args;
using TelegramBotJenkinsJobManager.Services;
using Serilog;

namespace TelegramBotJenkinsJobManager
{
    public class TelegramPollingBot
    {
        private readonly TelegramBotClient _client;
        private readonly ITelegramResponseHandler _responseHandler;

        public TelegramPollingBot(TelegramBotClient client, ITelegramResponseHandler responseHandler)
        {
            _client = client;
            _responseHandler = responseHandler;
            _client.OnMessage += OnMessageReceived;
            _client.OnCallbackQuery += OnCallbackQuery;
            _client.OnReceiveError += OnReceiveError;
            _client.OnReceiveGeneralError += OnReceiveGeneralError;
        }

        public void Start()
        {
            _client.StartReceiving();
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
