// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
using System;
using System.Globalization;
using System.IO;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.Core.Configuration {
    [Serializable]
    internal class RuntimeEngine : Element {
        private FileInfo _program;
        private ArgumentCollection _arguments = new ArgumentCollection();

        [TaskAttribute ("program", Required=true)]
        public FileInfo Program {
            get { return _program; }
            set { _program = value; }
        }

        /// <summary>
        /// The command-line arguments for the runtime engine.
        /// </summary>
        [BuildElementArray("arg")]
        public ArgumentCollection Arguments {
            get { return _arguments; }
        }

        protected override void Initialize() {
            base.Initialize ();

            if (Program != null & !Program.Exists) {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture, "Runtime engine '{0}'" +
                    " does not exist.", Program.FullName));
            }
        }
    }
}
