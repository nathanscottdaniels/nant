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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAnt.Core
{
    public class TaskCallStack : CallStack<TaskStackFrame>
    {
        /// <summary>
        /// Creates a new task stack
        /// </summary>
        /// <param name="project">The project</param>
        internal TaskCallStack(Project project)
            : base(project)
        {
        }

        /// <summary>
        /// Pushes a new task frame onto this stack
        /// </summary>
        /// <param name="task">The task to push</param>
        /// <returns>An <see cref="IDisposable"/> that, when disposed, pops the frame from the stack</returns>
        public IDisposable Push(Task task)
        {
            return this.PushNewFrame(new TaskStackFrame(task));
        }

        /// <summary>
        /// Clones this stack
        /// </summary>
        public override object Clone()
        {
            var clone = new TaskCallStack(this.Project);
            PopulateClone(this, clone);
            return clone;
        }
    }

    /// <summary>
    /// A frame in the <see cref="TaskCallStack"/>
    /// </summary>
    public class TaskStackFrame : StackFrame
    {
        /// <summary>
        /// Creates a new frame
        /// </summary>
        /// <param name="task">The new task</param>
        internal TaskStackFrame(Task task)
        {
            this.Task = task;
        }
        
        /// <summary>
        /// The task this frame is for
        /// </summary>
        public Task Task { get; }

        /// <summary>
        /// Clones this stack frame
        /// </summary>
        /// <returns>The clone</returns>
        internal override StackFrame Clone()
        {
            return new TaskStackFrame(this.Task);
        }
    }
}
