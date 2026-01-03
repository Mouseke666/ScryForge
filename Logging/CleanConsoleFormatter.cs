using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace ScryForge.Logging
{
    public class CleanConsoleFormatter : ConsoleFormatter
    {
        public CleanConsoleFormatter() : base("clean") { }

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider? scopeProvider,
            TextWriter textWriter)
        {
            // if (logEntry == null) throw new ArgumentNullException(nameof(logEntry));
            // if (textWriter == null) throw new ArgumentNullException(nameof(textWriter));

            var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
            if (string.IsNullOrWhiteSpace(message)) return;

            textWriter.WriteLine(message);
        }
    }
}