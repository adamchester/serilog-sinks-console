using System;
using Serilog.Sinks.SystemConsole.Output;
using Serilog.Events;
using System.IO;

namespace ConsoleDemo
{
    class EventSwitchingTemplateTokenRenderer : OutputTemplateTokenRenderer
    {
        readonly Func<LogEvent, OutputTemplateTokenRenderer> _getRendererForEvent;

        public EventSwitchingTemplateTokenRenderer(Func<LogEvent, OutputTemplateTokenRenderer> getRendererForEvent)
        {
            _getRendererForEvent = getRendererForEvent;
        }

        public override void Render(LogEvent logEvent, TextWriter output)
        {
            var renderer = _getRendererForEvent(logEvent);
            renderer?.Render(logEvent, output);
        }
    }
}
