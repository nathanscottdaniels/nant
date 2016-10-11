using NAnt.Core.Attributes;
using NAnt.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        /// Defines a set of path elements to add to the current path.
        /// </summary>
        /// <param name="path">The <see cref="PathSet" /> to add.</param>
        [BuildElement("pCall")]
        public void AddPath(ParallelTarget path)
        {
            this.Targets.Add(path);
        }

        protected override void ExecuteTask()
        {
            this.Project.Log(Level.Info, "Begining parallel execution of targets...");
            this.Project.Indent();

            try
            {
                Parallel.ForEach(this.Targets.Where(t => t.IfDefined && !t.UnlessDefined), (targetElement, state) =>
                {
                    try
                    {
                        this.Project.Log(Level.Info, $"Parallel: Executing \"{ targetElement.TargetName}\" in parallel.");

                        if (!state.IsExceptional && !state.IsStopped)
                        {
                            this.Project.Execute(targetElement.TargetName);
                        }
                    }
                    catch (Exception e)
                    {
                        state.Stop();

                        var message = $"ERROR: An exception has been thrown by the \"{targetElement.TargetName}\" target of the parallel.  Any currently executing targets will run to completion but the build will fail.";

                        this.Project.Log(Level.Error, new string('=', message.Length));
                        this.Project.Log(Level.Error, message);
                        this.Project.Log(Level.Error, new string('=', message.Length) + "\r\n");
                        throw;
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
                }
            }

            this.Project.Unindent();
        }

        protected override void Initialize()
        {
            base.Initialize();
        }
    }
}
