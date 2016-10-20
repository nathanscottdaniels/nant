// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
// Original NAnt Copyright (C) 2001-2002 Gerry Shaw
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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (imaclean@gmail.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core
{
    /// <summary>
    /// Class for handling NAnt targets.
    /// </summary>
    [Serializable()]
    public sealed class Target : Element, ICloneable
    {
        private string _name;
        private string _description;
        private string _ifCondition;
        private string _unlessCondition;
        private StringCollection _dependencies = new StringCollection();
        private bool _executed;
        /// <summary>
        /// Initializes a new instance of the <see cref="Target" /> class.
        /// </summary>
        public Target()
        {
        }
        /// <summary>
        /// This indicates whether the target has already executed.
        /// </summary>
        public bool Executed
        {
            get
            {
                return _executed;
            }
        }

        /// <summary>
        /// The name of the target.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   Hides <see cref="Element.Name"/> to have <see cref="Target" /> 
        ///   return the name of target, not the name of XML element - which 
        ///   would always be <c>target</c>.
        ///   </para>
        ///   <para>
        ///   Note: Properties are not allowed in the name.
        ///   </para>
        /// </remarks>
        [TaskAttribute("name", Required = true, ExpandProperties = false)]
        [StringValidator(AllowEmpty = false)]
        public new string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Whether or not this target is locked to a single thread of execution at a time.
        /// </summary>
        [TaskAttribute("locked", Required = false, ExpandProperties = true)]
        [BooleanValidator()]
        public Boolean Locked { get; set; }

        /// <summary>
        /// If <see langword="true" /> then the target will be executed; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if", ExpandProperties = false)]
        public string IfCondition
        {
            get { return _ifCondition; }
            set { _ifCondition = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Gets or sets the parameters for this target
        /// </summary>
        [BuildElement("parameters", Required = false)]
        public TargetParameterDeclarationCollection Parameters { get; set; }

        /// <summary>
        /// Gets a value indicating whether the target should be executed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the target should be executed; otherwise, 
        /// <see langword="false" />.
        /// </value>
        public bool IfDefined(PropertyAccessor propertyAccesor)
        {
            // expand properties in condition
            string expandedCondition = propertyAccesor.ExpandProperties(IfCondition, Location);

            // if a condition is supplied, it should evaluate to a bool
            if (!String.IsNullOrEmpty(expandedCondition))
            {
                try
                {
                    return Convert.ToBoolean(expandedCondition, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1070"), expandedCondition), Location);
                }
            }

            // no condition is supplied
            return true;
        }

        /// <summary>
        /// Opposite of <see cref="IfDefined" />. If <see langword="false" /> 
        /// then the target will be executed; otherwise, skipped. The default 
        /// is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless", ExpandProperties = false)]
        public string UnlessCondition
        {
            get { return _unlessCondition; }
            set { _unlessCondition = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Gets a value indicating whether the target should NOT be executed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the target should NOT be executed;
        /// otherwise, <see langword="false" />.
        /// </value>
        public bool UnlessDefined(PropertyAccessor propertyAccesor)
        {
            // expand properties in condition
            string expandedCondition = propertyAccesor.ExpandProperties(UnlessCondition, Location);

            // if a condition is supplied, it should evaluate to a bool
            if (!String.IsNullOrEmpty(expandedCondition))
            {
                try
                {
                    return Convert.ToBoolean(expandedCondition, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1069"), expandedCondition),
                        Location);
                }
            }

            // no condition is supplied
            return false;
        }

        /// <summary>
        /// The description of the target.
        /// </summary>
        [TaskAttribute("description")]
        public string Description
        {
            set { _description = value; }
            get { return _description; }
        }

        /// <summary>
        /// Space separated list of targets that this target depends on.
        /// </summary>
        [TaskAttribute("depends")]
        public string DependsListString
        {
            set
            {
                foreach (string str in value.Split(new char[] { ' ', ',' }))
                {
                    string dependency = str.Trim();
                    if (dependency.Length > 0)
                    {
                        Dependencies.Add(dependency);
                    }
                }
            }
        }

        /// <summary>
        /// A collection of target names that must be executed before this 
        /// target.
        /// </summary>
        public StringCollection Dependencies
        {
            get { return _dependencies; }
        }
        /// <summary>
        /// Creates a shallow copy of the <see cref="Target" />.
        /// </summary>
        /// <returns>
        /// A shallow copy of the <see cref="Target" />.
        /// </returns>
        object ICloneable.Clone()
        {
            return Clone();
        }
        /// <summary>
        /// Creates a shallow copy of the <see cref="Target" />.
        /// </summary>
        /// <returns>
        /// A shallow copy of the <see cref="Target" />.
        /// </returns>
        public Target Clone()
        {
            Target clone = new Target();
            base.CopyTo(clone);
            clone._dependencies = _dependencies;
            clone._description = _description;
            clone._executed = _executed;
            clone._ifCondition = _ifCondition;
            clone._name = _name;
            clone._unlessCondition = _unlessCondition;
            clone.Parameters = this.Parameters;
            return clone;
        }

        /// <summary>
        /// Executes this target, after acquiring any necessary locks.
        /// </summary>
        /// <param name="callStack">The current call stack on which this target will be pused</param>
        /// <param name="logger">The logger this target and its stasks will use for logging messages</param>
        /// <param name="arguments">Optionally, the arguments to provide to the target.  Should match those required by <see cref="Parameters"/></param>
        /// <exception cref="ArgumentException">If one of the non-defaulted parameters is not satisfied by an argument.</exception>
        public void Execute(TargetCallStack callStack, ITargetLogger logger, IList<CallArgument> arguments = null)
        {
            if (this.Locked)
            {
                lock (this)
                {
                    this.DoExecute(callStack, logger, arguments);
                }
            }
            else
            {
                this.DoExecute(callStack, logger, arguments);
            }
        }

        /// <summary>
        /// Executes this target
        /// </summary>
        /// <param name="callStack">The current call stack on which this target will be pused</param>
        /// <param name="logger">The logger this target and its stasks will use for logging messages</param>
        /// <param name="arguments">Optionally, the arguments to provide to the target.  Should match those required by <see cref="Parameters"/></param>
        /// <exception cref="ArgumentException">If one of the non-defaulted parameters is not satisfied by an argument.</exception>
        private void DoExecute(TargetCallStack callStack, ITargetLogger logger, IList<CallArgument> arguments = null)
        {
            var propertyAccessor = new PropertyAccessor(this.Project, callStack);

            var sw = Stopwatch.StartNew();

            if (IfDefined(propertyAccessor) && !UnlessDefined(propertyAccessor))
            {
                try
                {
                    using (callStack.Push(this))
                    {
                        this.PrepareArguments(arguments, callStack);

                        Project.OnTargetStarted(this, new TargetBuildEventArgs(this, sw));
                        logger.OnTargetLoggingStarted(this, new TargetBuildEventArgs(this, sw));

                        var paramtersDone = false;

                        // select all the task nodes and execute them
                        foreach (XmlNode childNode in XmlNode)
                        {
                            if (!(childNode.NodeType == XmlNodeType.Element) || !childNode.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant")))
                            {
                                continue;
                            }

                            if (childNode.Name.Equals("parameters"))
                            {
                                if (paramtersDone)
                                {
                                    throw new BuildException("parameters must appear before all tasks", this.Location);
                                }

                                continue;
                            }
                            else
                            {
                                paramtersDone = true;
                                if (TypeFactory.TaskBuilders.Contains(childNode.Name))
                                {
                                    Task task = Project.CreateTask(childNode, this, callStack);
                                    if (task != null)
                                    {
                                        task.Logger = logger;
                                        task.Execute();
                                    }
                                }
                                else if (TypeFactory.DataTypeBuilders.Contains(childNode.Name))
                                {
                                    DataTypeBase dataType = Project.CreateDataTypeBase(childNode, callStack);
                                    logger.Log(Level.Verbose, "Adding a {0} reference with id '{1}'.",
                                        childNode.Name, dataType.ID);
                                    if (!Project.DataTypeReferences.Contains(dataType.ID))
                                    {
                                        Project.DataTypeReferences.Add(dataType.ID, dataType);
                                    }
                                    else {
                                        Project.DataTypeReferences[dataType.ID] = dataType; // overwrite with the new reference.
                                    }
                                }
                                else
                                {
                                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                        ResourceUtils.GetString("NA1071"),
                                        childNode.Name), Project.LocationMap.GetLocation(childNode));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    _executed = true;
                    sw.Stop();
                    Project.OnTargetFinished(this, new TargetBuildEventArgs(this, sw));
                    logger.OnTargetLoggingFinished(this, new TargetBuildEventArgs(this, sw));
                }
            }
        }

        /// <summary>
        /// Extracts up the paramters specified in <see cref="Parameters"/> from <paramref name="arguments"/> 
        /// and ddds the necessary properties to the call stack's target properties.
        /// </summary>
        /// <param name="arguments">The arguments being passed in to the this target</param>
        /// <param name="callStack">The call stack where the properties will be placed</param>
        /// <exception cref="ArgumentException">If one of the non-defaulted parameters is not satisfied by an argument.</exception>
        private void PrepareArguments(IList<CallArgument> arguments, TargetCallStack callStack)
        {
            if (this.Parameters == null || !this.Parameters.Parameters.Any())
            {
                return;
            }

            var accessor = new PropertyAccessor(this.Project, callStack);

            var matchedProperties = new HashSet<String>();

            foreach (var param in this.Parameters.Parameters)
            {
                if (matchedProperties.Contains(param.PropertyName))
                {
                    throw new BuildException(String.Format(@"Paramter ""{0}"" was declared more than once", param.PropertyName));
                }

                matchedProperties.Add(param.PropertyName);

                CallArgument arg = null;

                if (arguments != null)
                {
                    arg = arguments.FirstOrDefault(a => a.PropertyName.Equals(param.PropertyName));
                }

                if (arg == null && param.DefaultValue != null)
                {
                    accessor.Set(param.PropertyName, param.DefaultValue, PropertyScope.Target, false, true);
                }
                else if (arg != null)
                {
                    if (arguments.Count(a => a.PropertyName.Equals(arg.PropertyName)) > 1)
                    {
                        throw new ArgumentException(String.Format(@"Argument ""{0}"" was specified more than once.", param.PropertyName));
                    }

                    accessor.Set(param.PropertyName, arg.PropertyValue, PropertyScope.Target, false, true);
                }
                else
                {
                    throw new ArgumentException(String.Format(@"Target ""{0}"" requires parameter ""{1}"" but it was not provided and has no default.", this.Name, param.PropertyName));
                }
            }

        }
    }
}
