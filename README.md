# pNAnt

### What is it? 

pNAnt is a fork of NAnt and NAntContrib that provides parallelization support and fixes some of the more serious shortcomings of NAnt.  You can find the original NAnt project here:

http://nant.sourceforge.net/

and the source here:

https://github.com/nant/nant

#### How do I pronounce it?
The 'p' is silent as in 'pneumatic'.  Or you can just call it 'pea-nant'.

# Additions to NAnt

The primary focus of pNAnt is to add the ability to run multiple build targets in parallel.  To this end, several tasks and attributes were added which allow fine-grained control over which targets can be run in parallel and which must be run sequentially.

## Tasks
The following tasks have been added:

* [`<parallel/>`](https://github.com/nathanscottdaniels/pnant/wiki/parallel--task): Specifies targets to run in parallel
* [`<sequence/>`](https://github.com/nathanscottdaniels/pnant/wiki/sequence--task): Specifies targets to tun in sequence
* [`<mutex/>`](https://github.com/nathanscottdaniels/pnant/wiki/mutex--task): Wraps a block of tasks in a mutual-exclusion lock

## Property Scope
No longer are all properties globally scoped.  Properties can now have one of three scopes:
* `global`: Visible to all targets, projects, and threads
* `thread`: Visible to all targets and projects in the current thread and child threads.
* `target`: Visible only to the target in which it is declared.

See the [Property Scope](https://github.com/nathanscottdaniels/pnant/wiki/property-scope) page for more information.

## Logging Changes
See [Logging Parallel Activities](https://github.com/nathanscottdaniels/pnant/wiki/logging-parallel-activities) for information on how logging works when dealing with multiple threads.

## Breaking Changes
pNAnt now requires .NET Framework 4.6.  I am sorry.

Any existing NAnt build files will work perfectly fine with pNAnt.

Plugins that were written for NAnt _might_ work without recompilation, however it is unlikely.  Changes were made to the NAnt API that will require any custom tasks or functions to be modified slightly.  No (non-deprecated) functionality was removed but method signatures were changed.

# License

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
