using NAnt.Core.Attributes;
using System;
using System.Threading;

namespace NAnt.Core.Tasks
{
    [TaskName("mutex")]
    public class MutexTask : TaskContainer
    {
        /// <summary>
        /// The name of the mutext to wait for
        /// </summary>
        [TaskAttribute("name", ExpandProperties = true, Required = true)]
        [StringValidator(AllowEmpty = false)]
        public String Name { get; set; }

        /// <summary>
        /// The amount of time to wait for the mutext to become available before failing the build
        /// </summary>
        [TaskAttribute("timeout")]
        [Int32Validator(MinValue = 1)]
        public Int32 Timeout { get; set; } = -1;

        /// <summary>
        /// The mutex to use
        /// </summary>
        private Mutex chosenMutex;

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask()
        {
            for (var ancestor = this.Caller; ancestor != null; ancestor = ancestor.Caller)
            {
                var task = ancestor as MutexTask;
                if (task != null)
                {
                    if (task.Name.Equals(this.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        throw new BuildException($"Deadlock detected!  A target is currently waiting for mutex \"{this.Name}\" which is held by a parent task.");
                    }
                }
            }

            if (this.Timeout != -1)
            {
                this.chosenMutex.WaitOne(new TimeSpan(0, 0, this.Timeout));
            }
            else
            {
                this.chosenMutex.WaitOne();
            }

            base.ExecuteTask();
            this.chosenMutex.ReleaseMutex();
        }

        /// <summary>
        /// Initializes this task
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.chosenMutex = new Mutex(false, this.Name);
        }
    }
}
