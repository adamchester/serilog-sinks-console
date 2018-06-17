using Serilog;
using System;
using System.Threading;
using Serilog.Sinks.SystemConsole.Themes;
using System.Linq;
using System.Collections.Generic;
using Serilog.Sinks.SystemConsole.Output;
using Serilog.Events;
using Serilog.Parsing;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleDemo
{
    public class Program
    {
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
                if (!theme.CanBuffer)
                {
                    throw new ArgumentOutOfRangeException(nameof(theme), "No support for scrubbing unbuffered themes at this stage.");
                }
            }

            public override void Render(LogEvent logEvent, TextWriter output)
            {
                var bufferWriter = new StringWriter(_provider);
                _inner.Render(logEvent, bufferWriter);
                var writtenString = _scrubber(bufferWriter.GetStringBuilder().ToString());
                output.Write(writtenString);
            }
        }

        static OutputTemplateRenderer.TokenRendererFactory RegexSrubbingTemplateRendererFactory(Regex matcher, string replacement)
        {
            return (t, pt, th, p) =>
            {
                if (pt.PropertyName == "Exception")
                {
                    return new BufferScrubbingMessageRenderer(th, p,
                        scrubber: rendered => matcher.Replace(rendered, replacement),
                        inner: new ThemedExceptionTokenRenderer(th, pt));
                }
                return new BufferScrubbingMessageRenderer(th, p,
                    scrubber: rendered => matcher.Replace(rendered, replacement),
                    inner: OutputTemplateRenderer.NewStandardRenderer(t, pt, th, p));
            };
        }

        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(
                    outputTemplate: "[UNSCRUBBED] [{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Literate)
                .WriteTo.Console(
                    new OutputTemplateRenderer(
                        theme: AnsiConsoleTheme.Code,
                        outputTemplate: "[SCRUBBED  ] [{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                        formatProvider: null,
                        rendererFactory: RegexSrubbingTemplateRendererFactory(
                            matcher: new Regex("adam", options: RegexOptions.IgnoreCase | RegexOptions.Compiled),
                            replacement: "<redacted>")),
                    standardErrorFromLevel: LevelAlias.Minimum)
                .CreateLogger();

            try
            {
                Log.Debug("Getting started");

                Log.Information("Hello {Name} from thread {ThreadId}", Environment.GetEnvironmentVariable("USERNAME"), Thread.CurrentThread.ManagedThreadId);

                Log.Warning("No coins remain at position {@Position}", new { Lat = 25, Long = 134 });

                Fail("abc", 123, "s1", new List<int> { 1, 2, 3 });
            }
            catch (Exception e)
            {
                Log.Error(e, "Something went wrong");
            }

            Log.CloseAndFlush();
        }

        static void Fail<T1, T2>(T1 param1, T2 param2, string s1, List<int> list1)
        {
            // make sure we get a stack frame that is not in our code, and make it an aggreggate
            Enumerable
                .Range(1, 5)
                .Select(i => i.ToString("i", provider: null))
                .AsParallel()
                .WithDegreeOfParallelism(5)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .ToArray();
        }
    }
}
