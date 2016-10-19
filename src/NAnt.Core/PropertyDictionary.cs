// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
// Original NAnt Copyright (C) 2003 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Tomas Restrepo (tomasr@mvps.org)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NAnt.Core.Util;

namespace NAnt.Core
{
    /// <summary>
    /// Dictionary to collect a projects properties.
    /// </summary>
    [Serializable()]
    public class PropertyDictionary : DictionaryBase
    {
        /// <summary>
        /// Indexer property. 
        /// </summary>
        public virtual string this[string name]
        {
            get
            {
                string value = (string)Dictionary[(object)name];

                // check whether (built-in) property is deprecated
                CheckDeprecation(name);

                return value;
            }
            set
            {
                Dictionary[name] = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDictionary" />
        /// class holding properties for the given <see cref="Project" /> 
        /// instance.
        /// </summary>
        /// <param name="project">The project for which the dictionary will hold properties.</param>
        /// <param name="scope">The scope of the properties this colleciton contains</param>
        public PropertyDictionary(Project project, PropertyScope scope)
        {
            _project = project;
            this.Scope = scope;
        }

        /// <summary>
        /// Gets the project for which the dictionary holds properties.
        /// </summary>
        /// <value>
        /// The project for which the dictionary holds properties.
        /// </value>
        public Project Project
        {
            get { return _project; }
        }

        /// <summary>
        /// Gets the scope this collection is for
        /// </summary>
        public PropertyScope Scope { get; private set; }

        internal string GetPropertyValue(string propertyName)
        {
            // check whether (built-in) property is deprecated
            CheckDeprecation(propertyName);

            return (string)Dictionary[propertyName];
        }

        protected override void OnClear()
        {
            _readOnlyProperties.Clear();
            _dynamicProperties.Clear();
        }

        protected override void OnSet(object key, object oldValue, object newValue)
        {
            // at this point we're sure the key is valid, as it has already
            // been verified by OnValidate
            string propertyName = (string)key;

            // do not allow value of read-only property to be overwritten
            if (IsReadOnlyProperty(propertyName))
            {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                                       ResourceUtils.GetString("NA1068"), propertyName),
                    Location.UnknownLocation);
            }

            base.OnSet(key, oldValue, newValue);
        }

        /// <summary>
        /// Performs additional custom processes before inserting a new element 
        /// into the <see cref="DictionaryBase" /> instance.
        /// </summary>
        /// <param name="key">The key of the element to insert.</param>
        /// <param name="value">The value of the element to insert.</param>
        protected override void OnInsert(object key, object value)
        {
            // at this point we're sure the key is valid, as it has already
            // been verified by OnValidate
            string propertyName = (string)key;

            // ensure property doesn't already exist
            if (Contains(propertyName))
            {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1065"),
                    propertyName), Location.UnknownLocation);
            }
        }

        /// <summary>
        /// Performs additional custom processes before removing an element
        /// from the <see cref="DictionaryBase" /> instance.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="value">The value of the element to remove.</param>
        protected override void OnRemove(object key, object value)
        {
            string propertyName = key as string;
            if (propertyName != null && _readOnlyProperties.Contains(propertyName))
            {
                _readOnlyProperties.Remove(propertyName);
            }
        }

        /// <summary>
        /// Performs additional custom processes when validating the element 
        /// with the specified key and value.
        /// </summary>
        /// <param name="key">The key of the element to validate.</param>
        /// <param name="value">The value of the element to validate.</param>
        protected override void OnValidate(object key, object value)
        {
            string propertyName = key as string;
            if (propertyName == null)
            {
                throw new ArgumentException("Property name must be a string.", "key");
            }

            ValidatePropertyName(propertyName, Location.UnknownLocation);
            ValidatePropertyValue(propertyName, value, Location.UnknownLocation);
            base.OnValidate(key, value);
        }
        /// <summary>
        /// Adds a property that cannot be changed.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <remarks>
        /// Properties added with this method can never be changed.  Note that
        /// they are removed if the <see cref="DictionaryBase.Clear" /> method is called.
        /// </remarks>
        public virtual void AddReadOnly(string name, string value)
        {
            if (!IsReadOnlyProperty(name))
            {
                Dictionary.Add(name, value);
                _readOnlyProperties.Add(name);
            }
        }

        /// <summary>
        /// Marks a property as a property of which the value is expanded at 
        /// execution time.
        /// </summary>
        /// <param name="name">The name of the property to mark as dynamic.</param>
        public virtual void MarkDynamic(string name)
        {
            if (!IsDynamicProperty(name))
            {
                // check if the property actually exists
                if (!Contains(name))
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1067")));
                }

                _dynamicProperties.Add(name);
            }
        }

        /// <summary>
        /// Adds a property to the collection.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to assign to the property.</param>
        public virtual void Add(string name, string value)
        {
            Dictionary.Add(name, value);
        }

        /// <summary>
        /// Determines whether the specified property is listed as read-only.
        /// </summary>
        /// <param name="name">The name of the property to check.</param>
        /// <returns>
        /// <see langword="true" /> if the property is listed as read-only; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        public virtual bool IsReadOnlyProperty(string name)
        {
            return _readOnlyProperties.Contains(name);
        }

        /// <summary>
        /// Determines whether the specified property is listed as dynamic.
        /// </summary>
        /// <param name="name">The name of the property to check.</param>
        /// <returns>
        /// <see langword="true" /> if the property is listed as dynamic; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        public virtual bool IsDynamicProperty(string name)
        {
            return _dynamicProperties.Contains(name);
        }

        /// <summary>
        /// Determines whether a property already exists.
        /// </summary>
        /// <param name="name">The name of the property to check.</param>
        /// <returns>
        /// <see langword="true" /> if the specified property already exists; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(string name)
        {
            return Dictionary.Contains(name);
        }

        /// <summary>
        /// Removes the property with the specified name.
        /// </summary>
        /// <param name="name">The name of the property to remove.</param>
        public void Remove(string name)
        {
            Dictionary.Remove(name);
        }

        private static void ValidatePropertyName(string propertyName, Location location)
        {
            const string propertyNamePattern = "^[_A-Za-z0-9][_A-Za-z0-9\\-.]*$";

            // validate property name
            //
            if (!Regex.IsMatch(propertyName, propertyNamePattern))
            {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1064"), propertyName), location);
            }
            if (propertyName.EndsWith("-") || propertyName.EndsWith("."))
            {
                // this additional rule helps simplify the regex pattern
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1064"), propertyName), location);
            }
        }

        private static void ValidatePropertyValue(string name, object value, Location loc)
        {
            CultureInfo ci = CultureInfo.InvariantCulture;

            try
            {
                if (value == null)
                {
                    throw new ArgumentException(String.Format(ci,
                        ResourceUtils.GetString("NA1194"), name));
                }

                if (!(value is string))
                {
                    throw new ArgumentException(String.Format(ci,
                        ResourceUtils.GetString("NA1066"), value.GetType()),
                        "value");
                }
            }
            catch (Exception x)
            {
                throw new BuildException("Property value validation failed: ", loc, x);
            }
        }

        /// <summary>
        /// Inherits properties from an existing property dictionary Instance.
        /// </summary>
        /// <param name="source">Property list to inherit.</param>
        /// <param name="excludes">The list of properties to exclude during inheritance.</param>
        public virtual void Inherit(PropertyDictionary source, StringCollection excludes)
        {
            foreach (DictionaryEntry entry in source.Dictionary)
            {
                string propertyName = (string)entry.Key;

                if (excludes != null && excludes.Contains(propertyName))
                {
                    continue;
                }

                // do not overwrite an existing read-only property
                if (IsReadOnlyProperty(propertyName))
                {
                    continue;
                }

                // add property to dictionary
                ValidatePropertyName(propertyName, Location.UnknownLocation);
                Dictionary[propertyName] = entry.Value;

                // if property is readonly, add to collection of readonly properties
                if (source.IsReadOnlyProperty(propertyName))
                {
                    _readOnlyProperties.Add(propertyName);
                }

                // if property is dynamic, add to collection of dynamic properties
                // if it was not already in that collection
                if (source.IsDynamicProperty(propertyName) && !IsDynamicProperty(propertyName))
                {
                    _dynamicProperties.Add(propertyName);
                }
            }
        }

        /// <summary>
        /// Checks whether the specified property is deprecated.
        /// </summary>
        /// <param name="name">The property to check.</param>
        private void CheckDeprecation(string name)
        {
            switch (name)
            {
                case Project.NAntPropertyFileName:
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use assembly::get-location(nant::get-assembly()) expression instead.", name);
                    break;
                case Project.NAntPropertyVersion:
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the assemblyname::get-version(assembly::get-name(nant::get-assembly))"
                        + " expression instead.", name);
                    break;
                case Project.NAntPropertyLocation:
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the nant::get-base-directory() function instead.",
                        name);
                    break;
                case Project.NAntPropertyProjectBaseDir:
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the project::get-base-directory() function instead.",
                        name);
                    break;
                case Project.NAntPropertyProjectName:
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the project::get-name() function instead.",
                        name);
                    break;
                case Project.NAntPropertyProjectBuildFile:
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the project::get-buildfile-uri() function"
                        + " instead.", name);
                    break;
                case Project.NAntPropertyProjectDefault:
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the project::get-default-target() function"
                        + " instead.", name);
                    break;
                case Project.NAntPlatformName:
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the platform::get-name() function instead.",
                        name);
                    break;
                case Project.NAntPlatform + ".win32":
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the platform::is-win32() function instead.",
                        name);
                    break;
                case Project.NAntPlatform + ".unix":
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the platform::is-unix() function instead.",
                        name);
                    break;
                case "nant.settings.currentframework.description":
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the framework::get-description(framework::get-target-framework())"
                        + " function instead.", name);
                    break;
                case "nant.settings.currentframework.frameworkdirectory":
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the framework::get-framework-directory(framework::get-target-framework())"
                        + " function instead.", name);
                    break;
                case "nant.settings.currentframework.sdkdirectory":
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the framework::get-sdk-directory(framework::get-target-framework())"
                        + " function instead.", name);
                    break;
                case "nant.settings.currentframework.frameworkassemblydirectory":
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the framework::get-assembly-directory(framework::get-target-framework())"
                        + " function instead.", name);
                    break;
                case "nant.settings.currentframework.runtimeengine":
                    this.Project.Log(Level.Warning, "Built-in property '{0}' is deprecated."
                        + " Use the framework::get-runtime-engine(framework::get-target-framework())"
                        + " function instead.", name);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Builds an appropriate exception detailing a specified circular
        /// reference.
        /// </summary>
        /// <param name="end">The property reference to stop at. Must not be <see langword="null" />.</param>
        /// <param name="stack">A stack of property references. Must not be <see langword="null" />.</param>
        /// <returns>
        /// A <see cref="BuildException" /> detailing the specified circular 
        /// dependency.
        /// </returns>
        internal static BuildException CreateCircularException(string end, Stack stack)
        {
            StringBuilder sb = new StringBuilder("Circular property reference: ");
            sb.Append(end);

            string c;

            do
            {
                c = (string)stack.Pop();
                sb.Append(" <- ");
                sb.Append(c);
            } while (!c.Equals(end));

            return new BuildException(sb.ToString());
        }
        /// <summary>
        /// Maintains a list of the property names that are readonly.
        /// </summary>
        private StringCollection _readOnlyProperties = new StringCollection();

        /// <summary>
        /// Maintains a list of the property names of which the value is expanded
        /// on usage, not at initalization.
        /// </summary>
        private StringCollection _dynamicProperties = new StringCollection();

        /// <summary>
        /// The project for which the dictionary holds properties.
        /// </summary>
        private readonly Project _project;
        /// <summary>
        /// Constant for the "visiting" state, used when traversing a DFS of 
        /// property references.
        /// </summary>
        internal const string Visiting = "VISITING";

        /// <summary>
        /// Constant for the "visited" state, used when travesing a DFS of 
        /// property references.
        /// </summary>
        internal const string Visited = "VISITED";
    }
}
