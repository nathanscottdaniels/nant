using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAnt.Core
{
    public class StackFrame
    {
        public StackFrame(Target target, Task caller = null)
        {
            this.Target = target;
            this.Caller = caller;
        }

        public Target Target { get; }

        public Task Caller { get; }
    }
}
