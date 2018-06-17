using System;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Sinks.SystemConsole.Output;
using Serilog.Events;
using System.IO;

namespace ConsoleDemo
{
    /// <summary>
    /// Decorates an <see cref="OutputTemplateTokenRenderer"/> with a "scrubber"; a fuction that can take
    /// the rendered output (including any applied theme color codes) and return a new string that will be
    /// written to the output instead.
    /// </summary>
    class BufferScrubbingMessageRenderer : OutputTemplateTokenRenderer
    {
        readonly ConsoleTheme _theme;
        readonly IFormatProvider _provider;
        readonly Func<string, string> _scrubber;
        readonly OutputTemplateTokenRenderer _inner;

        public BufferScrubbingMessageRenderer(ConsoleTheme theme, IFormatProvider provider, Func<string, string> scrubber, OutputTemplateTokenRenderer inner)
        {
            _theme = theme;
            _provider = provider;
            _scrubber = scrubber;
            _inner = inner;
        }

        public override void Render(LogEvent logEvent, TextWriter output)
        {
            var bufferWriter = new StringWriter(_provider);
            _inner.Render(logEvent, bufferWriter);
            var writtenString = _scrubber(bufferWriter.GetStringBuilder().ToString());
            output.Write(writtenString);
        }
    }
}
