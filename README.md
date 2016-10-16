# pNAnt

### What is it? 

pNAnt is a fork of NAnt that provides parallelization support and fixes some of the more serious shortcomings of NAnt.  You can find the original NAnt project here:

http://nant.sourceforge.net/

and the source here:

https://github.com/nant/nant

# Additions to NAnt

The primary focus of pNAnt is to add the ability to run multiple build targets in parallel.  To this end, several tasks and attributes were added which allow fine-grained control over which targets can be run in parallel and which must be run sequentially.

## Tasks
The following tasks have been added:

* [`<parallel/>`](#parallel): Specifies targets to run in parallel
* [`<sequence/>`](#sequence): Specifies targets to tun in sequence
* [`<mutex/>`](#mutex): Wraps a block of tasks in a mutual-exclusion lock

### `<parallel/>`

The __`parallel`__ task is the primary addition made by pNAnt.  This task allows you to call multiple targets simultaneously.
The task has the following attributes (there are no required attributes):

##### Optional Attributes:
* __`name`__: Allows you to give a name to the task.  This serves both to increase build script readability and is also used by pNAnt when logging to the console.  
* __`description`__: Allows you to describe this task to improve buildscript readability.  Unlike `name`, the value of this attribute is ignored by pNAnt.  
* __`forceSequential`__: A boolean value that, when evaluated to `true`, foreces this `parallal` task to execute each target in order, rather than in parallel, thereby disabling any parallalization.  This is useful if you would like to run targets in parallel only under certain circumstances.  _The default value is `false`, obviously_.  
* __`cacophony`__: A boolean value that, when evaluated to `true`, makes all children of this `parallel` task output their log messages immediately as they happen.  See below for more information about cacophony logging.  The use of this attribute is not recommended.  _The default value is `false`_.  
* __`if`__: Same as the `if` attrubute of other NAnt tasks.
* __`unless`__: Same as the `unless` attrubute of other NAnt tasks.

#### Nested Elements
The `parallel` task supports three nested element types which are used to specify the actions that should be performed in parallel:

#### __`<pcall>`__
The `pcall` element is the the most important element within the `<parallel>`' task.  It specifies a target to perform in parallel.  The `pcall` element has one required attrubute, __`target`__, which specifies the name of the target.  In addition, the __`if`__ and __`unless`__ attributes are supported and behave as expected.

__Examples:__
>`<pcall target="target_name"/>`  
>`<pcall target="${target_name}" if="${something}" unless="${something_else}/>`

#### __`<sequence>`__
The `sequence` task specifies targets that are to be run in sequence, but the sequence as a whole can be run in parallel with with other children on this __`<parallel>`__

License
-------
Copyright (C) 2016 Nathan Daniels
Original NAnt Copyright (C) 2001-2012 Gerry Shaw

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

As a special exception, the copyright holders of this software give you
permission to link the assemblies with independent modules to produce new
assemblies, regardless of the license terms of these independent modules,
and to copy and distribute the resulting assemblies under terms of your
choice, provided that you also meet, for each linked independent module,
the terms and conditions of the license of that module. An independent
module is a module which is not derived from or based on these assemblies.
If you modify this software, you may extend this exception to your version
of the software, but you are not obligated to do so. If you do not wish to
do so, delete this exception statement from your version. 

A copy of the GNU General Public License is available in the COPYING.txt file 
included with all NAnt distributions.

For more licensing information refer to the GNU General Public License on the 
GNU Project web site.
http://www.gnu.org/copyleft/gpl.html

