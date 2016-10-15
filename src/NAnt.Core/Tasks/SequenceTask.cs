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

namespace NAnt.Core.Tasks
{
    /// <summary>
    /// A simple subclass of <see cref="ParallelTask"/> that alwys runs its children in sequence.
    /// </summary>
    [TaskName("sequence")]
    public class SequenceTask : ParallelTask
    {
        /// <summary>
        /// Executes this task
        /// </summary>
        protected override void ExecuteTask()
        {
            this.RunInSerial = true;
            base.ExecuteTask();
        }

        /// <summary>
        /// Returns true and throw a <see cref="BuildException"/> if attempted to be set;
        /// </summary>
        public override bool RunInSerial
        {
            get
            {
                return true;
            }

            set
            {
                if (!value)
                {
                    throw new BuildException("Cannot set forceSequential to false on a sequence task");
                }
            }
        }
    }
}
