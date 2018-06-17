using Serilog;
using System;
using System.Threading;
using Serilog.Sinks.SystemConsole.Themes;
using System.Linq;
using System.Collections.Generic;
using Serilog.Sinks.SystemConsole.Output;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace ConsoleDemo
{
    public class Program
    {
        /// <summary>
        /// An <see cref="OutputTemplateRenderer.TokenRendererFactory"/> which will "scrub" output; written text will be
        /// matched with a <see cref="Regex"/> then replaced with a <paramref name="replacement"/> string instead.
        /// </summary>
        static OutputTemplateRenderer.TokenRendererFactory LevelThemeSwitchingTemplateRendererFactory(
            Func<LogEventLevel, ConsoleTheme> getThemeForLevel)
        {
            var allLevelsWithTheme = Enum.GetValues(typeof(LogEventLevel))
                .Cast<LogEventLevel>()
                .Select(lvl => new { Level = lvl, Theme = getThemeForLevel(lvl) })
                .ToArray();

            return (t, pt, th, p) =>
            {
                if (pt.PropertyName == "Exception")
                {
                    return new ThemedExceptionTokenRenderer(
                        theme: th,
                        propertyToken: pt,
                        fallbackExceptionRenderer: OutputTemplateRenderer.NewStandardRenderer(t, pt, th, p));
                }

                var renderersByLevel = allLevelsWithTheme
                    .ToDictionary(
                        lt => lt.Level,
                        lt => OutputTemplateRenderer.NewStandardRenderer(t, pt, lt.Theme, p)
                            ?? OutputTemplateRenderer.NewEventPropertyTokenRenderer(t, pt, lt.Theme, p));

                return new EventSwitchingTemplateTokenRenderer(evt => renderersByLevel[evt.Level]);
            };
        }

        /// <summary>
        /// An <see cref="OutputTemplateRenderer.TokenRendererFactory"/> which will "scrub" output; written text will be
        /// matched with a <see cref="Regex"/> then replaced with a <paramref name="replacement"/> string instead.
        /// </summary>
        static OutputTemplateRenderer.TokenRendererFactory RegexSrubbingTemplateRendererFactory(Regex matcher, string replacement)
        {
            return (t, pt, th, p) =>
            {
                if (pt.PropertyName == Serilog.Formatting.Display.OutputProperties.LevelPropertyName
                    || pt.PropertyName == Serilog.Formatting.Display.OutputProperties.NewLinePropertyName
                    || pt.PropertyName == Serilog.Formatting.Display.OutputProperties.TimestampPropertyName)
                {
                    // It would be just overhead to "scrub" these standard properties
                    return OutputTemplateRenderer.NewStandardRenderer(t, pt, th, p);
                }
                else if (pt.PropertyName == "Exception")
                {
                    // Try to use the themed exception renderer, but fallback to the built-in exception
                    // rendering if something goes wrong.
                    return new BufferScrubbingMessageRenderer(th, p,
                        scrubber: rendered => matcher.Replace(rendered, replacement),
                        inner: new ThemedExceptionTokenRenderer(
                            theme: th,
                            propertyToken: pt,
                            fallbackExceptionRenderer: OutputTemplateRenderer.NewStandardRenderer(t, pt, th, p)));
                }

                return new BufferScrubbingMessageRenderer(th, p,
                    scrubber: rendered => matcher.Replace(rendered, replacement),
                    inner: OutputTemplateRenderer.NewStandardRenderer(t, pt, th, p)
                        ?? OutputTemplateRenderer.NewEventPropertyTokenRenderer(t, pt, th, p));
            };
        }

        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                //.WriteTo.Console(
                //    outputTemplate: "[UNSCRUBBED] [{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}",
                //    theme: AnsiConsoleTheme.Literate)
                .WriteTo.Console(
                    new OutputTemplateRenderer(
                        theme: AnsiConsoleTheme.Code,
                        formatProvider: System.Globalization.CultureInfo.CurrentUICulture,
                        outputTemplate: "[LEVELTHEME] [{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                        rendererFactory: LevelThemeSwitchingTemplateRendererFactory(
                            getThemeForLevel: l => l > LogEventLevel.Information ? AnsiConsoleTheme.Code : AnsiConsoleTheme.Grayscale)),
                    standardErrorFromLevel: LevelAlias.Minimum)
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
                Log.Verbose("Doing some really {Level} stuff here with {@Version}", LogEventLevel.Verbose, new Version("1.2.3.4"));
                Log.Debug("Getting started");

                Log.Information("Hello {Name} from thread {ThreadId}", Environment.GetEnvironmentVariable("USERNAME"), Thread.CurrentThread.ManagedThreadId);
                Log.Verbose("Doing some really {Level} stuff here with {@Version}", LogEventLevel.Verbose, new Version("1.2.3.4"));
                Log.Verbose("Doing some really {Level} stuff here with {@Version}", LogEventLevel.Verbose, new Version("1.2.3.4"));
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
