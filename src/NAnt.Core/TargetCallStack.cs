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
using System.Collections.Specialized;
using System.Linq;

namespace NAnt.Core
{
    public class TargetCallStack : CallStack<TargetStackFrame>
    {
        /// <summary>
        /// Creates a new target call stack for this project
        /// </summary>
        /// <param name="project">The project</param>
        internal TargetCallStack(Project project) : base (project)
        {
            this.ThreadProperties = new PropertyDictionary(project, PropertyScope.Thread);
        }

        /// <summary>
        /// Gets the thread-scoped properties in this stack
        /// </summary>
        public PropertyDictionary ThreadProperties { get; private set; }

        /// <summary>
        /// Gets the <see cref="ITargetLogger"/> that should be used for the current point in the stack.
        /// </summary>
        public ITargetLogger HeadLogger
        {
            get
            {
                if (this.CurrentFrame == null)
                {
                    return this.Project;
                }

                return this.CurrentFrame.Logger;
            }
        }

        /// <summary>
        /// Pushes a new target frame onto this stack
        /// </summary>
        /// <param name="target">The target to push</param>
        /// <param name="logger">The logger that tasks on this frame shoudl use, if different than the current one</param>
        /// <returns>An <see cref="IDisposable"/> that, when disposed, pops the frame from the stack</returns>
        public IDisposable Push(Target target, ITargetLogger logger = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return this.PushNewFrame(
                new TargetStackFrame(target, this.Project, logger ?? this.CurrentFrame.Logger));
        }

        /// <summary>
        /// Gets the entire chain of tasks that lead to this task
        /// </summary>
        /// <returns>the entire chain of tasks that lead to this task</returns>
        public IEnumerable<TaskStackFrame> GetEntireTaskAncestry()
        {
            return this.Traverser.SelectMany(frame => frame.TaskCallStack.Traverser);
        }

        /// <summary>
        /// Clones this stack.  This method is probably thread-safe.
        /// </summary>
        public override object Clone()
        {
            var clone = new TargetCallStack(this.Project);
            PopulateClone(this, clone);

            clone.ThreadProperties.Inherit(this.ThreadProperties, new StringCollection());

            return clone;
        }

        /// <summary>
        /// Pushes the first frame onto the stack
        /// </summary>
        /// <returns><see cref="Push(Target)"/></returns>
        internal IDisposable PushRoot()
        {
            if (this.CurrentFrame == null)
            {
                return this.PushNewFrame(
                    new TargetStackFrame(null, this.Project, this.Project));
            }
            else
            {
                throw new InvalidOperationException("Only the first frame may be the root frame");
            }
        }
    }

    /// <summary>
    /// A frame in a <see cref="TargetCallStack"/>.  Each time a new target is called,
    /// a frame is added to the stack.
    /// </summary>
    public class TargetStackFrame: StackFrame
    {
        /// <summary>
        /// Creates a new frame
        /// </summary>
        /// <param name="target">The new target</param>
        /// <param name="project">The project</param>
        /// <param name="logger">The logger to be used</param>
        internal TargetStackFrame(
            Target target,
            Project project,
            ITargetLogger logger)
        {
            this.Target = target;
            this.TargetProperties = new PropertyDictionary(project, PropertyScope.Target);
            this.TaskCallStack = new TaskCallStack(project);
            this.Logger = logger;
        }

        /// <summary>
        /// The target-scoped properties
        /// </summary>
        public PropertyDictionary TargetProperties { get; private set; }

        /// <summary>
        /// The target this frame is for
        /// </summary>
        public Target Target { get; private set; }

        /// <summary>
        /// Gets a stack of all of the tasks that have been performed up to this point
        /// in this target.  Tasks such as if and mutex
        /// will be in here followed by tasks within them.
        /// </summary>
        public TaskCallStack TaskCallStack { get; private set; }

        /// <summary>
        /// Gets or sets the logger to be used for tasks in this frame
        /// </summary>
        internal ITargetLogger Logger { get; private set; }

        /// <summary>
        /// Clones this stack frame
        /// </summary>
        /// <returns>The clone</returns>
        internal override StackFrame Clone()
        {
            // Intentionally don't include the target properties
            return new TargetStackFrame(this.Target, this.TargetProperties.Project, this.Logger)
            {
                TaskCallStack = this.TaskCallStack.Clone() as TaskCallStack
            };
        }
    }
}
