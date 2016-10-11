using NAnt.Core.Attributes;
using NAnt.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NAnt.Core.Tasks
{
    [TaskName("parallel")]
    public class ParallelTask : Task
    {
        /// <summary>
        /// The targets to execute in parallel
        /// </summary>
        internal List<ParallelTarget> Targets { get; set; } = new List<ParallelTarget>();

        /// <summary>
        /// If true, the targets within this parallel will not run in parallel but instead in order.
        /// </summary>
        [TaskAttribute("serial", Required = false)]
        [BooleanValidator]
        public Boolean RunInSerial { get; set; }

        /// <summary>
        /// Defines a set of path elements to add to the current path.
        /// </summary>
        /// <param name="path">The <see cref="PathSet" /> to add.</param>
        [BuildElement("pCall")]
        public void AddPath(ParallelTarget path)
        {
            this.Targets.Add(path);
        }

        /// <summary>
        /// Executes this task
        /// </summary>
        protected override void ExecuteTask()
        {
            this.Log(Level.Info, "Begining parallel execution of targets...");
            this.Project.Indent();

            var targets = this.Targets.Where(t => t.IfDefined && !t.UnlessDefined);

            if (this.RunInSerial)
            {
                foreach (var targetElement in targets)
                {
                    this.Log(Level.Info, $"Executing \"{ targetElement.TargetName}\" in sequence.");
                    this.Project.Execute(targetElement.TargetName, this);
                }
            }
            else
            {
                try
                {
                    Parallel.ForEach(targets, (targetElement, state) =>
                    {
                        try
                        {
                            this.Log(Level.Info, $"Executing \"{ targetElement.TargetName}\" in parallel.");

                            if (!state.IsExceptional && !state.IsStopped)
                            {
                                this.Project.Execute(targetElement.TargetName, this);
                            }
                        }
                        catch (Exception e)
                        {
                            state.Stop();

                            lock (this) // Prevent ourself from jumbling our loging, at least
                            {
                                var message = $"ERROR: An exception has been thrown by the \"{targetElement.TargetName}\" target of the parallel.  Any currently executing targets will run to completion but the build will fail.";

                                this.Log(Level.Error, new string('=', message.Length));
                                this.Log(Level.Error, message);
                                this.Log(Level.Error, e.Message);
                                this.Log(Level.Error, new string('=', message.Length) + "\r\n");
                                throw;
                            }
                        }
                    });
                }
                catch (AggregateException agge)
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
    }
}
