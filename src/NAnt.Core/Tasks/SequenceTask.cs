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
