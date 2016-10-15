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
using NAnt.Core.Attributes;

namespace NAnt.Core.Configuration {
    [Serializable]
    internal class Runtime : Element {
        private ManagedExecutionModes _modes = new ManagedExecutionModes ();
        private DirList _probingPaths = new DirList();

        [BuildElement("probing-paths")]
        public DirList ProbingPaths {
            get { return _probingPaths; }
        }

        [BuildElement("modes")]
        public ManagedExecutionModes Modes {
            get { return _modes; }
        }
    }
}
