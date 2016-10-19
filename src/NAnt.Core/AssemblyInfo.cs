// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
// Original NAnt Copyright (C) 2001 Gerry Shaw
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
// Scott Hernandez (ScottHernandez@hotmail.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System.Reflection;
using System.Runtime.CompilerServices;

// This will not compile with Visual Studio.  If you want to build a signed
// executable use the NAnt build file.  To build under Visual Studio just
// exclude this file from the build.
[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile(@"..\NAnt.key")]
[assembly: AssemblyKeyName("")]

[assembly: InternalsVisibleTo("NAnt.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010015dcf96a8654abf9ce02478cc14dd549174d22ffc7f49646a0541a9fa6cdeaf01e4bb78d4699b14cff75d7a35bf42c0b655d827c9749bde6625c87d76255e6e5f864eec02b406ad2e346e2a035ba231c8537e72f26df215d47ce9b60a9c5b97b9944c5f3d222afb0a834d8e8896976259fa9705877e321dfe258e0e207dec5e7")]
[assembly: InternalsVisibleTo("NantContrib.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010015dcf96a8654abf9ce02478cc14dd549174d22ffc7f49646a0541a9fa6cdeaf01e4bb78d4699b14cff75d7a35bf42c0b655d827c9749bde6625c87d76255e6e5f864eec02b406ad2e346e2a035ba231c8537e72f26df215d47ce9b60a9c5b97b9944c5f3d222afb0a834d8e8896976259fa9705877e321dfe258e0e207dec5e7")]