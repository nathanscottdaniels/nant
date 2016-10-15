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

namespace NAnt.Core.Types
{
    [Serializable]
    [ElementName("pcall")]
    public class ParallelTarget : DataTypeBase
    {
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
        public bool IfDefined { get; set; } = true;

        /// <summary>
        /// Indicates if the option should not be passed to the task.
        /// If <see langword="false" /> then the option will be passed; 
        /// otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined { get; set; } = false;

        /// <summary>
        /// Execute the specified targets dependencies -- even if they have been 
        /// previously executed. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("cascade")]
        public bool CascadeDependencies { get; set; } = true;
    }
}
