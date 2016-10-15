using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public PropertyDictionary ThreadProperties { get; }

        /// <summary>
        /// Pushes a new target frame onto this stack
        /// </summary>
        /// <param name="target">The target to push</param>
        /// <returns>An <see cref="IDisposable"/> that, when disposed, pops the frame from the stack</returns>
        public IDisposable Push(Target target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return this.PushNewFrame(
                new TargetStackFrame(target, this.Project));
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
        /// Clones this stack
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
                    new TargetStackFrame(null, this.Project));
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
        internal TargetStackFrame(
            Target target,
            Project project)
        {
            this.Target = target;
            this.TargetProperties = new PropertyDictionary(project, PropertyScope.Target);
            this.TaskCallStack = new TaskCallStack(project);
        }

        /// <summary>
        /// The target-scoped properties
        /// </summary>
        public PropertyDictionary TargetProperties { get; }

        /// <summary>
        /// The target this frame is for
        /// </summary>
        public Target Target { get; }

        /// <summary>
        /// Gets a stack of all of the tasks that have been performed up to this point
        /// in this target.  Tasks such as <see cref="IfTask"/> and <see cref="MutexTask"/> 
        /// will be in here followed by tasks within them.
        /// </summary>
        public TaskCallStack TaskCallStack { get; private set; }

        /// <summary>
        /// Clones this stack frame
        /// </summary>
        /// <returns>The clone</returns>
        internal override StackFrame Clone()
        {
            // Intentionally don't include the target properties
            return new TargetStackFrame(this.Target, this.TargetProperties.Project)
            {
                TaskCallStack = this.TaskCallStack.Clone() as TaskCallStack
            };
        }
    }
}
