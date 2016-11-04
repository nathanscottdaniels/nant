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
using System.Diagnostics;

namespace NAnt.Core
{
    /// <summary>
    /// Defines an interface that Targets will use in order to log messagaes to the console/file/whatever
    /// </summary>
    public interface ITargetLogger
    {
        /// <summary>
        /// Writes a <see cref="Project" /> level message to the build log with
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        void Log(Level messageLevel, string message);

        /// <summary>
        /// Writes a <see cref="Project" /> level formatted message to the build 
        /// log with the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        void Log(Level messageLevel, string message, params object[] args);

        /// <summary>
        /// Writes a <see cref="Task" /> task level message to the build log 
        /// with the given <see cref="Level" />.
        /// </summary>
        /// <param name="task">The <see cref="Task" /> from which the message originated.</param>
        /// <param name="stopwatch">The stopwatch of the task.</param>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        void Log(Task task, Stopwatch stopwatch, Level messageLevel, string message);

        /// <summary>
        /// Writes a <see cref="Target" /> level message to the build log with 
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="target">The <see cref="Target" /> from which the message orignated.</param>
        /// <param name="stopwatch">The stopwatch of the target.</param>
        /// <param name="messageLevel">The level to log at.</param>
        /// <param name="message">The message to log.</param>
        void Log(Target target, Stopwatch stopwatch, Level messageLevel, string message);

        /// <summary>
        /// Dispatches a <see cref="TaskStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TaskBuildEventArgs" /> that contains the event data.</param>
        void OnTaskLoggingStarted(object sender, TaskBuildEventArgs e);

        /// <summary>
        /// Dispatches the <see cref="TaskFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TaskBuildEventArgs" /> that contains the event data.</param>
        void OnTaskLoggingFinished(object sender, TaskBuildEventArgs e);

        /// <summary>
        /// Dispatches a <see cref="TargetFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TargetBuildEventArgs" /> that contains the event data.</param>
        void OnTargetLoggingFinished(object sender, TargetBuildEventArgs e);

        /// <summary>
        /// Dispatches a <see cref="TargetStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TargetBuildEventArgs" /> that contains the event data.</param>
        void OnTargetLoggingStarted(object sender, TargetBuildEventArgs e);

        /// <summary>
        /// Signals that the last target has finished and logging for the build is complete.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        void OnBuildLoggingFinished(object sender, BuildEventArgs e);

        /// <summary>
        /// Signals that logging for a build has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event is fired before any targets have started.
        /// </remarks>
        void OnBuildLoggingStarted(object sender, BuildEventArgs e);

        /// <summary>
        /// Increases the indentation level of the log
        /// </summary>
        void Indent();

        /// <summary>
        /// Decreases 
        /// </summary>
        void Unindent();
    }
}
