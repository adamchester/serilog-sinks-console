// Copyright 2017 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using Serilog.Sinks.SystemConsole.Themes;

namespace Serilog.Sinks.SystemConsole.Output
{
    /// <summary>
    /// An <see cref="ITextFormatter"/> implementation which supports writing to a <see cref="TextWriter"/> such as
    /// <see cref="Console.Out"/> or <see cref="Console.Error"/> and optionally uses a <see cref="ConsoleTheme"/>
    /// to enhance readability. Output is formatted according on the provided output template.
    /// </summary>
    /// <remarks>
    /// The standard output template is "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}".
    /// Note that a <see cref="ConsoleTheme"/> may not support buffering (<see cref="ConsoleTheme.CanBuffer"/>). If
    /// buffering is supported, the styling is written into the text output.
    /// </remarks>
    public class OutputTemplateRenderer : ITextFormatter
    {
        /// <summary>
        /// Defines the signature of a method creates an <see cref="OutputTemplateTokenRenderer"/>. An implementation
        /// may return <see langword="null" /> to indicate that no renderer is available for the provided options, and
        /// ultimately a generic rendering will be used instead.
        /// </summary>
        /// <param name="outputTemplate">The full output template itself.</param>
        /// <param name="propertyToken">The output template property token for which the factory is creating a renderer.</param>
        /// <param name="theme">The output theme that will be used by the created renderer</param>
        /// <param name="formatProvider">The format provider which the created renderer will use (if appropriate)</param>
        /// <returns>An <see cref="OutputTemplateTokenRenderer"/> instance which renders the </returns>
        public delegate OutputTemplateTokenRenderer TokenRendererFactory(
            MessageTemplate outputTemplate, PropertyToken propertyToken, ConsoleTheme theme, IFormatProvider formatProvider);

        /// <summary>
        /// A map from output template property names (<see cref="OutputProperties"/>) to an
        /// <see cref="TokenRendererFactory"/> which can create the
        /// <see cref="OutputTemplateTokenRenderer"/> instance.
        /// </summary>
        static Dictionary<string, TokenRendererFactory> DefaultPropertyRenderers { get; } =
            new Dictionary<string, TokenRendererFactory>
            {
                [OutputProperties.LevelPropertyName]        = (template, pt, theme, formatProvider) => new LevelTokenRenderer(theme, pt),
                [OutputProperties.NewLinePropertyName]      = (template, pt, theme, formatProvider) => new NewLineTokenRenderer(pt.Alignment),
                [OutputProperties.ExceptionPropertyName]    = (template, pt, theme, formatProvider) => new ExceptionTokenRenderer(theme, pt),
                [OutputProperties.MessagePropertyName]      = (template, pt, theme, formatProvider) => new MessageTemplateOutputTokenRenderer(theme, pt, formatProvider),
                [OutputProperties.TimestampPropertyName]    = (template, pt, theme, formatProvider) => new TimestampTokenRenderer(theme, pt, formatProvider),
                [OutputProperties.PropertiesPropertyName]   = (template, pt, theme, formatProvider) => new PropertiesTokenRenderer(theme, pt, template, formatProvider),
            };

        /// <summary>
        /// Creates an <see cref="OutputTemplateTokenRenderer"/> for the provided <see cref="PropertyToken"/> for one of the
        /// standard <see cref="OutputProperties"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="OutputTemplateTokenRenderer" /> or <see langword="null"/> if the
        /// <paramref name="propertyToken"/> is not one of the standard output property names.
        /// </returns>
        public static OutputTemplateTokenRenderer NewStandardRenderer(
            MessageTemplate outputTemplate, PropertyToken propertyToken, ConsoleTheme theme, IFormatProvider formatProvider)
        {
            return DefaultPropertyRenderers.TryGetValue(propertyToken.PropertyName, out var rendererFactory)
                ? rendererFactory(outputTemplate, propertyToken, theme, formatProvider)
                : null;
        }

        /// <summary>
        /// Creates an <see cref="OutputTemplateTokenRenderer"/> for the provided <see cref="PropertyToken"/> in a
        /// in an output template (itself, a <see cref="MessageTemplate"/>) for the standard <see cref="OutputProperties"/>
        /// or <see langword="null"/> if the <paramref name="propertyToken"/> is not one of the standard output property names.
        /// </summary>
        public static OutputTemplateTokenRenderer NewEventPropertyTokenRenderer(
            MessageTemplate outputTemplate, PropertyToken propertyToken, ConsoleTheme theme, IFormatProvider formatProvider)
        {
            return new EventPropertyTokenRenderer(theme, propertyToken, formatProvider);
        }

        readonly OutputTemplateTokenRenderer[] _renderers;
        readonly ConsoleTheme _theme;

        internal bool CanBuffer => _theme.CanBuffer;

        /// <summary>
        /// For the love of developer happiness, document me, please!
        /// </summary>
        public OutputTemplateRenderer(ConsoleTheme theme, string outputTemplate, IFormatProvider formatProvider)
            : this(theme, outputTemplate, formatProvider, NewStandardRenderer)
        {
        }

        /// <summary>
        /// For the love of developer happiness, document me, please!
        /// </summary>
        public OutputTemplateRenderer(ConsoleTheme theme, string outputTemplate, IFormatProvider formatProvider, TokenRendererFactory rendererFactory)
        {
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));
            if (rendererFactory == null) throw new ArgumentNullException(nameof(rendererFactory));

            var template = new MessageTemplateParser().Parse(outputTemplate);

            var renderers = new List<OutputTemplateTokenRenderer>();
            foreach (var token in template.Tokens)
            {
                if (token is TextToken tt)
                {
                    renderers.Add(new TextTokenRenderer(theme, tt.Text));
                    continue;
                }

                var pt = (PropertyToken)token;
                renderers.Add(
                    rendererFactory(template, pt, theme, formatProvider)
                        ?? NewEventPropertyTokenRenderer(template, pt, theme, formatProvider));
            }

            _renderers = renderers.ToArray();
            _theme = theme ?? ConsoleTheme.None;
        }
        
        /// <summary>
        /// Renders the <see cref="LogEvent"/> to the <see cref="TextWriter"/> output.
        /// </summary>
        /// <param name="logEvent">The event to render</param>
        /// <param name="output">The destination of the rendered output text</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));

            foreach (var renderer in _renderers)
                renderer.Render(logEvent, output);
        }
    }
}
