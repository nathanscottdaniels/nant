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
namespace NAnt.Core
{
    /// <summary>
    /// Defines the scope of a property
    /// </summary>
    public enum PropertyScope
    {
        Unchanged,

        /// <summary>
        /// Thread scope.  This is the default scope.  Properties in this scope are 
        /// visible to all future target, but changes to properties in this scope 
        /// only affect the current thread.
        /// When a new thread is spawned, The property values are cloned so that 
        /// downstream threads can see and change the value the property had in the
        /// original thread but changes would only be visible in the current thread.
        /// </summary>
        Thread,

        /// <summary>
        /// Global scope.  Equivalent to static variables, properties in this scope
        /// are visible and mutable to all targets, regardless of thread.
        /// </summary>
        Global,

        /// <summary>
        /// Target scope.  This is equivalent to local variables.  Properties in this 
        /// scope are only visible to tasks within the current target.
        /// </summary>
        Target
    }
}
