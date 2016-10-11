using NAnt.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAnt.Core.Types
{
    [Serializable]
    [ElementName("pCall")]
    public class ParallelTarget : DataTypeBase
    {
        /// <summary>
        /// Value of the option. The default is <see langword="null" />.
        /// </summary>
        [TaskAttribute("target")]
        public string TargetName { get; set; }

        /// <summary>
        /// Indicates if the option should be passed to the task. 
        /// If <see langword="true" /> then the option will be passed; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined { get; set; } = true;

        /// <summary>
        /// Indicates if the option should not be passed to the task.
        /// If <see langword="false" /> then the option will be passed; 
        /// otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined { get; set; } = false;
    }
}
