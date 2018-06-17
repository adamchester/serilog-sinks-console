using System;
using System.IO;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.SystemConsole.Output;
using Serilog.Sinks.SystemConsole.Themes;

namespace ConsoleDemo
{

    class ThemedExceptionTokenRenderer : OutputTemplateTokenRenderer
    {
        const string StackFrameLinePrefix = "   ";

        readonly ConsoleTheme _theme;
        readonly PropertyToken _pt;
        readonly OutputTemplateTokenRenderer _fallbackExceptionRenderer;

        public ThemedExceptionTokenRenderer(ConsoleTheme theme, PropertyToken propertyToken, OutputTemplateTokenRenderer fallbackExceptionRenderer = null)
        {
            _theme = theme;
            _pt = propertyToken;
            _fallbackExceptionRenderer = fallbackExceptionRenderer;
        }

        public override void Render(LogEvent logEvent, TextWriter output)
        {
            // Padding is never applied by this renderer.
            if (logEvent.Exception == null)
                return;

            try
            {
                var parsedFrames = StackTraceParser.Parse(
                    logEvent.Exception.StackTrace,
                    (frame, type, method, paramList, parameters, file, line) => new
                        { frame, type, method, paramList, parameters, file, line });

                var _ = 0;
                
                using (_theme.Apply(output, ConsoleThemeStyle.SecondaryText, ref _))
                    output.Write(logEvent.Exception.GetType().FullName);

                using (_theme.Apply(output, ConsoleThemeStyle.TertiaryText, ref _))
                    output.Write(": ");

                using (_theme.Apply(output, ConsoleThemeStyle.Text, ref _))
                    output.WriteLine(logEvent.Exception.Message);

                foreach (var frame in parsedFrames)
                {
                    using (_theme.Apply(output, ConsoleThemeStyle.TertiaryText, ref _))
                        output.Write("   at ");

                    if (string.IsNullOrEmpty(frame.file) || string.IsNullOrEmpty(frame.line))
                    {
                        // just render the regular old boring frame
                        using (_theme.Apply(output, ConsoleThemeStyle.TertiaryText, ref _))
                            output.WriteLine(frame.frame);
                    }
                    else
                    {
                        /* {{ frame = ConsoleDemo.Program.Fail() in C:\My\repo\serilog-sinks-console\sample\ConsoleDemo\Program.cs:line 39, type = ConsoleDemo.Program, method = Fail, paramList = (), parameters = System.Linq.Enumerable+SelectIPartitionIterator`2[System.Int32,System.Collections.Generic.KeyValuePair`2[System.String,System.String]], file = C:\My\repo\serilog-sinks-console\sample\ConsoleDemo\Program.cs, line = 39 }}
                            file: "C:\\My\\repo\\serilog-sinks-console\\sample\\ConsoleDemo\\Program.cs"
                            frame: "ConsoleDemo.Program.Fail() in C:\\My\\repo\\serilog-sinks-console\\sample\\ConsoleDemo\\Program.cs:line 39"
                            line: "39"
                            @method: "Fail"
                            paramList: "()"
                            parameters: {System.Linq.Enumerable.SelectIPartitionIterator<int, System.Collections.Generic.KeyValuePair<string, string>>}
                            @type: "ConsoleDemo.Program"
                        */
                        using (_theme.Apply(output, ConsoleThemeStyle.Text, ref _))
                            output.Write(frame.type);

                        using (_theme.Apply(output, ConsoleThemeStyle.TertiaryText, ref _))
                            output.Write(".");

                        using (_theme.Apply(output, ConsoleThemeStyle.Name, ref _))
                            output.Write(frame.method);

                        using (_theme.Apply(output, ConsoleThemeStyle.TertiaryText, ref _))
                            output.Write(frame.paramList);

                        using (_theme.Apply(output, ConsoleThemeStyle.TertiaryText, ref _))
                            output.Write(" in ");

                        using (_theme.Apply(output, ConsoleThemeStyle.String, ref _))
                            output.Write(frame.file);

                        using (_theme.Apply(output, ConsoleThemeStyle.TertiaryText, ref _))
                            output.Write(":line ");

                        using (_theme.Apply(output, ConsoleThemeStyle.Number, ref _))
                            output.Write(frame.line);

                        output.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Debugging.SelfLog.WriteLine(
                    "Exception while writing themed exception: {0}", ex.ToString());

                _fallbackExceptionRenderer?.Render(logEvent, output);
            }

        }
    }
}