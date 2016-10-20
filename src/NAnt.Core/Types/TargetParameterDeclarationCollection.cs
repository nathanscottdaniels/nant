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
using NAnt.Core.Attributes;
using System;
using System.Collections.Generic;

namespace NAnt.Core.Types
{
    /// <summary>
    /// A collection of parameters to be required by a target
    /// </summary>
    [Serializable]
    [ElementName("parameters")]
    public class TargetParameterDeclarationCollection : Element
    {
        /// <summary>
        /// Creates a new instance of <see cref="TargetParameterDeclarationCollection"/>
        /// </summary>
        public TargetParameterDeclarationCollection()
        {
            this.Parameters = new List<TargetParameterDeclaration>();
        }

        /// <summary>
        /// Gets the required parameters
        /// </summary>
        public IList<TargetParameterDeclaration> Parameters { get; private set; }

        /// <summary>
        /// Adds a new parameter
        /// </summary>
        /// <param name="parameter">The parameter</param>
        [BuildElement("parameter", Required = true)]
        public void AddParameter(TargetParameterDeclaration parameter)
        {
            this.Parameters.Add(parameter);
        }
    }
}
