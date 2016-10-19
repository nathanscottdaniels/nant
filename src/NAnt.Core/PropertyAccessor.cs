// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
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
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NAnt.Core
{
    public sealed class PropertyAccessor: IEnumerable
    {
        /// <summary>
        /// The project from which to obtain global properties
        /// </summary>
        private readonly Project project;

        /// <summary>
        /// The call stack from which to obtain thread and target properties
        /// </summary>
        private readonly TargetCallStack callStack;

        /// <summary>
        /// Create a new instance of <see cref="PropertyAccessor"/>
        /// </summary>
        /// <param name="project">The project from which to obtain global properties</param>
        /// <param name="callStack">The call stack from which to obtain thread and target properties</param>
        public PropertyAccessor(Project project, TargetCallStack callStack)
        {
            this.project = project;
            this.callStack = callStack;
        }

        /// <summary>
        /// Gets a property or sets a thread-scoped non-readonly property
        /// </summary>
        /// <param name="name">The property name</param>
        /// <returns>The property value</returns>
        public String this[String name]
        {
            get
            {
                return this.Lookup(name);
            }
            set
            {
                this.Set(name, value);
            }
        }

        /// <summary>
        /// Expands a <see cref="string" /> from known properties.
        /// </summary>
        /// <param name="input">The replacement tokens.</param>
        /// <param name="location">The <see cref="Location" /> to pass through for any exceptions.</param>
        /// <returns>The expanded and replaced string.</returns>
        public string ExpandProperties(string input, Location location)
        {
            Hashtable state = new Hashtable();
            Stack visiting = new Stack();
            return ExpandProperties(input, location, state, visiting);
        }

        /// <summary>
        /// Expands a <see cref="string" /> from known properties.
        /// </summary>
        /// <param name="input">The replacement tokens.</param>
        /// <param name="location">The <see cref="Location" /> to pass through for any exceptions.</param>
        /// <param name="state">A mapping from properties to states. The states in question are "VISITING" and "VISITED". Must not be <see langword="null" />.</param>
        /// <param name="visiting">A stack of properties which are currently being visited. Must not be <see langword="null" />.</param>
        /// <returns>The expanded and replaced string.</returns>
        internal string ExpandProperties(string input, Location location, Hashtable state, Stack visiting)
        {
            return EvaluateEmbeddedExpressions(input, location, state, visiting);
        }

        /// <summary>
        /// Look up a property with the given name
        /// </summary>
        /// <param name="name">The name</param>
        /// <returns>The property value</returns>
        public String Lookup(String name)
        {
            var dict = this.Find(name);
            if (dict == null)
            {
                throw new BuildException(String.Format("property \"{0}\" not found within the current scope.", name));
            }

            return dict[name];
        }

        /// <summary>
        /// DEPRECATED: Adds a new property
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="value">The value</param>
        [Obsolete("Use PropertyAccessor.Set instead.  This method will default to Unchanged scope")]
        public void Add(String name, String value)
        {
            this.Set(name, value);
        }

        /// <summary>
        /// Adds or sets a property value
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <param name="value">The value of the property</param>
        /// <param name="scope">The scope the property should be set in.  Only applicable for new properties.</param>
        /// <param name="dynamic">Whether or not this property is dynamic</param>
        /// <param name="readOnly">Whether or not this property is read only.  Only applicable for new properties.</param>
        public void Set(String name, String value, PropertyScope scope = PropertyScope.Unchanged, Boolean dynamic = false, Boolean readOnly = false)
        {
            var dict = this.Find(name);
            if (dict == null)
            {
                switch (scope)
                {
                    case PropertyScope.Global:
                        dict = this.project.GlobalProperties;
                        break;
                    case PropertyScope.Target:
                        dict = this.callStack.CurrentFrame.TargetProperties;
                        break;
                    case PropertyScope.Thread:
                    case PropertyScope.Unchanged:
                        dict = this.callStack.ThreadProperties;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("scope");
                }

                if (readOnly)
                {
                    dict.AddReadOnly(name, value);
                }
                else
                {
                    dict.Add(name, value);
                }
            }
            else
            {
                if (scope != PropertyScope.Unchanged && dict.Scope != scope)
                {
                    throw new BuildException(
                        String.Format("property {0} cannot be set at the {1} scope because it was found to be set at the {2} scope."
                        + "  For your protection, this is not allowed.  Please note that the default scope for newly-defined properties is \"thread\".  Omit the scope if you are modifying an existing property.",
                        name,
                        Enum.GetName(typeof(PropertyScope), scope),
                        Enum.GetName(typeof(PropertyScope), dict.Scope)));
                }

                dict[name] = value;
            }

            if (dynamic)
            {
                dict.MarkDynamic(name);
            }
        }

        /// <summary>
        /// Determines if a property is dynamic
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <returns><c>true</c> or <c>false</c></returns>
        public bool IsDynamicProperty(string propertyName)
        {
            var dict = this.Find(propertyName);
            if (dict == null)
            {
                throw new BuildException("property \"" + propertyName + "\" not found within the current scope.");
            }

            return dict.IsDynamicProperty(propertyName);
        }

        /// <summary>
        /// Adds global and thread properties from this accessor into the specified project
        /// </summary>
        /// <param name="project">The target project</param>
        /// <param name="excludes">The properties to skip</param>
        public void MergeInto(Project project, StringCollection excludes)
        {
            project.GlobalProperties.Inherit(this.project.GlobalProperties, excludes);
            project.RootTargetCallStack.ThreadProperties.Inherit(this.callStack.ThreadProperties, excludes);
        }

        /// <summary>
        /// Finds the dictionary that has the given property
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <returns>The dictionary or <see langword="null"/> if the property was not found</returns>
        private PropertyDictionary Find(String name)
        {
            var targetProps = this.callStack.CurrentFrame.TargetProperties;
            var threadProps = this.callStack.ThreadProperties;
            var globalProps = this.project.GlobalProperties;

            // First, check target properties
            if (targetProps.Contains(name))
            {
                return targetProps;
            }

            // Next, check the thread properties
            if (threadProps.Contains(name))
            {
                return threadProps;
            }

            // Finally, check the global properties
            if (globalProps.Contains(name))
            {
                return globalProps;
            }

            return null;
        }

        /// <summary>
        /// Evaluates the given expression string and returns the result
        /// </summary>
        /// <param name="input"></param>
        /// <param name="location"></param>
        /// <param name="state"></param>
        /// <param name="visiting"></param>
        /// <returns></returns>
        private string EvaluateEmbeddedExpressions(string input, Location location, Hashtable state, Stack visiting)
        {
            if (input == null)
            {
                return null;
            }

            if (input.IndexOf('$') < 0)
            {
                return input;
            }

            try
            {
                StringBuilder output = new StringBuilder(input.Length);

                ExpressionTokenizer tokenizer = new ExpressionTokenizer();
                ExpressionEvaluator eval = new ExpressionEvaluator(this.project, this, state, visiting, this.callStack);

                tokenizer.IgnoreWhitespace = false;
                tokenizer.SingleCharacterMode = true;
                tokenizer.InitTokenizer(input);

                while (tokenizer.CurrentToken != ExpressionTokenizer.TokenType.EOF)
                {
                    if (tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Dollar)
                    {
                        tokenizer.GetNextToken();
                        if (tokenizer.CurrentToken == ExpressionTokenizer.TokenType.LeftCurlyBrace)
                        {
                            tokenizer.IgnoreWhitespace = true;
                            tokenizer.SingleCharacterMode = false;
                            tokenizer.GetNextToken();

                            string val = Convert.ToString(eval.Evaluate(tokenizer), CultureInfo.InvariantCulture);
                            output.Append(val);
                            tokenizer.IgnoreWhitespace = false;

                            if (tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightCurlyBrace)
                            {
                                throw new ExpressionParseException("'}' expected", tokenizer.CurrentPosition.CharIndex);
                            }
                            tokenizer.SingleCharacterMode = true;
                            tokenizer.GetNextToken();
                        }
                        else {
                            if (tokenizer.CurrentToken != ExpressionTokenizer.TokenType.Dollar)
                                output.Append('$');
                            if (tokenizer.CurrentToken != ExpressionTokenizer.TokenType.EOF)
                            {
                                output.Append(tokenizer.TokenText);
                                tokenizer.GetNextToken();
                            }
                        }
                    }
                    else {
                        output.Append(tokenizer.TokenText);
                        tokenizer.GetNextToken();
                    }
                }
                return output.ToString();
            }
            catch (ExpressionParseException ex)
            {
                StringBuilder errorMessage = new StringBuilder();
                string reformattedInput = input;

                // replace CR, LF and TAB with a space
                reformattedInput = reformattedInput.Replace('\n', ' ');
                reformattedInput = reformattedInput.Replace('\r', ' ');
                reformattedInput = reformattedInput.Replace('\t', ' ');

                errorMessage.Append(ex.Message);
                errorMessage.Append(Environment.NewLine);

                string label = "Expression: ";

                errorMessage.Append(label);
                errorMessage.Append(reformattedInput);

                int p0 = ex.StartPos;
                int p1 = ex.EndPos;

                if (p0 != -1 || p1 != -1)
                {
                    errorMessage.Append(Environment.NewLine);
                    if (p1 == -1)
                        p1 = p0 + 1;

                    for (int i = 0; i < p0 + label.Length; ++i)
                        errorMessage.Append(' ');
                    for (int i = p0; i < p1; ++i)
                        errorMessage.Append('^');
                }

                throw new BuildException(errorMessage.ToString(), location,
                    ex.InnerException);
            }
        }

        /// <summary>
        /// Removes a property from where it is found
        /// </summary>
        /// <param name="property">The property to remove</param>
        internal void Remove(string property)
        {
            var d = this.Find(property);
            if (d != null)
            {
                d.Remove(property);
            }
        }

        /// <summary>
        /// Determines if the property has been declared
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return this.Find(name) != null;
        }

        /// <summary>
        /// Determines if a property is read-only
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <returns><c>true</c> or <c>false</c></returns>
        public bool IsReadOnlyProperty(string propertyName)
        {
            var dict = this.Find(propertyName);
            if (dict == null)
            {
                throw new BuildException("property \"" + propertyName + "\" not found within the current scope.");
            }

            return dict.IsReadOnlyProperty(propertyName);
        }
        
        /// <summary>
        /// Gets an enumerator of all properties that are currently visible in this context
        /// </summary>
        /// <returns>The enumerator</returns>
        [Obsolete("Do not try and enumerate all properties")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            var concatList = new ArrayList();

            foreach (var x in this.project.GlobalProperties)
            {
                concatList.Add(x);
            }
            foreach (var x in this.callStack.ThreadProperties)
            {
                concatList.Add(x);
            }
            foreach (var x in this.callStack.CurrentFrame.TargetProperties)
            {
                concatList.Add(x);
            }

            return concatList.GetEnumerator();
        }
    }
}
