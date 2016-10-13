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
