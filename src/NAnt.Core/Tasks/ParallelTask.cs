using NAnt.Core.Attributes;
using NAnt.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NAnt.Core.Tasks
{
    /// <summary>
    /// Children of this task are run in parallel.
    /// </summary>
    [TaskName("parallel")]
    public class ParallelTask : Task
    {
        /// <summary>
        /// The targets to execute in parallel
        /// </summary>
        protected List<Element> Children { get; set; } = new List<Element>();

        /// <summary>
        /// If true, the targets within this parallel will not run in parallel but instead will run in sequential order.
        /// </summary>
        /// <remarks>
        /// This property makes this <see cref="ParallelTask"/> behave identically to a <see cref="SequenceTask"/> and 
        /// in fact the <see cref="SequenceTask"/> simply uses the behacior of a <see cref="ParallelTask"/> with this 
        /// property set to <c>false</c>.
        /// </remarks>
        [TaskAttribute("forceSequential", Required = false)]
        [BooleanValidator]
        public virtual Boolean RunInSerial { get; set; }

        /// <summary>
        /// Adds a new <see cref="ParallelTarget"/> child to this parallel task.
        /// </summary>
        /// <param name="path">The <see cref="PathSet" /> to add.</param>
        [BuildElement("pcall")]
        public virtual void AddPath(ParallelTarget path)
        {
            this.Children.Add(path);
        }

        /// <summary>
        /// Adds a new sequence task to this task
        /// </summary>
        /// <param name="task">The sequence task</param>
        [BuildElement("sequence")]
        public void AddTask(SequenceTask task)
        {
            this.Children.Add(task);
        }

        /// <summary>
        /// Adds a new parallel task to this task.
        /// </summary>
        /// <param name="task">The parallel task</param>
        [BuildElement("parallel")]
        public void AddTask(ParallelTask task)
        {
            this.Children.Add(task);
        }

        /// <summary>
        /// The description of this task
        /// </summary>
        [TaskAttribute("description", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public String Description { get; set; } = String.Empty;

        /// <summary>
        /// Executes this task
        /// </summary>
        protected override void ExecuteTask()
        {
            this.Log(Level.Info, $"Begining {(this.RunInSerial ? "sequential" : "parallel")} execution of targets: {this.Description}");
            this.Project.Indent();

            // Targets can be either the ParallelTargets or SequenceTasks or ParallelTasks
            var targets = this.Children.Where(t => t is ParallelTask ||  (t as ParallelTarget).IfDefined && !(t as ParallelTarget).UnlessDefined);

            // Sequential execution is simple
            if (this.RunInSerial || this.Project.ForceSequential)
            {
                foreach (var element in targets)
                {
                    // If it's a pcall, execute the target
                    if (element is ParallelTarget)
                    {
                        var targetElement = element as ParallelTarget;
                        this.Log(Level.Info, $"Executing \"{ targetElement.TargetName}\" in sequence.");
                        this.Project.Execute(targetElement.TargetName, this, this.CallStack);
                    }
                    else
                    {
                        // otherwise execute the tasks as is
                        (element as ParallelTask).Execute();
                    }
                }
            }
            else  // Parallel execution
            {
                try
                {
                    Parallel.ForEach(targets, (element, state) =>
                    {
                        try
                        {
                            // If it's a pcall, execute the target
                            if (element is ParallelTarget)
                            {
                                var targetElement = element as ParallelTarget;
                                this.Log(Level.Info, $"Executing \"{ targetElement.TargetName}\" in parallel.");

                                if (!state.IsExceptional && !state.IsStopped)
                                {
                                    this.Project.Execute(targetElement.TargetName, this, this.CloneCallStack());
                                }
                            }
                            else
                            {
                                // otherwise execute the tasks as is
                                (element as ParallelTask).ExecuteTask();
                            }
                        }
                        catch (Exception e) // This catch is inside the foreach and so we do some first-chance handling here
                        {
                            // Try to stop other threads from starting.
                            state.Stop();
                            var targetElement = element as ParallelTarget;

                            // Only log if this is not another parallel task
                            if (targetElement != null)
                            {
                                lock (this) // Prevent ourself from jumbling our logging, at least
                                {
                                    var message = $"ERROR: An exception has been thrown by the \"{targetElement.TargetName}\" target of the parallel.  Any currently executing targets will run to completion but the build will fail.";

                                    this.Log(Level.Error, new string('=', message.Length));
                                    this.Log(Level.Error, message);
                                    this.Log(Level.Error, e.Message);
                                    this.Log(Level.Error, new string('=', message.Length) + "\r\n");
                                    throw;
                                }
                            }
                            else
                            {
                                throw;
                            }
                        }
                    });
                }
                catch (AggregateException agge) // This catch is outside the foreach so we only have aggregate exceptions but we know for sure that everything is stopped now.
                {
                    foreach (var inner in agge.InnerExceptions)
                    {
                        if (inner is BuildException)
                        {
                            throw inner;
                        }
                        else
                        {
                            throw new BuildException(inner.Message, inner);
                        }
                    }
                }
            }

            this.Project.Unindent();
        }

        /// <summary>
        /// Clones the call stack of this <see cref="Task"/>
        /// </summary>
        /// <returns>The cloned call stack</returns>
        private TargetCallStack CloneCallStack()
        {
            return this.CallStack.Clone() as TargetCallStack;
        }
    }
}
