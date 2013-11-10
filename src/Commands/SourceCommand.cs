/*
 * SDB - Mono Soft Debugger Client
 * Copyright 2013 Alex Rønne Petersen
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mono.Debugger.Client.Commands
{
    sealed class SourceCommand : Command
    {
        public override string[] Names
        {
            get { return new[] { "source", "src" }; }
        }

        public override string Summary
        {
            get { return "Show the source for the current stack frame."; }
        }

        public override string Syntax
        {
            get { return "source|src [lower] [upper]"; }
        }

        public override void Process(string args)
        {
            var f = Debugger.ActiveFrame;

            if (f == null)
            {
                Log.Error("No active stack frame");
                return;
            }

            var lower = 10;
            var upper = 10;

            var lowerStr = args.Split(' ').Where(x => x != string.Empty).FirstOrDefault();

            if (lowerStr != null)
            {
                if (!int.TryParse(lowerStr, out lower))
                {
                    Log.Error("Invalid lower bound value");
                    return;
                }

                lower = System.Math.Abs(lower);

                var upperStr = new string(args.Skip(lowerStr.Length).ToArray()).Trim();

                if (upperStr.Length != 0)
                {
                    if (!int.TryParse(upperStr, out upper))
                    {
                        Log.Error("Invalid upper bound value");
                        return;
                    }
                }
            }

            var loc = f.SourceLocation;
            var file = loc.FileName;
            var line = loc.Line;

            if (file != null && line != -1)
            {
                if (!File.Exists(file))
                {
                    Log.Error("Source file '{0}' not found", file);
                    return;
                }

                StreamReader reader;

                try
                {
                    reader = File.OpenText(file);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not open source file '{0}'", file);
                    Log.Error(ex.ToString());

                    return;
                }

                try
                {
                    var exec = Debugger.CurrentExecutable;

                    if (exec != null && File.GetLastWriteTime(file) > exec.LastWriteTime)
                        Log.Notice("Source file '{0}' is newer than the debuggee executable", file);

                    var cur = 0;

                    while (!reader.EndOfStream)
                    {
                        var str = reader.ReadLine();

                        var i = line - cur;
                        var j = cur - line;

                        if (i > 0 && i < lower + 2 || j >= 0 && j < upper)
                        {
                            PrintLine(cur + 1, str, cur == line - 1);
                        }

                        cur++;
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            }
            else
                Log.Error("No source information available");
        }

        // List of keywords to highlight
        static private HashSet<string> Keywords = new HashSet<string>() {
            // C#
            "class", "struct", "interface", "using", "if", "else", "for",
            "while", "try", "catch", "finally", "new", "as", "is", "ref", 
            "out", "private", "protected", "public", "internal", "virtual", 
            "override",
            // Boo
            "def", "import", "from", "elif", "unless", "except", "ensure", 
            "macro", "isa", "yield", "do", "callable", "and", "or", "not",
            "in"
        };
        
        // List of constants to highlight
        static private HashSet<string> Constants = new HashSet<string>() {
            // C#
            "true", "false", "null",
            "int", "uint", "double", "bool", "string", "object",
            // Boo
            "print", "property"
        };

        // Styles order must match the capturing groups in the regex
        static private string LexerRegex = "\\b([A-Za-z_]+[A-Za-z0-9_]*)\\b|\\b(\\d+(?:\\.\\d+)?)\\b|('[^']*')|(\"[^\"]*\")|(#.*|//.*)|([=\\+\\*/~<>\\?^|-]+)|(.)";
        static private ConsoleColor[] Styles = new ConsoleColor[] {
            ConsoleColor.White,    // idents
            ConsoleColor.Magenta,  // numbers
            ConsoleColor.Yellow,   // single quoted
            ConsoleColor.Yellow,   // double quoted
            ConsoleColor.DarkCyan, // comments
            ConsoleColor.Red,      // operators
            ConsoleColor.Cyan      // anything else
        };

        private void PrintLine(int ln, string code, bool selected)
        {
            Console.ForegroundColor = selected ? ConsoleColor.Yellow : ConsoleColor.Gray;

            if (selected)
                Console.Write(" -->");
            else
                Console.Write("{0,3} ", ln);

            // Simple regex based highlighting
            foreach (Match match in Regex.Matches(code, LexerRegex))
            {
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    Group grp = match.Groups[i];
                    if (grp.Success)
                    {
                        if (Keywords.Contains(grp.Value))
                            Console.ForegroundColor = ConsoleColor.DarkBlue;
                        else if (Constants.Contains(grp.Value))
                            Console.ForegroundColor = ConsoleColor.Green;
                        else
                            Console.ForegroundColor = Styles[i - 1];

                        Console.Write(grp.Value);
                        break;
                    }
                }
            }

            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
