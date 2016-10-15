// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
// Original NAnt Copyright (C) 2001-2003 Gerry Shaw
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
// Ian McLean (ianm@activestate.com)
// Mitch Denny (mitch.denny@monash.net)
// Gert Driesen (drieseng@users.sourceforge.net)
// Charles Chan (cchan_qa@users.sourceforge.net)

using System.Globalization;
using System.Threading;
using System.Xml;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class XmlPeekTest : BuildTestBase {
        private CultureInfo originalCulture;
        private const string _projectXml = "<?xml version=\"1.0\"?>"
            + "<project>"
                + "<xmlpeek {0} property=\"configuration.server\" />"
            + "</project>";

        private const string _projectXmlWithNamespace = "<?xml version=\"1.0\"?>"
            + "<project>"
                + "<xmlpeek {0} property=\"configuration.server\">"
                    + "<namespaces>"
                        + "<namespace prefix=\"x\" uri=\"http://www.gordic.cz/shared/project-config/v_1.0.0.0\" />"
                    + "</namespaces>"
                + "</xmlpeek>"
            + "</project>";

        private const string _validXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" 
            + "<configuration>" 
                + "<appSettings>"
                    + "<add key=\"server\" value=\"testhost.somecompany.com\" />"
                + "</appSettings>"
            + "</configuration>";
            
        private const string _validXmlWithMultipleNodes = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" 
            + "<configuration>" 
                + "<appSettings>"
                    + "<add key=\"server\" value=\"testhost.somecompany.com\" />"
                    + "<add key=\"server.backup\" value=\"backuphost1.somecompany.com\" />"
                    + "<add key=\"server.backup\" value=\"backuphost2.somecompany.com\" />"
                    + "<add key=\"server.backup\" value=\"-5\" />"
                + "</appSettings>"
                + "<constants>"
                    + "<pi>3.14159265</pi>"
                    + "<c>2.99E8</c>" // speed of light
                + "</constants>"
            + "</configuration>";

        private const string _validXmlWithNamespace = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>"
            + "<configuration xmlns=\"http://www.gordic.cz/shared/project-config/v_1.0.0.0\">"
                + "<appSettings>"
                    + "<add key=\"server\" value=\"testhost.somecompany.com\" />"
                + "</appSettings>"
            + "</configuration>";
        [Test]
        public void Test_PeekValidXml() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXml);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key='server']/@value\"",
                xmlFile);

            // create project
            Project p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("testhost.somecompany.com", propertyAccessor["configuration.server"]);
        }

        /// <summary>
        /// Tests to make sure that XmlPeek will output xml format when multiple nodes 
        /// are requested.
        /// </summary>
        [Test]
        public void Test_PeekValidXmlRetrieveInnerNodes() {
            string expectedInnerText = "<pi>3.14159265</pi><c>2.99E8</c>";
            Project p;

            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXmlWithMultipleNodes);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/constants\"",
                xmlFile);

            // create project
            p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual(expectedInnerText, 
                propertyAccessor["configuration.server"], 
                string.Format("Expected Output: {0}\nActual Output: {1}", 
                expectedInnerText, propertyAccessor["configuration.server"]));
        }
        
        [Test]
        public void Test_PeekValidXmlRetrieveDoubleValue() {
            Project p;

            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXmlWithMultipleNodes);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/constants/pi\"",
                xmlFile);

            // create project
            p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("3.14159265", propertyAccessor["configuration.server"], "#A");

            // set-up task attributes
            taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/constants/c\"",
                xmlFile);

            // create project
            p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("2.99E8", propertyAccessor["configuration.server"], "#B");

            // set-up task attributes
            taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"-(56.43)\"",
                xmlFile);

            // create project
            p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("-56.43", propertyAccessor ["configuration.server"], "#C");
        }

        [Test]
        public void Test_PeekValidXmlUsingXPathNumericFunction() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXmlWithMultipleNodes);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"count(/configuration/appSettings/add)\"",
                xmlFile);

            // create project
            Project p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("4", propertyAccessor ["configuration.server"]);
        }

        [Test]
        public void Test_PeekValidXmlUsingXPathBooleanFunction() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXmlWithMultipleNodes);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"boolean(count(/configuration/appSettings/add) = 4)\"",
                xmlFile);

            // create project
            Project p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("True", propertyAccessor ["configuration.server"]);
        }

        [Test]
        public void Test_PeekValidXmlUsingXPathNodeExpression() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXmlWithMultipleNodes);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key='server.backup'][2]/@value\"",
                xmlFile);

            // create project
            Project p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("backuphost2.somecompany.com", propertyAccessor ["configuration.server"]);
        }

        [Test]
        public void Test_PeekValidXmlWithNamespace() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxmlnamespace.xml", _validXmlWithNamespace);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/x:configuration/x:appSettings/x:add[@key='server']/@value\"", xmlFile);

            // create project
            Project p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXmlWithNamespace, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("testhost.somecompany.com", propertyAccessor ["configuration.server"]);
        }

        /// <summary>
        /// Ensures a <see cref="BuildException" /> is thrown when a nodeindex
        /// is set that is out of range.
        /// </summary>
        [Test]
        public void Test_PeekValidXmlNodeIndexOutOfRange() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXml);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"" +
                " nodeindex=\"2\"", xmlFile);

            // create project
            Project p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            try {
                // execute build
                ExecuteProject(p);
                // have the test fail
                Assert.Fail("Build should have failed.");
            } catch (TestBuildException ex) {
                // assert that a BuildException was the cause of the TestBuildException
                Assert.IsTrue((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
            }
        }

        /// <summary>
        /// Ensures a <see cref="BuildException" /> is thrown when no nodes 
        /// match the XPath expression.
        /// </summary>
        [Test]
        public void Test_PeekValidXmlNoMatches() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXml);

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='anythingisok']/@value\"",
                xmlFile);

            // create project
            Project p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));

            try {
                // execute build
                ExecuteProject(p);
                // have the test fail
                Assert.Fail("Build should have failed.");
            } catch (TestBuildException ex) {
                // assert that a BuildException was the cause of the TestBuildException
                Assert.IsTrue((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
            }
        }

        [Test]
        public void Test_PeekNodeIndex() {
            Project p;
            string taskAttributes;

            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXmlWithMultipleNodes);

            // set-up task attributes
            taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key='server.backup']/@value\"",
                xmlFile);

            // create project
            p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            var propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("backuphost1.somecompany.com", propertyAccessor ["configuration.server"], "#A");

            // set-up task attributes
            taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key='server.backup']/@value\" nodeindex=\"2\"",
                xmlFile);

            // create project
            p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("-5", propertyAccessor ["configuration.server"], "#B");

            // set-up task attributes
            taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key='server.backup']/@value\" nodeindex=\"1\"",
                xmlFile);

            // create project
            p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));
            propertyAccessor = new PropertyAccessor(p, p.RootTargetCallStack);

            // execute build
            ExecuteProject(p);

            // ensure the correct node was read
            Assert.AreEqual("backuphost2.somecompany.com", propertyAccessor ["configuration.server"], "#C");

        }

        [Test]
        public void Test_PeekEmptyFile() {
            // create empty file
            string xmlFile = CreateTempFile("empty.xml");

            // set-up task attributes
            string taskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"",
                xmlFile);

            // create project
            Project p = CreateFilebasedProject(string.Format(CultureInfo.InvariantCulture,
                _projectXml, taskAttributes, "${configuration.server}"));

            try {
                // execute build
                ExecuteProject(p);
                // have the test fail
                Assert.Fail("Build should have failed.");
            } catch (TestBuildException ex) {
                // assert that a BuildException was the cause of the TestBuildException
                Assert.IsTrue((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
                // assert that an XmlException was the cause of the BuildException
                Assert.IsTrue((ex.InnerException.InnerException != null && ex.InnerException.InnerException.GetType() == typeof(XmlException)));
            }
        }
        protected override void SetUp() {
            base.SetUp();

            // save current culture
            originalCulture = Thread.CurrentThread.CurrentCulture;

            // change current culture
            CultureInfo c = new CultureInfo(originalCulture.Name, false);
            c.NumberFormat.NegativeSign = "neg";
            Thread.CurrentThread.CurrentCulture = c;
        }

        protected override void TearDown() {
            base.TearDown();

            // restore original culture
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
   }
}
