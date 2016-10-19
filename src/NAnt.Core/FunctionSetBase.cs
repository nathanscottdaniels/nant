// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
// Original NAnt Copyright (C) 2001-2003 Gerry Shaw
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
//
// Ian MacLean (imaclean@gmail.com)

using System;
using System.Collections.Specialized;

namespace NAnt.Core {
    /// <summary>
    /// Base class for implementing NAnt functions.
    /// </summary>
    public abstract class FunctionSetBase {

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSetBase"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        /// <param name="callStack">The target call stack</param>
        protected FunctionSetBase(Project project, PropertyAccessor properties, TargetCallStack callStack)
        {
            _project = project;
            this.PropertyAccesor = properties;
            this.CallStack = callStack;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSetBase"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        [Obsolete("This constructor was kept only for backwards compatability with old function sets.  Use the other constructor instead.")]
        protected FunctionSetBase(Project project, PropertyDictionary properties)
        {
            var hack = properties as BackwardsCompatiblePropertyAccessor;
            if (hack == null)
            {
                throw new InvalidOperationException("This constructor should never be called from outside a subclass's constructor.");
            }

            _project = project;
            this.PropertyAccesor = hack.PropertyAccessor;
            this.CallStack = hack.TargetCallStack;
        }


        /// <summary>
        /// Gets or sets the <see cref="Project" /> that this functionset will 
        /// reference.
        /// </summary>
        /// <value>
        /// The <see cref="Project" /> that this functionset will reference.
        /// </value>
        public virtual Project Project {
            get { return _project; }
            set { _project = value; }
        }

        /// <summary>
        /// Gets the <see cref="PropertyAccesor"/> used to get and set property values
        /// </summary>
        protected PropertyAccessor PropertyAccesor { get; private set; }

        /// <summary>
        /// Gets the call stack for this function set
        /// </summary>
        protected TargetCallStack CallStack { get; private set; }

        /// <summary>
        /// The project.
        /// </summary>
        private Project _project;
    }

    /// <summary>
    /// A hack to wrap the new constructor parameters of <see cref="FunctionSetBase"/> in a class that was being passed in the old constructor
    /// </summary>
    [Obsolete("Don't use this.  It exists only to allow old function sets to still compile")]
    internal class BackwardsCompatiblePropertyAccessor : PropertyDictionary
    {
        public PropertyAccessor PropertyAccessor { get; private set; }

        public TargetCallStack TargetCallStack { get; private set; }

        public BackwardsCompatiblePropertyAccessor(Project project, PropertyAccessor accessor, TargetCallStack callstack)
            : base(project, PropertyScope.Global)
        {
            this.PropertyAccessor = accessor;
            this.TargetCallStack = callstack;
        }


        private static Exception Deprecated = new NotSupportedException("The direct use of the PropertyDictionary class has been removed.  Please use the PropertyAccessor class available on FunctionSetBase");

        public override void Add(string name, string value)
        {
            throw Deprecated;
        }

        public override void AddReadOnly(string name, string value)
        {
            throw Deprecated;
        }

        public override void Inherit(PropertyDictionary source, StringCollection excludes)
        {
            throw Deprecated;
        }

        public override bool IsDynamicProperty(string name)
        {
            throw Deprecated;
        }

        public override bool IsReadOnlyProperty(string name)
        {
            throw Deprecated;
        }

        public override void MarkDynamic(string name)
        {
            throw Deprecated;
        }

        public override string this[string name]
        {
            get
            {
                throw Deprecated;
            }

            set
            {
                throw Deprecated;
            }
        }
    }
}
