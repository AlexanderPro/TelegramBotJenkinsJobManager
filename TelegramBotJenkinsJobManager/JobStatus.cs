namespace TelegramBotJenkinsJobManager
{
    public class JobStatus
    {
        public bool Building { get; set; }

        public string Result { get; set; }

        public long TimeStamp { get; set; }

        public string Status => Building ? "BUILDING" : string.IsNullOrEmpty(Result) ? "UNKNOWN" : Result;
    }
}
