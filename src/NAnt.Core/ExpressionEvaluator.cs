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
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Collections;
using System.Reflection;
using System.Globalization;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core {
    [FunctionSet("property", "NAnt")]
    public class ExpressionEvaluator : ExpressionEvalBase {
        private PropertyAccessor _properties;
        private Hashtable _state;
        private Stack _visiting;

        /// <summary>
        /// The call stack needed for executing certain functions along the way
        /// </summary>
        private readonly TargetCallStack callStack;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionEvaluator"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="properties">The projects properties.</param>
        /// <param name="state">The state.</param>
        /// <param name="visiting">The visiting.</param>
        /// <param name="targetCallStack">The call stack needed for executing certain functions along the way</param>
        public ExpressionEvaluator(Project project, PropertyAccessor properties, Hashtable state, Stack visiting, TargetCallStack targetCallStack)
            : base(project) {
            _properties = properties;
            _state = state;
            _visiting = visiting;
            this.callStack = targetCallStack;
        }

        protected override object EvaluateProperty(string propertyName) {
            return GetPropertyValue(propertyName);
        }

        protected override object EvaluateFunction(MethodInfo methodInfo, object[] args) {
            try {
                if (methodInfo.IsStatic) {
                    return methodInfo.Invoke(null, args);
                } else if (methodInfo.DeclaringType.IsAssignableFrom(typeof(ExpressionEvaluator))) {
                    return methodInfo.Invoke(this, args);
                } else {
                    // create new instance.
                    ConstructorInfo constructor = methodInfo.DeclaringType.GetConstructor(new Type[] {typeof(Project), typeof(PropertyAccessor), typeof(TargetCallStack)});

                    if (constructor != null)
                    {
                        var o = constructor.Invoke(new object[] { Project, this._properties, this.callStack });

                        return methodInfo.Invoke(o, args);
                    }
                    else
                    {
                        // Old function set sub classes could be using the old constructor so lets hack our way through
                        constructor = methodInfo.DeclaringType.GetConstructor(new Type[] { typeof(Project), typeof(PropertyDictionary)});

                        if (constructor == null)
                        {
                            throw new TypeLoadException("A proper constuctor was not found for the class containing function " + methodInfo.Name + ".  Make sure your class has only the constructors necessary for superclass FunctionSetBase.");
                        }

#pragma warning disable CS0618 // We know this is obsolete.  It is obsolete so no one ELSE uses it
                        var o = constructor.Invoke(new object[] { Project, new BackwardsCompatiblePropertyAccessor(this.Project, this._properties, this.callStack) });
#pragma warning restore CS0618 // Type or member is obsolete

                        return methodInfo.Invoke(o, args);
                    }
                }
            } catch (TargetInvocationException ex) {
                if (ex.InnerException != null) {
                    // throw actual exception
                    throw ex.InnerException;
                }
                // re-throw exception
                throw;
            }
        }
        /// <summary>
        /// Gets the value of the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property to get the value of.</param>
        /// <returns>
        /// The value of the specified property.
        /// </returns>
        [Function("get-value")]
        public string GetPropertyValue(string propertyName) {
            if (_properties.IsDynamicProperty(propertyName)) {
                string currentState = (string)_state[propertyName];

                // check for circular references
                if (currentState == PropertyDictionary.Visiting) {
                    // Currently visiting this node, so have a cycle
                    throw PropertyDictionary.CreateCircularException(propertyName, _visiting);
                }

                _visiting.Push(propertyName);
                _state[propertyName] = PropertyDictionary.Visiting;

                string propertyValue = _properties.Lookup(propertyName);
                if (propertyValue == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1053"), propertyName));
                }

                Location propertyLocation = Location.UnknownLocation;

                // TODO - get the proper location of the property declaration
                
                propertyValue = _properties.ExpandProperties(propertyValue, 
                    propertyLocation, _state, _visiting);

                _visiting.Pop();
                _state[propertyName] = PropertyDictionary.Visited;
                return propertyValue;
            } else {
                string propertyValue = _properties.Lookup(propertyName);
                if (propertyValue == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1053"), propertyName));
                }

                return propertyValue;
            }
        }
    }
}
