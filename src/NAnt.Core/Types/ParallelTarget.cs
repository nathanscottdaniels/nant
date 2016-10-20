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
using NAnt.Core.Attributes;
using System;
using System.Collections.Generic;

namespace NAnt.Core.Types
{
    /// <summary>
    /// A target call within a parallel task
    /// </summary>
    [Serializable]
    [ElementName("pcall")]
    public class ParallelTarget : DataTypeBase
    {
        /// <summary>
        /// Creates a new target
        /// </summary>
        public ParallelTarget()
        {
            this.IfDefined = true;
            this.CascadeDependencies = true;
            this.Arguments = new List<CallArgument>();
        }

        /// <summary>
        /// Value of the option. The default is <see langword="null" />.
        /// </summary>
        [TaskAttribute("target")]
        public string TargetName { get; set; }

        /// <summary>
        /// Indicates if the option should be passed to the task. 
        /// If <see langword="true" /> then the option will be passed; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined { get; set; }

        /// <summary>
        /// Indicates if the option should not be passed to the task.
        /// If <see langword="false" /> then the option will be passed; 
        /// otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined { get; set; } 

        /// <summary>
        /// Execute the specified targets dependencies -- even if they have been 
        /// previously executed. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("cascade")]
        public bool CascadeDependencies { get; set; }

        /// <summary>
        /// Gets the list of arguments to pass to the target
        /// </summary>
        public IList<CallArgument> Arguments { get; private set; }

        /// <summary>
        /// Adds a new argument to be passed to the target
        /// </summary>
        /// <param name="arg"></param>
        [BuildElement("argument", Required = false)]
        public void AddArgument(CallArgument arg)
        {
            this.Arguments.Add(arg);
        }
    }
}
