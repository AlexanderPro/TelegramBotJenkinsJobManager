﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using TelegramBotJenkinsJobManager.Services;


namespace TelegramBotJenkinsJobManager.Controllers
{
    [Route("bot")]
    public class TelegramBotController : Controller
    {
        private readonly ITelegramResponseHandler _responseHandler;

        public TelegramBotController(ITelegramResponseHandler responseHandler)
        {
            _responseHandler = responseHandler ?? throw new ArgumentNullException(nameof(responseHandler));
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Update update)
        {
            if (update == null)
            {
                return Ok();
            }

            if (update.Message != null)
            {
                await _responseHandler.HandleMessageAsync(update.Message);
            }

            if (update.CallbackQuery != null)
            {
                await _responseHandler.HandleCallbackQueryAsync(update.CallbackQuery);
            }

            return Ok();
        }
    }
}