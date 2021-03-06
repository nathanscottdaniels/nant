// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
// Original NAnt Copyright (C) 2001-2002 Gerry Shaw
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

// Eric Gunnerson
// Gerry Shaw (gerry_shaw@yahoo.com)

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Functions {
    [TestFixture]
    public class TargetFunctionsTest : BuildTestBase {
        [Test]
        public void Test_CurrentTarget() {

            string buildFragment = @"
                <project default='A'>
                    <target name='A'>
                        <property name='A.1' value='${target::get-current-target()}' />
                        <call target='B' />
                        <property name='A.2' value='${target::get-current-target()}' />
                    </target>

                    <target name='B' depends='C'>
                        <property name='B' value='${target::get-current-target()}' />
                    </target>

                    <target name='C'>
                        <property name='C' value='${target::get-current-target()}' />
                    </target>
                </project>";

            Project project = CreateFilebasedProject(buildFragment);
            var propertyAccessor = new PropertyAccessor(project, project.RootTargetCallStack);
            ExecuteProject(project);
            
            // check whether all expected properties exist
            Assert.IsTrue(propertyAccessor.Contains("A.1"), "Property \"A.1\" does not exist.");
            Assert.IsTrue(propertyAccessor.Contains("A.2"), "Property \"A.2\" does not exist.");
            Assert.IsTrue(propertyAccessor.Contains("B"), "Property \"B\" does not exist.");
            Assert.IsTrue(propertyAccessor.Contains("C"), "Property \"C\" does not exist.");

            // check values
            Assert.AreEqual("A", propertyAccessor["A.1"], "A.1");
            Assert.AreEqual("A", propertyAccessor["A.2"], "A.2");
            Assert.AreEqual("B", propertyAccessor["B"], "B");
            Assert.AreEqual("C", propertyAccessor["C"], "C");
        }
    }
}
