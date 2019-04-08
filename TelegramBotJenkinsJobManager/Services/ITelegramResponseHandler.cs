using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TelegramBotJenkinsJobManager.Services
{
    public interface ITelegramResponseHandler
    {
        Task HandleMessageAsync(Message message);

        Task HandleCallbackQueryAsync(CallbackQuery callbackQuery);
    }
}
