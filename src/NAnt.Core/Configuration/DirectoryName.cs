using System;
using NAnt.Core.Attributes;

namespace NAnt.Core.Configuration {
    [Serializable]
    internal class DirectoryName : Element, IConditional {
        private string _name;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        [TaskAttribute("name", Required = true)]
        public string DirName
        {
            get { return _name; }
            set { _name = value; }
        }

        #endregion

        [TaskAttribute("if")]
        [BooleanValidator()]
        protected bool IfDefined
        {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        [TaskAttribute("unless")]
        [BooleanValidator()]
        protected bool UnlessDefined
        {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }


        bool IConditional.IfDefined
        {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        bool IConditional.UnlessDefined
        {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion
    }
}
