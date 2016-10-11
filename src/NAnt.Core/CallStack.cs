using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAnt.Core
{
    public class CallStack : ICloneable
    {
        private Stack<StackFrame> InnerStack { get; set; } = new Stack<StackFrame>();

        public StackFrame CurrentFrame
        {
            get
            {
                return this.InnerStack.Peek();
            }
        }

        public IEnumerable<StackFrame> Traverser
        {
            get
            {
                return this.InnerStack;
            }
        }

        internal void Push(StackFrame stackFrame)
        {
            this.InnerStack.Push(stackFrame);
        }

        internal StackFrame Pop()
        {
            return this.InnerStack.Pop();
        }

        public object Clone()
        {
            return new CallStack()
            {
                InnerStack = new Stack<StackFrame>(new Stack<StackFrame>(this.InnerStack))
            };
        }
    }
}
