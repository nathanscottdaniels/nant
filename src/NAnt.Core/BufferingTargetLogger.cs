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
using System.Collections.Concurrent;
using System.Threading;

namespace NAnt.Core
{
    /// <summary>
    /// A <see cref="ITargetLogger"/> that buffers all log messages until flushed, thus preventing any logging from
    /// occuring until a desired time at which time it happens in bulk.
    /// </summary>
    /// <remarks>
    /// Used by the parallel task to only show logging for one target at a time, if desired
    /// </remarks>
    internal class BufferingTargetLogger : ITargetLogger, IDisposable
    {
        /// <summary>
        /// Provides storage for lock objects specific to each target logger.
        /// </summary>
        private static ConcurrentDictionary<ITargetLogger, Object> LoggerLocks = new ConcurrentDictionary<ITargetLogger, object>();

        /// <summary>
        /// Whether or the not the buffer has been disabled
        /// </summary>
        private volatile Boolean disabled = false;

        /// <summary>
        /// A lock that all log writing methods acquire a read-lock on and the dismantle method acquired the write-lock.
        /// This lets unlimited log writers add to the queue UNLESS the dismantle method is running.  This is because the
        /// dismantle method has the goal of completely draining the queue for the final time, and any log writer adding 
        /// to that queue could be bad.  Once the dismantle method is done draining the queue, it flips the <see cref="disabled"/>
        /// flag and releases the lock.  Once the read-lock is obtainable by the log writers, they will see the <see cref="disabled"/>
        /// flag is up and bypass the queue from then on
        /// </summary>
        private ReaderWriterLock disablingLock = new ReaderWriterLock();

        /// <summary>
        /// Creates a new <see cref="BufferingTargetLogger"/>
        /// </summary>
        /// <param name="destination">The destination logger that will be used when this logger is flushed</param>
        public BufferingTargetLogger(ITargetLogger destination)
        {
            this.DestinationLogger = destination;
            LoggerLocks.AddOrUpdate(destination, new object(), (key, old) => old);
        }

        /// <summary>
        /// Gets whether or not <see cref="FlushAndDismantle"/> has been called on this instance
        /// </summary>
        public Boolean IsDismantled
        {
            get
            {
                return this.disabled;
            }
        }

        /// <summary>
        /// The destination logger that will be used when this logger is flushed
        /// </summary>
        public ITargetLogger DestinationLogger { get; }

        /// <summary>
        /// The queue of actions to be performed
        /// </summary>
        private ConcurrentQueue<Action> LoggingQueue { get; } = new ConcurrentQueue<Action>();

        /// <summary>
        /// Writes a <see cref="Project" /> level message to the build log with
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        public virtual void Log(Level messageLevel, string message)
        {
            this.disablingLock.AcquireReaderLock(1000);

            if (disabled)
            {
                this.DestinationLogger.Log(messageLevel, message);
            }
            else
            {
                this.LoggingQueue.Enqueue(() => { this.DestinationLogger.Log(messageLevel, message); });
            }

            this.disablingLock.ReleaseReaderLock();
        }

        /// <summary>
        /// Writes a <see cref="Project" /> level formatted message to the build 
        /// log with the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        public virtual void Log(Level messageLevel, string message, params object[] args)
        {
            this.disablingLock.AcquireReaderLock(1000);

            if (disabled)
            {
                this.DestinationLogger.Log(messageLevel, message, args);
            }
            else
            {
                this.LoggingQueue.Enqueue(() => { this.DestinationLogger.Log(messageLevel, message, args); });
            }

            this.disablingLock.ReleaseReaderLock();
        }

        /// <summary>
        /// Writes a <see cref="Task" /> task level message to the build log 
        /// with the given <see cref="Level" />.
        /// </summary>
        /// <param name="task">The <see cref="Task" /> from which the message originated.</param>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        public virtual void Log(Task task, Level messageLevel, string message)
        {
            this.disablingLock.AcquireReaderLock(1000);

            if (disabled)
            {
                this.DestinationLogger.Log(task, messageLevel, message);
            }
            else
            {
                this.LoggingQueue.Enqueue(() => { this.DestinationLogger.Log(task, messageLevel, message); });
            }

            this.disablingLock.ReleaseReaderLock();
        }

        /// <summary>
        /// Writes a <see cref="Target" /> level message to the build log with 
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="target">The <see cref="Target" /> from which the message orignated.</param>
        /// <param name="messageLevel">The level to log at.</param>
        /// <param name="message">The message to log.</param>
        public virtual void Log(Target target, Level messageLevel, string message)
        {
            this.disablingLock.AcquireReaderLock(1000);

            if (disabled)
            {
                this.DestinationLogger.Log(target, messageLevel, message);
            }
            else
            {
                this.LoggingQueue.Enqueue(() => { this.DestinationLogger.Log(target, messageLevel, message); });
            }

            this.disablingLock.ReleaseReaderLock();
        }

        /// <summary>
        /// Increases the indentation level of the log
        /// </summary>
        public void Indent()
        {
            this.disablingLock.AcquireReaderLock(1000);

            if (disabled)
            {
                this.DestinationLogger.Indent();
            }
            else
            {
                this.LoggingQueue.Enqueue(this.DestinationLogger.Indent);
            }

            this.disablingLock.ReleaseReaderLock();
        }

        /// <summary>
        /// Decreases the indentation level of the log
        /// </summary>
        public void Unindent()
        {
            this.disablingLock.AcquireReaderLock(1000);

            if (disabled)
            {
                this.DestinationLogger.Unindent();
            }
            else
            {
                this.LoggingQueue.Enqueue(this.DestinationLogger.Unindent);
            }

            this.disablingLock.ReleaseReaderLock();
        }

        /// <summary>
        /// Permanently disables the buffer.  Any queued log messages are immedately passed on to the <see cref="DestinationLogger"/>
        /// and any future log messages are logged immediately instead of placed into the buffer.  Dismantling this <see cref="BufferingTargetLogger"/>
        /// can happen only once and cannot be undone.
        /// </summary>
        /// <exception cref="InvalidOperationException">If <see cref="FlushAndDismantle"/> had alreaddy been called on this object</exception>
        public void FlushAndDismantle()
        {
            // With this write lock, no other thread can add to this queue until we flip the disabled flag,
            // at which time they will all stop using the queue
            this.disablingLock.AcquireWriterLock(30000);

            if (disabled)
            {
                this.disablingLock.ReleaseWriterLock();
                throw new InvalidOperationException("Buffer has already been dismantled");
            }

            this.Flush();
            this.disabled = true; // Very important we flip this before releasing the write lock
            this.disablingLock.ReleaseWriterLock();
        }

        /// <summary>
        /// Causes all pending log writes to be performed in the order in which they occured
        /// </summary>
        public virtual void Flush()
        {
            // Prevent two intances of this class from writing to the same destination logger at the same time
            lock (LoggerLocks[this.DestinationLogger])
            {
                Action action;
                while (this.LoggingQueue.TryDequeue(out action))
                {
                    action();
                }
            }
        }

        /// <summary>
        /// Always flush the queue before disposal
        /// </summary>
        public void Dispose()
        {
            this.Flush();
        }
    }
}
