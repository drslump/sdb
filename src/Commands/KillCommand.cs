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

using System.Collections.Generic;

namespace Mono.Debugger.Client.Commands
{
    sealed class KillCommand : Command
    {
        public override string[] Names
        {
            get { return new[] { "kill" }; }
        }

        public override string Summary
        {
            get { return "Kill the inferior process."; }
        }

        public override string Syntax
        {
            get { return "kill|stop"; }
        }

        public override void Process(string args)
        {
            if (Debugger.State == State.Exited)
                Log.Error("No inferior process");
            else
                Debugger.Kill();
        }
    }
}
