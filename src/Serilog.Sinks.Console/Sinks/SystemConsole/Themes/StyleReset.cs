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
using System.IO;

namespace Serilog.Sinks.SystemConsole.Themes
{
    /// <summary>
    /// Allows an applied <see cref="ConsoleTheme"/> style to be reset.
    /// </summary>
    public struct StyleReset : IDisposable
    {
        readonly ConsoleTheme _theme;
        readonly TextWriter _output;

        /// <summary>
        /// Ctor.
        /// </summary>
        public StyleReset(ConsoleTheme theme, TextWriter output)
        {
            _theme = theme;
            _output = output;
        }

        /// <summary>
        /// Resets the style.
        /// </summary>
        public void Dispose()
        {
            _theme.Reset(_output);
        }
    }
}