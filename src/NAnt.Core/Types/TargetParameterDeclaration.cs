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

namespace NAnt.Core.Types
{
    /// <summary>
    /// A single parameter to be required by a target
    /// </summary>
    [Serializable]
    [ElementName("parameter")]
    public class TargetParameterDeclaration : DataTypeBase
    {
        /// <summary>
        /// Gets or sets the name of the property to set
        /// </summary>
        [TaskAttribute("name", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the value of the property to set
        /// </summary>
        [TaskAttribute("default", Required = false)]
        [StringValidator()]
        public string DefaultValue { get; set; }
    }
}
