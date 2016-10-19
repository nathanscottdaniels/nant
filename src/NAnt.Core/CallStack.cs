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
    /// <summary>
    /// Base implementation of a call stack
    /// </summary>
    /// <typeparam name="TFrame">The type of stack frame</typeparam>
    public abstract class CallStack<TFrame> : ICloneable where TFrame : StackFrame
    {
        /// <summary>
        /// Creates a new call stack
        /// </summary>
        /// <param name="project">The project</param>
        protected internal CallStack(Project project)
        {
            this.Project = project;
            this.InnerStack = new Stack<TFrame>();
        }

        /// <summary>
        /// Peeks at the top frame in this stack
        /// </summary>
        public TFrame CurrentFrame
        {
            get
            {
                try
                {
                    return this.InnerStack.Peek();
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Allows traversal of the stack without affecting the elements
        /// </summary>
        public IEnumerable<TFrame> Traverser
        {
            get
            {
                return this.InnerStack;
            }
        }

        /// <summary>
        /// The project
        /// </summary>
        protected Project Project { get; private set; }

        /// <summary>
        /// The backing stack
        /// </summary>
        private Stack<TFrame> InnerStack { get; set; }

        /// <summary>
        /// Pushes a new frame onto this stack
        /// </summary>
        /// <param name="frame">The frame to push</param>
        /// <returns>An <see cref="IDisposable"/> that, when disposed, pops the frame from the stack</returns>
        protected IDisposable PushNewFrame(TFrame frame)
        {
            this.Push(frame);
            return new StackPopper(this);
        }

        /// <summary>
        /// Pushes a new frame onto this stack
        /// </summary>
        /// <param name="stackFrame"></param>
        private void Push(TFrame stackFrame)
        {
            this.InnerStack.Push(stackFrame);
        }

        /// <summary>
        /// Pops the top frame from the stack
        /// </summary>
        /// <returns>The frame popped</returns>
        private TFrame Pop()
        {
            return this.InnerStack.Pop();
        }

        /// <summary>
        /// Clones this stack
        /// </summary>
        /// <returns>The cloned stack</returns>
        public abstract object Clone();

        /// <summary>
        /// Clones this stack.  This method is thread-safe.
        /// </summary>
        protected static void PopulateClone(CallStack<TFrame> original, CallStack<TFrame> clone)
        {
            clone.InnerStack = new Stack<TFrame>(new Stack<TFrame>(
                original.InnerStack.Select(frame => frame.Clone() as TFrame)));
        }

        /// <summary>
        /// A simple class that pops a frame from a stack when disposed
        /// </summary>
        private class StackPopper : IDisposable
        {
            /// <summary>
            /// The targeted stack
            /// </summary>
            private readonly CallStack<TFrame> callStack;

            /// <summary>
            /// Creates a new popper
            /// </summary>
            /// <param name="stack">The targeted stack</param>
            public StackPopper(CallStack<TFrame> stack)
            {
                this.callStack = stack;
            }

            /// <summary>
            /// Pops the last item in the stack
            /// </summary>
            public void Dispose()
            {
                this.callStack.Pop();
            }
        }
    }

    /// <summary>
    /// Defines a frame in a <see cref="CallStack{TFrame}"/>
    /// </summary>
    public abstract class StackFrame
    {
        internal abstract StackFrame Clone();
    }
}
