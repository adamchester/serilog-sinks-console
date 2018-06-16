using Serilog;
using System;
using System.Threading;
using Serilog.Sinks.SystemConsole.Themes;
using System.Linq;
using System.Collections.Generic;
using Serilog.Sinks.SystemConsole.Output;
using Serilog.Events;
using Serilog.Parsing;

namespace ConsoleDemo
{
    public class Program
    {
        static OutputTemplateTokenRenderer ThemedExceptionSupportingTemplateRendererFactory(
            MessageTemplate template, PropertyToken propertyToken, ConsoleTheme theme, IFormatProvider provider)
        {
            if (propertyToken.PropertyName == "ThemedException")
            {
                return new ThemedExceptionTokenRenderer(theme, propertyToken);
            }
            return OutputTemplateRenderer.CreateStandardDisplayOutputPropertiesRenderer(template, propertyToken, theme, provider);
        }

        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{ThemedException}",
                    outputTemplateTokenRendererFactory: ThemedExceptionSupportingTemplateRendererFactory)
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
