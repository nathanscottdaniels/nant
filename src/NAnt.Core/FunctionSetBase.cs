// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
// Original NAnt Copyright (C) 2001-2003 Gerry Shaw
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
//
// Ian MacLean (imaclean@gmail.com)

namespace NAnt.Core {
    /// <summary>
    /// Base class for implementing NAnt functions.
    /// </summary>
    public abstract class FunctionSetBase {

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSetBase"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        protected FunctionSetBase(Project project, PropertyAccessor properties, TargetCallStack callStack) {
            _project = project;
            this.PropertyAccesor = properties;
            this.CallStack = callStack;
        }


        /// <summary>
        /// Gets or sets the <see cref="Project" /> that this functionset will 
        /// reference.
        /// </summary>
        /// <value>
        /// The <see cref="Project" /> that this functionset will reference.
        /// </value>
        public virtual Project Project {
            get { return _project; }
            set { _project = value; }
        }

        protected PropertyAccessor PropertyAccesor { get; private set; }

        protected TargetCallStack CallStack { get; private set; }

        private Project _project;
    }
}
