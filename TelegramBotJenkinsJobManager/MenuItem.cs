using System;
using System.Collections.Generic;

namespace TelegramBotJenkinsJobManager
{
    public class MenuItem
    {
        public string RunName => $"RUN_{Name}";

        public string StatusName => $"STATUS_{Name}";

        public string ArtifactName => $"ARTIFACT_{Name}";

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Path { get; set; }

        public int Row { get; set; }

        public bool NotifyWhenBuildIsFinished { get; set; }

        public IList<Parameter> Parameters { get; set; }

        public class Parameter
        {
            public string Name { get; set; }

            public string Value { get; set; }
        }
    }
}