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
    public class ParallelTask : Task, IEquatable<ParallelTask>
    {

        public ParallelTask()
        {
            this.Children = new List<Element>();
            this.ShortName = String.Empty;
            this.Description = String.Empty;
        }

        /// <summary>
        /// The targets to execute in parallel
        /// </summary>
        protected List<Element> Children { get; set; }

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
        /// The description of this task
        /// </summary>
        [TaskAttribute("description", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public String Description { get; set; }

        /// <summary>
        /// The name of this task
        /// </summary>
        [TaskAttribute("name", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public String ShortName { get; set; }

        /// <summary>
        /// Gets the name of this task
        /// </summary>
        public override string Name
        {
            get
            {
                if (String.IsNullOrWhiteSpace(this.ShortName))
                {
                    return base.Name;
                }

                return this.ShortName;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not logging from child targets should happen simultaenoulsy 
        /// instead of only once a target completes.
        /// </summary>
        [TaskAttribute("cacophony")]
        [BooleanValidator()]
        public Boolean Cacophony { get; set; }

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
        /// Perform initial checks
        /// </summary>
        private void ValidateContext()
        {
            base.Initialize();
            if (this.Cacophony && this.Project.BuildListeners.ContainsType<XmlLogger>())
            {
                throw new BuildException("Cacophony logging is impossible with the XmlLogger logger");
            }

            foreach (var parent in this.CallStack.GetEntireTaskAncestry())
            {
                if (parent.Task is ParallelTask && parent.Task != this && (parent.Task as ParallelTask).Equals(this))
                {
                    throw new BuildException("Infinite loop detected!  Parallel/Sequence task \"" + this.ShortName + "\" is indirectly being called by itself.");
                }
            }
        }

        /// <summary>
        /// Executes this task
        /// </summary>
        protected override void ExecuteTask()
        {
            this.Log(Level.Info, "Begining " + (this.RunInSerial ? "sequential" : "parallel") + " execution [" + this.Name + " : " + this.Description + "]");
            this.ValidateContext();
            this.Logger.Indent();

            var cacophony = this.Cacophony;

            // Targets can be either the ParallelTargets or SequenceTasks or ParallelTasks
            var targets = this.Children.Where(t => t is ParallelTask || (t as ParallelTarget).IfDefined && !(t as ParallelTarget).UnlessDefined).ToArray();

            var concurrencyState = new ConcurrencyState()
            {
                Loggers = new BufferingTargetLogger[targets.Length],
                CompletedList = new bool[targets.Length]
            };

            // Sequential execution is simple
            if (this.RunInSerial || this.Project.ForceSequential)
            {
                for (var i = 0; i < targets.Length; i++)
                {
                    var element = targets[i];

                    // If it's a pcall, execute the target
                    if (element is ParallelTarget)
                    {
                        var targetElement = element as ParallelTarget;

                        this.Logger.Unindent();
                        this.Log(Level.Info, "Executing \"" + targetElement.TargetName + "\" in sequence.");
                        this.Logger.Indent();
                        try
                        {
                            this.Project.Execute(targetElement.TargetName, targetElement.CascadeDependencies, this, this.CloneCallStack(), this.Logger, targetElement.Arguments);
                        }
                        catch (ArgumentException e)
                        {
                            throw new BuildException(e.Message, this.Location);
                        }
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
                                this.Log(Level.Info, "Executing \"" + targetElement.TargetName + "\" in parallel.");

                                if (!state.IsExceptional && !state.IsStopped)
                                {
                                    this.ExecuteWithCorrectLogging(
                                        targetElement,
                                        cacophony,
                                        concurrencyState,
                                        Array.IndexOf(targets, element));
                                }
                            }
                            else
                            {
                                // otherwise execute the tasks as is
                                this.ExecuteWithCorrectLogging(element as ParallelTask, cacophony, concurrencyState, Array.IndexOf(targets, element));
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
                                    this.Logger.Unindent();
                                    var message = "ERROR: An exception has been thrown by the \"" + targetElement.TargetName + "\" target of the parallel.  Any currently executing targets will run to completion but the build will fail.";

                                    this.Log(Level.Error, new string('=', message.Length));
                                    this.Log(Level.Error, message);
                                    this.Log(Level.Error, e.Message);
                                    this.Log(Level.Error, new string('=', message.Length) + "\r\n");
                                    this.Logger.Indent();
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
                    throw new BuildException(
                        String.Concat(
                            "The following build errors were encountered during the parallel execution of targets:",
                            Environment.NewLine,
                            Environment.NewLine,
                            String.Join(
                                String.Concat(
                                    Environment.NewLine,
                                    Environment.NewLine),
                                agge.InnerExceptions.Select(ex => ex.Message)),
                        Environment.NewLine,
                        "Please look earlier in the log to see the contexts in which these errors occurred"));
                }
            }

            this.Logger.Unindent();
        }

        /// <summary>
        /// Executes a parallel target using the correct <see cref="ITargetLogger"/>
        /// </summary>
        /// <param name="pcall">The target</param>
        /// <param name="cacophony">Whether or not cacophany logging is requested</param>
        /// <param name="state">The concurrency state used to synchonize the threads</param>
        /// <param name="index">The index of this thread in the grand scheme of things</param>
        private void ExecuteWithCorrectLogging(ParallelTarget pcall, Boolean cacophony, ConcurrencyState state, Int32 index)
        {
            try
            {
                this.ExecuteWithCorrectLogging(
                    () => this.Project.Execute(pcall.TargetName, pcall.CascadeDependencies, this, this.CloneCallStack(), this.Logger, pcall.Arguments),
                    logger => this.Project.Execute(pcall.TargetName, pcall.CascadeDependencies, this, this.CloneCallStack(), logger, pcall.Arguments),
                    cacophony,
                    state,
                    index);
            }
            catch (ArgumentException e)
            {
                throw new BuildException(e.Message, this.Location);
            }
        }

        /// <summary>
        /// Executes a parallel task using the correct <see cref="ITargetLogger"/>
        /// </summary>
        /// <param name="task">The task to execute</param>
        /// <param name="cacophony">Whether or not cacophany logging is requested</param>
        /// <param name="state">The concurrency state used to synchonize the threads</param>
        /// <param name="index">The index of this thread in the grand scheme of things</param>
        private void ExecuteWithCorrectLogging(ParallelTask task, Boolean cacophony, ConcurrencyState state, Int32 index)
        {
            task.Logger = this.Logger;
            this.ExecuteWithCorrectLogging(
                task.Execute,
                logger =>
                {
                    task.Logger = logger;
                    task.Execute();
                },
                cacophony,
                state,
                index);
        }

        /// <summary>
        /// Executes something using the correct <see cref="ITargetLogger"/>
        /// </summary>
        /// <param name="something">The action to perform when there is no special logging to be done</param>
        /// <param name="newLoggerAction">The action to be performed when a new logger is to be used</param>
        /// <param name="cacophony">Whether or not cacophany logging is requested</param>
        /// <param name="state">The concurrency state used to synchonize the threads</param>
        /// <param name="index">The index of this thread in the grand scheme of things</param>
        private void ExecuteWithCorrectLogging(
            Action something,
            Action<ITargetLogger> newLoggerAction,
            Boolean cacophony,
            ConcurrencyState state,
            Int32 index)
        {
            if (cacophony)
            {
                something();
            }
            else
            {
                var logger = new BufferingTargetLogger(this.Logger);

                lock (state)
                {
                    state.Loggers[index] = logger;

                    // If this is the first item, or if all other items before this one have completed (rare, but it can happen)
                    // then we want to immediately dismantle this buffer
                    if (index == 0 || state.CompletedList.Take(index).All(z => z))
                    {
                        logger.FlushAndDismantle();
                    }
                }

                try
                {
                    if (newLoggerAction != null)
                    {
                        newLoggerAction(logger);
                    }
                    else
                    {
                        something();
                    }
                }
                finally
                {
                    lock (state)
                    {
                        state.CompletedList[index] = true;
                        if (logger.IsDismantled)
                        {
                            // If our logger has been dismanted, then we are the thread that the others are waiting on to complete
                            // Dismantle the next logger, assuming there is one.  Keep dismantling until we find one that belongs to
                            // A still-running task
                            for (var i = index + 1; i < state.Loggers.Length; i++)
                            {
                                if (state.Loggers[i] == null || state.Loggers[i].IsDismantled)
                                {
                                    break;
                                }

                                state.Loggers[i].FlushAndDismantle();

                                if (!state.CompletedList[i])
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clones the call stack of this <see cref="Task"/>
        /// </summary>
        /// <returns>The cloned call stack</returns>
        private TargetCallStack CloneCallStack()
        {
            return this.CallStack.Clone() as TargetCallStack;
        }

        /// <summary>
        /// Checks if two parallel tasks contain the same inner xml
        /// </summary>
        /// <param name="other">The other task</param>
        /// <returns>true if they are equal</returns>
        public bool Equals(ParallelTask other)
        {
            return
                this.XmlNode.OuterXml.GetHashCode() == other.XmlNode.OuterXml.GetHashCode()
                && this.CallStack.CurrentFrame.Target == other.CallStack.CurrentFrame.Target;
        }

        /// <summary>
        /// Holds information needed for tracking the state of the multiple target threads 
        /// </summary>
        private class ConcurrencyState
        {
            /// <summary>
            /// The <see cref="BufferingTargetLogger"/>s in use.
            /// </summary>
            public BufferingTargetLogger[] Loggers;

            /// <summary>
            /// Each index should correspond exactly with the thread that owns the logger at the same index in <see cref="Loggers"/>.
            /// When the value is true, the thread has finished executing
            /// </summary>
            public Boolean[] CompletedList;
        }
    }
}
