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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (imaclean@gmail.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core
{
    /// <summary>
    /// Central representation of a NAnt project.
    /// </summary>
    /// <example>
    ///   <para>
    ///   The <see cref="Run" /> method will initialize the project with the build
    ///   file specified in the constructor and execute the default target.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// Project p = new Project("foo.build", Level.Info);
    /// p.Run();
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   If no target is given, the default target will be executed if specified 
    ///   in the project.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// Project p = new Project("foo.build", Level.Info);
    /// p.Execute("build");
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable()]
    public class Project : ITargetLogger
    {

        /// <summary>
        /// Holds the logger for this class.
        /// </summary>
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // XML element and attribute names that are not defined in metadata
        private const string RootXml = "project";
        private const string ProjectNameAttribute = "name";
        private const string ProjectDefaultAttribte = "default";
        private const string ProjectBaseDirAttribute = "basedir";
        private const string TargetXml = "target";
        private const string WildTarget = "*";

        /// <summary>
        /// Constant for the "visiting" state, used when traversing a DFS of 
        /// target dependencies.
        /// </summary>
        private const string Visiting = "VISITING";

        /// <summary>
        /// Constant for the "visited" state, used when traversing a DFS of 
        /// target dependencies.
        /// </summary>
        private const string Visited = "VISITED";

        // named properties
        internal const string NAntPlatform = "nant.platform";
        internal const string NAntPlatformName = NAntPlatform + ".name";
        internal const string NAntPropertyFileName = "nant.filename";
        internal const string NAntPropertyVersion = "nant.version";
        internal const string NAntPropertyLocation = "nant.location";
        internal const string NAntPropertyProjectName = "nant.project.name";
        internal const string NAntPropertyProjectBuildFile = "nant.project.buildfile";
        internal const string NAntPropertyProjectBaseDir = "nant.project.basedir";
        internal const string NAntPropertyProjectDefault = "nant.project.default";
        internal const string NAntPropertyOnSuccess = "nant.onsuccess";
        internal const string NAntPropertyOnFailure = "nant.onfailure";

        /// <summary>
        /// Occurs when a build is started.
        /// </summary>
        public event BuildEventHandler BuildStarted;

        /// <summary>
        /// Occurs when a build has finished.
        /// </summary>
        public event BuildEventHandler BuildFinished;

        /// <summary>
        /// Occurs when logging a build is started.
        /// </summary>
        public event BuildEventHandler BuildLoggingStarted;

        /// <summary>
        /// Occurs when logging a build has finished.
        /// </summary>
        public event BuildEventHandler BuildLoggingFinished;

        /// <summary>
        /// Occurs when a target is started.
        /// </summary>
        public event TargetBuildEventHandler TargetStarted;

        /// <summary>
        /// Occurs when a target has finished.
        /// </summary>
        public event TargetBuildEventHandler TargetFinished;

        /// <summary>
        /// Occurs when a task is started.
        /// </summary>
        public event TaskBuildEventHandler TaskStarted;

        /// <summary>
        /// Occurs when a task has finished.
        /// </summary>
        public event TaskBuildEventHandler TaskFinished;

        /// <summary>
        /// Occurs when logging for  a target is started.
        /// </summary>
        public event TargetBuildEventHandler TargetLoggingStarted;

        /// <summary>
        /// Occurs when logging for  a target has finished.
        /// </summary>
        public event TargetBuildEventHandler TargetLoggingFinished;

        /// <summary>
        /// Occurs when logging for  a task is started.
        /// </summary>
        public event TaskBuildEventHandler TaskLoggingStarted;

        /// <summary>
        /// Occurs when logging for a task has finished.
        /// </summary>
        public event TaskBuildEventHandler TaskLoggingFinished;

        /// <summary>
        /// Occurs when a message is logged.
        /// </summary>
        public event BuildEventHandler MessageLogged;

        private string _baseDirectory;
        private string _projectName = "";
        private string _defaultTargetName;
        private int _indentationSize = 2;
        private int _indentationLevel = 0;
        private BuildListenerCollection _buildListeners = new BuildListenerCollection();
        private StringCollection _buildTargets = new StringCollection();
        private TargetCollection _targets = new TargetCollection();
        private LocationMap _locationMap = new LocationMap();
        private PropertyDictionary _frameworkNeutralProperties;

        // info about frameworks
        private FrameworkInfoDictionary _frameworks = new FrameworkInfoDictionary();
        private FrameworkInfo _runtimeFramework;
        private FrameworkInfo _targetFramework;

        [NonSerialized()]
        private XmlNode _configurationNode;
        [NonSerialized()]
        private XmlDocument _doc; // set in ctorHelper
        [NonSerialized()]
        private XmlNamespaceManager _nsMgr = new XmlNamespaceManager(new NameTable()); //used to map "nant" to default namespace.
        [NonSerialized()]
        private DataTypeBaseDictionary _dataTypeReferences = new DataTypeBaseDictionary();

        /// <summary>
        /// Holds the default threshold for build loggers.
        /// </summary>
        private Level _threshold = Level.Info;

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// document, message threshold and indentation level.
        /// </summary>
        /// <param name="doc">Any valid build format will do.</param>
        /// <param name="threshold">The message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        public Project(XmlDocument doc, Level threshold, int indentLevel)
        {
            // use NAnt settings from application configuration file for loading 
            // internal configuration settings
            _configurationNode = GetConfigurationNode();

            // initialize project
            CtorHelper(doc, threshold, indentLevel, Optimizations.None);
        }

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// document, message threshold and indentation level, and using 
        /// the specified <see cref="XmlNode" /> to load internal configuration
        /// settings.
        /// </summary>
        /// <param name="doc">Any valid build format will do.</param>
        /// <param name="threshold">The message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        /// <param name="configurationNode">The <see cref="XmlNode" /> NAnt should use to initialize configuration settings.</param>
        /// <remarks>
        /// This constructor is useful for developers using NAnt as a class
        /// library.
        /// </remarks>
        public Project(XmlDocument doc, Level threshold, int indentLevel, XmlNode configurationNode)
        {
            // set configuration node to use for loading internal configuration 
            // settings
            _configurationNode = configurationNode;

            // initialize project
            CtorHelper(doc, threshold, indentLevel, Optimizations.None);
        }

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// source, message threshold and indentation level.
        /// </summary>
        /// <param name="uriOrFilePath">
        /// <para>The full path to the build file.</para>
        /// <para>This can be of any form that <see cref="M:XmlDocument.Load(string)" /> accepts.</para>
        /// </param>
        /// <param name="threshold">The message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        /// <remarks>
        /// If the source is a uri of form 'file:///path' then use the path part.
        /// </remarks>
        public Project(string uriOrFilePath, Level threshold, int indentLevel)
        {
            // use NAnt settings from application configuration file for loading 
            // internal configuration settings
            _configurationNode = GetConfigurationNode();

            // initialize project
            CtorHelper(LoadBuildFile(uriOrFilePath), threshold, indentLevel,
                Optimizations.None);
        }

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// source, message threshold and indentation level, and using 
        /// the specified <see cref="XmlNode" /> to load internal configuration
        /// settings.
        /// </summary>
        /// <param name="uriOrFilePath">
        /// <para>The full path to the build file.</para>
        /// <para>This can be of any form that <see cref="M:XmlDocument.Load(string)" /> accepts.</para>
        /// </param>
        /// <param name="threshold">The message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        /// <param name="configurationNode">The <see cref="XmlNode" /> NAnt should use to initialize configuration settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configurationNode" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// If the source is a uri of form 'file:///path' then use the path part.
        /// </remarks>
        public Project(string uriOrFilePath, Level threshold, int indentLevel, XmlNode configurationNode)
        {
            // set configuration node to use for loading internal configuration 
            // settings
            _configurationNode = configurationNode;

            // initialize project
            CtorHelper(LoadBuildFile(uriOrFilePath), threshold, indentLevel,
                Optimizations.None);
        }

        /// <summary>
        /// Initializes a <see cref="Project" /> as subproject of the specified
        /// <see cref="Project" />.
        /// </summary>
        /// <param name="uriOrFilePath">
        /// <para>The full path to the build file.</para>
        /// <para>This can be of any form that <see cref="M:XmlDocument.Load(string)" /> accepts.</para>
        /// </param>
        /// <param name="parent">The parent <see cref="Project" />.</param>
        /// <remarks>
        /// Optimized for framework initialization projects, by skipping automatic
        /// discovery of extension assemblies and framework configuration.
        /// </remarks>
        internal Project(string uriOrFilePath, Project parent)
        {
            // set configuration node to use for loading internal configuration 
            // settings
            _configurationNode = parent.ConfigurationNode;

            // initialize project
            CtorHelper(LoadBuildFile(uriOrFilePath), parent.Threshold,
                parent.IndentationLevel + 1, Optimizations.SkipFrameworkConfiguration);

            // add listeners of current project to new project
            AttachBuildListeners(parent.BuildListeners);

            // inherit discovered frameworks from current project
            foreach (FrameworkInfo framework in parent.Frameworks)
            {
                Frameworks.Add(framework.Name, framework);
            }

            // have the new project inherit the runtime framework from the 
            // current project
            RuntimeFramework = parent.RuntimeFramework;

            // have the new project inherit the current framework from the 
            // current project 
            TargetFramework = parent.TargetFramework;
        }

        /// <summary>
        /// Initializes a <see cref="Project" /> with <see cref="Threshold" />
        /// set to <see cref="Level.None" />, and <see cref="IndentationLevel" />
        /// set to 0.
        /// </summary>
        /// <param name="doc">An <see cref="XmlDocument" /> containing the build script.</param>
        /// <remarks>
        /// Optimized for framework initialization projects, by skipping automatic
        /// discovery of extension assemblies and framework configuration.
        /// </remarks>
        internal Project(XmlDocument doc)
        {
            // initialize project
            CtorHelper(doc, Level.None, 0, Optimizations.SkipAutomaticDiscovery |
                Optimizations.SkipFrameworkConfiguration);
        }

        /// <summary>
        /// Gets or sets the indendation level of the build output.
        /// </summary>
        /// <value>
        /// The indentation level of the build output.
        /// </value>
        /// <remarks>
        /// To change the <see cref="IndentationLevel" />, the <see cref="Indent()" /> 
        /// and <see cref="Unindent()" /> methods should be used.
        /// </remarks>
        public int IndentationLevel
        {
            get { return _indentationLevel; }
        }

        /// <summary>
        /// Gets or sets the indentation size of the build output.
        /// </summary>
        /// <value>
        /// The indendation size of the build output.
        /// </value>
        public int IndentationSize
        {
            get { return _indentationSize; }
        }

        /// <summary>
        /// Gets or sets the default threshold level for build loggers.
        /// </summary>
        /// <value>
        /// The default threshold level for build loggers.
        /// </value>
        public Level Threshold
        {
            get { return _threshold; }
            set { _threshold = value; }
        }

        /// <summary>
        /// Gets the name of the <see cref="Project" />.
        /// </summary>
        /// <value>
        /// The name of the <see cref="Project" /> or an empty <see cref="string" />
        /// if no name is specified.
        /// </value>
        public string ProjectName
        {
            get { return _projectName; }
        }

        /// <summary>
        /// Gets or sets the base directory used for relative references.
        /// </summary>
        /// <value>
        /// The base directory used for relative references.
        /// </value>
        /// <exception cref="BuildException">The directory is not rooted.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="BaseDirectory" /> gets and sets the built-in property 
        /// named "nant.project.basedir".
        /// </para>
        /// </remarks>
        public string BaseDirectory
        {
            get
            {
                if (_baseDirectory == null)
                {
                    return null;
                }

                if (!Path.IsPathRooted(_baseDirectory))
                {
                    throw new BuildException(string.Format(CultureInfo.InstalledUICulture,
                        "Invalid base directory '{0}'. The project base directory"
                        + "must be rooted.", _baseDirectory), Location.UnknownLocation);
                }

                return _baseDirectory;
            }
            set
            {
                if (!Path.IsPathRooted(value))
                {
                    throw new BuildException(string.Format(CultureInfo.InstalledUICulture,
                        "Invalid base directory '{0}'. The project base directory"
                        + "must be rooted.", value), Location.UnknownLocation);
                }

                this.SetGlobalProperty(NAntPropertyProjectBaseDir, _baseDirectory = value);
            }
        }

        /// <summary>
        /// Gets the <see cref="XmlNamespaceManager" />.
        /// </summary>
        /// <value>
        /// The <see cref="XmlNamespaceManager" />.
        /// </value>
        /// <remarks>
        /// The <see cref="NamespaceManager" /> defines the current namespace 
        /// scope and provides methods for looking up namespace information.
        /// </remarks>
        public XmlNamespaceManager NamespaceManager
        {
            get { return _nsMgr; }
        }

        /// <summary>
        /// Gets the <see cref="Uri" /> form of the current project definition.
        /// </summary>
        /// <value>
        /// The <see cref="Uri" /> form of the current project definition.
        /// </value>
        public Uri BuildFileUri
        {
            get
            {
                //TODO: Need to remove this.
                if (Document == null || String.IsNullOrEmpty(Document.BaseURI))
                {
                    return null; //new Uri("http://localhost");
                }
                else
                {
                    // manually escape '#' in URI (why doesn't .NET do this?) to allow
                    // projects in paths containing a '#' character
                    string escapedUri = Document.BaseURI.Replace("#", Uri.HexEscape('#'));
                    // return escaped URI
                    return new Uri(escapedUri);
                }
            }
        }

        /// <summary>
        /// Gets a collection of available .NET frameworks.
        /// </summary>
        /// <value>
        /// A collection of available .NET frameworks.
        /// </value>
        public FrameworkInfoDictionary Frameworks
        {
            get { return _frameworks; }
        }

        /// <summary>
        /// Gets the list of supported frameworks filtered by the specified
        /// <see cref="FrameworkTypes" /> parameter.
        /// </summary>
        /// <param name="types">A bitwise combination of <see cref="FrameworkTypes" /> values that filter the frameworks to retrieve.</param>
        /// <returns>
        /// An array of type <see cref="FrameworkInfo" /> that contains the
        /// frameworks specified by the <paramref name="types" /> parameter,
        /// sorted on name.
        /// </returns>
        internal FrameworkInfo[] GetFrameworks(FrameworkTypes types)
        {
            ArrayList matches = new ArrayList(Frameworks.Count);

            foreach (FrameworkInfo framework in Frameworks.Values)
            {
                if ((types & FrameworkTypes.InstallStateMask) != 0)
                {
                    if ((types & FrameworkTypes.Installed) == 0 && framework.IsValid)
                        continue;
                    if ((types & FrameworkTypes.NotInstalled) == 0 && !framework.IsValid)
                        continue;
                }

                if ((types & FrameworkTypes.DeviceMask) != 0)
                {
                    switch (framework.ClrType)
                    {
                        case ClrType.Compact:
                            if ((types & FrameworkTypes.Compact) == 0)
                                continue;
                            break;
                        case ClrType.Desktop:
                            if ((types & FrameworkTypes.Desktop) == 0)
                                continue;
                            break;
                        case ClrType.Browser:
                            if ((types & FrameworkTypes.Browser) == 0)
                                continue;
                            break;
                        default:
                            throw new NotSupportedException(string.Format(
                                CultureInfo.InvariantCulture, "CLR type '{0}'"
                                + " is not supported.", framework.ClrType));
                    }
                }

                if ((types & FrameworkTypes.VendorMask) != 0)
                {
                    switch (framework.Vendor)
                    {
                        case VendorType.Mono:
                            if ((types & FrameworkTypes.Mono) == 0)
                                continue;
                            break;
                        case VendorType.Microsoft:
                            if ((types & FrameworkTypes.MS) == 0)
                                continue;
                            break;
                    }
                }

                matches.Add(framework);
            }

            matches.Sort(FrameworkInfo.NameComparer);

            FrameworkInfo[] frameworks = new FrameworkInfo[matches.Count];
            matches.CopyTo(frameworks);
            return frameworks;
        }

        /// <summary>
        /// Gets the framework in which NAnt is currently running.
        /// </summary>
        /// <value>
        /// The framework in which NAnt is currently running.
        /// </value>
        public FrameworkInfo RuntimeFramework
        {
            get { return _runtimeFramework; }
            set { _runtimeFramework = value; }
        }

        /// <summary>
        /// Gets or sets the framework to use for compilation.
        /// </summary>
        /// <value>
        /// The framework to use for compilation.
        /// </value>
        /// <exception cref="ArgumentNullException">The value specified is <see langword="null" />.</exception>
        /// <exception cref="BuildException">The specified framework is not installed, or not configured correctly.</exception>
        /// <remarks>
        /// We will use compiler tools and system assemblies for this framework 
        /// in framework-related tasks.
        /// </remarks>
        public FrameworkInfo TargetFramework
        {
            get { return _targetFramework; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                value.Validate();
                _targetFramework = value;
                UpdateTargetFrameworkProperties();
            }
        }

        /// <summary>
        /// Gets the name of the platform on which NAnt is currently running.
        /// </summary>
        /// <value>
        /// The name of the platform on which NAnt is currently running.
        /// </value>
        /// <remarks>
        /// <para>
        /// Possible values are:
        /// </para>
        /// <list type="bullet">
        ///     <item>
        ///         <description>win32</description>
        ///     </item>
        ///     <item>
        ///         <description>unix</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="BuildException">NAnt does not support the current platform.</exception>
        public string PlatformName
        {
            get
            {
                if (PlatformHelper.IsWin32)
                {
                    return "win32";
                }
                else if (PlatformHelper.IsUnix)
                {
                    return "unix";
                }
                else
                {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1060"),
                        Environment.OSVersion.Platform.ToString(CultureInfo.InvariantCulture),
                        (int)Environment.OSVersion.Platform));
                }
            }
        }

        /// <summary>
        /// Gets the path to the build file.
        /// </summary>
        /// <value>
        /// The path to the build file, or <see langword="null" /> if the build
        /// document is not file backed.
        /// </value>
        public string BuildFileLocalName
        {
            get
            {
                if (BuildFileUri != null && BuildFileUri.IsFile)
                {
                    return BuildFileUri.LocalPath;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the active <see cref="Project" /> definition.
        /// </summary>
        /// <value>
        /// The active <see cref="Project" /> definition.
        /// </value>
        public XmlDocument Document
        {
            get { return _doc; }
        }

        /// <summary>
        /// Gets the <see cref="XmlNode" /> NAnt should use to initialize 
        /// configuration settings.
        /// </summary>
        /// <value>
        /// The <see cref="XmlNode" /> NAnt should use to initialize 
        /// configuration settings.
        /// </value>
        public XmlNode ConfigurationNode
        {
            get { return _configurationNode; }
        }

        /// <remarks>
        /// Gets the name of the target that will be executed when no other 
        /// build targets are specified.
        /// </remarks>
        /// <value>
        /// The name of the target that will be executed when no other 
        /// build targets are specified, or <see langword="null" /> if no
        /// default target is specified in the build file.
        /// </value>
        public string DefaultTargetName
        {
            get { return _defaultTargetName; }
        }

        /// <summary>
        /// Gets a value indicating whether tasks should output more build log 
        /// messages.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if tasks should output more build log message; 
        /// otherwise, <see langword="false" />.
        /// </value>
        public bool Verbose
        {
            get { return Level.Verbose >= Threshold; }
        }

        /// <summary>
        /// The list of targets to build.
        /// </summary>
        /// <remarks>
        /// Targets are built in the order they appear in the collection.  If 
        /// the collection is empty the default target will be built.
        /// </remarks>
        public StringCollection BuildTargets
        {
            get { return _buildTargets; }
        }

        /// <summary>
        /// Gets the <see cref="DataTypeBase" /> instances defined in this project.
        /// </summary>
        /// <value>
        /// The <see cref="DataTypeBase" /> instances defined in this project.
        /// </value>
        /// <remarks>
        /// <para>
        /// This is the collection of <see cref="DataTypeBase" /> instances that
        /// are defined by <see cref="DataTypeBase" /> (eg fileset) declarations.
        /// </para>
        /// </remarks>
        public DataTypeBaseDictionary DataTypeReferences
        {
            get { return _dataTypeReferences; }
        }

        /// <summary>
        /// When true, all parallelism should be disabled
        /// </summary>
        public Boolean ForceSequential { get; internal set; }

        /// <summary>
        /// Gets the targets defined in this project.
        /// </summary>
        /// <value>
        /// The targets defined in this project.
        /// </value>
        public TargetCollection Targets
        {
            get { return _targets; }
        }

        /// <summary>
        /// Gets the build listeners for this project. 
        /// </summary>
        /// <value>
        /// The build listeners for this project.
        /// </value>
        public BuildListenerCollection BuildListeners
        {
            get { return _buildListeners; }
        }

        internal LocationMap LocationMap
        {
            get { return _locationMap; }
        }

        /// <summary>
        /// Gets the root target call stack for the main thread of this project
        /// </summary>
        internal TargetCallStack RootTargetCallStack { get; private set; }

        /// <summary>
        /// Gets the properties defined in this project.
        /// </summary>
        /// <value>The properties defined in this project.</value>
        /// <remarks>
        /// <para>
        /// This is the collection of properties that are defined by the system 
        /// and property task statements.
        /// </para>
        /// <para>
        /// These properties can be used in expansion.
        /// </para>
        /// </remarks>
        internal PropertyDictionary GlobalProperties { get; private set; }

        /// <summary>
        /// Adds a new non-dynamic global property.
        /// </summary>
        /// <param name="name">The name of the property to add</param>
        /// <param name="value">The value</param>
        /// <param name="readOnly">Whether or not the property should be read only</param>
        internal void SetGlobalProperty(String name, String value, Boolean readOnly = false)
        {
            if (readOnly)
            {
                this.GlobalProperties.AddReadOnly(name, value);
            }
            else
            {
                this.GlobalProperties[name] = value;
            }
        }

        /// <summary>
        /// Gets the value of a global property
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <returns>The value</returns>
        internal String GetGlobalProperty(String name)
        {
            return this.GlobalProperties[name];
        }

        /// <summary>
        /// Returns the <see cref="Location"/> of the given node in an XML
        /// file loaded by NAnt.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The <paramref name="node" /> must be from an <see cref="XmlDocument" />
        ///   that has been loaded by NAnt.
        ///   </para>
        ///   <para>
        ///   NAnt also does not process any of the following node types:
        ///   </para>
        ///   <list type="bullet">
        ///     <item>
        ///         <description><see cref="XmlNodeType.Whitespace" /></description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="XmlNodeType.EndElement" /></description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="XmlNodeType.ProcessingInstruction" /></description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="XmlNodeType.XmlDeclaration" /></description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="XmlNodeType.DocumentType" /></description>
        ///     </item>
        ///   </list>
        ///   <para>
        ///   As a result, no location information is available for these nodes.
        ///   </para>
        /// </remarks>
        /// <param name="node">The <see cref="XmlNode" /> to get the <see cref="Location"/> for.</param>
        /// <returns>
        /// <see cref="Location"/> of the given node in an XML file loaded by NAnt, or
        /// <see cref="Location.UnknownLocation" /> if the node was not loaded from
        /// an XML file.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="node" /> is from an XML file that was not loaded by NAnt.</para>
        ///   <para>-or</para>
        ///   <para><paramref name="node" /> was not processed by NAnt (eg. an XML declaration).</para>
        /// </exception>
        public Location GetLocation(XmlNode node)
        {
            return LocationMap.GetLocation(node);
        }

        /// <summary>
        /// Signals that the last target has finished and logging for the build is complete.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        void ITargetLogger.OnBuildLoggingFinished(object sender, BuildEventArgs e)
        {
            if (BuildLoggingFinished != null)
            {
                BuildLoggingFinished(sender, e);
            }
        }

        /// <summary>
        /// Signals that logging for a build has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event is fired before any targets have started.
        /// </remarks>
        void ITargetLogger.OnBuildLoggingStarted(object sender, BuildEventArgs e)
        {
            if (BuildLoggingStarted != null)
            {
                BuildLoggingStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="BuildStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnBuildStarted(object sender, BuildEventArgs e)
        {
            if (BuildStarted != null)
            {
                BuildStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="BuildFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnBuildFinished(object sender, BuildEventArgs e)
        {
            if (BuildFinished != null)
            {
                BuildFinished(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TargetStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TargetBuildEventArgs" /> that contains the event data.</param>
        public void OnTargetStarted(object sender, TargetBuildEventArgs e)
        {
            if (TargetStarted != null)
            {
                TargetStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TargetFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TargetBuildEventArgs" /> that contains the event data.</param>
        public void OnTargetFinished(object sender, TargetBuildEventArgs e)
        {
            if (TargetFinished != null)
            {
                TargetFinished(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TargetStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TargetBuildEventArgs" /> that contains the event data.</param>
        public void OnTargetLoggingStarted(object sender, TargetBuildEventArgs e)
        {
            if (TargetStarted != null)
            {
                TargetLoggingStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TargetFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TargetBuildEventArgs" /> that contains the event data.</param>
        public void OnTargetLoggingFinished(object sender, TargetBuildEventArgs e)
        {
            if (TargetFinished != null)
            {
                TargetLoggingFinished(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TaskStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TaskBuildEventArgs" /> that contains the event data.</param>
        public void OnTaskStarted(object sender, TaskBuildEventArgs e)
        {
            if (TaskStarted != null)
            {
                TaskStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches the <see cref="TaskFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TaskBuildEventArgs" /> that contains the event data.</param>
        public void OnTaskFinished(object sender, TaskBuildEventArgs e)
        {
            if (TaskFinished != null)
            {
                TaskFinished(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TaskStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TaskBuildEventArgs" /> that contains the event data.</param>
        public void OnTaskLoggingStarted(object sender, TaskBuildEventArgs e)
        {
            if (TaskStarted != null)
            {
                TaskLoggingStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches the <see cref="TaskFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="TaskBuildEventArgs" /> that contains the event data.</param>
        public void OnTaskLoggingFinished(object sender, TaskBuildEventArgs e)
        {
            if (TaskFinished != null)
            {
                TaskLoggingFinished(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="MessageLogged" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnMessageLogged(BuildEventArgs e)
        {
            if (MessageLogged != null)
            {
                MessageLogged(this, e);
            }
        }

        /// <summary>
        /// Writes a <see cref="Project" /> level message to the build log with
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        void ITargetLogger.Log(Level messageLevel, string message)
        {
            BuildEventArgs eventArgs = new BuildEventArgs(this);

            eventArgs.Message = message;
            eventArgs.MessageLevel = messageLevel;
            OnMessageLogged(eventArgs);
        }

        /// <summary>
        /// Writes a <see cref="Project" /> level formatted message to the build 
        /// log with the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        void ITargetLogger.Log(Level messageLevel, string message, params object[] args)
        {
            BuildEventArgs eventArgs = new BuildEventArgs(this);

            eventArgs.Message = string.Format(CultureInfo.InvariantCulture, message, args);
            eventArgs.MessageLevel = messageLevel;
            OnMessageLogged(eventArgs);
        }

        /// <summary>
        /// Writes a <see cref="Task" /> task level message to the build log 
        /// with the given <see cref="Level" />.
        /// </summary>
        /// <param name="task">The <see cref="Task" /> from which the message originated.</param>
        /// <param name="stopwatch">The stopwatch of the task.</param>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        void ITargetLogger.Log(Task task, Stopwatch stopwatch, Level messageLevel, string message)
        {
            BuildEventArgs eventArgs = new TaskBuildEventArgs(task, stopwatch);

            eventArgs.Message = message;
            eventArgs.MessageLevel = messageLevel;
            OnMessageLogged(eventArgs);
        }

        /// <summary>
        /// Writes a <see cref="Target" /> level message to the build log with 
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="target">The <see cref="Target" /> from which the message orignated.</param>
        /// <param name="stopwatch">The stopwatch of the target.</param>
        /// <param name="messageLevel">The level to log at.</param>
        /// <param name="message">The message to log.</param>
        void ITargetLogger.Log(Target target, Stopwatch stopwatch, Level messageLevel, string message)
        {
            BuildEventArgs eventArgs = new TargetBuildEventArgs(target, stopwatch);

            eventArgs.Message = message;
            eventArgs.MessageLevel = messageLevel;
            OnMessageLogged(eventArgs);
        }

        /// <summary>
        /// Executes the default target.
        /// </summary>
        /// <remarks>
        /// No top level error handling is done. Any <see cref="BuildException" /> 
        /// will be passed onto the caller.
        /// </remarks>
        public virtual void Execute(TargetCallStack callStack)
        {
            if (BuildTargets.Count == 0 && !String.IsNullOrEmpty(DefaultTargetName))
            {
                BuildTargets.Add(DefaultTargetName);
            }

            //log the targets specified, or the default target if specified.
            StringBuilder sb = new StringBuilder();
            if (BuildTargets != null)
            {
                foreach (string target in BuildTargets)
                {
                    sb.Append(target);
                    sb.Append(" ");
                }
            }

            if (sb.Length > 0)
            {
                (this as ITargetLogger).Log(Level.Info, "Target(s) specified: " + sb.ToString());
                (this as ITargetLogger).Log(Level.Info, string.Empty);
            }
            else
            {
                (this as ITargetLogger).Log(Level.Info, string.Empty);
            }

            // initialize the list of Targets, and execute any global tasks.
            InitializeProjectDocument(Document, callStack);

            if (BuildTargets.Count == 0)
            {
                //It is okay if there are no targets defined in a build file. 
                //It just means we have all global tasks. -- skot
                //throw new BuildException("No Target Specified");
            }
            else
            {
                foreach (string targetName in BuildTargets)
                {
                    //do not force dependencies of build targets.
                    Execute(targetName, false, null, callStack);
                }
            }
        }

        /// <summary>
        /// Executes a specific target after first executing its dependencies.
        /// </summary>
        /// <param name="targetName">The name of the target to execute.</param>
        /// <param name="forceDependencies">Whether dependencies should be forced to execute.  Defaults to true</param>
        /// <param name="caller">The task responsible for calling this target.  Not required if <paramref name="callStack"/> is provided</param>
        /// <param name="callStack">Optionally, the current call stack for this task.  If not specified, the callstack of <paramref name="caller"/> is used.</param>
        /// <param name="specialLogger">An optional <see cref="ITargetLogger"/> that this target should use
        /// to log messages.  If not specified, the default logging will be used (<see cref="Project"/>)
        /// </param>
        /// <param name="arguments">Optionally, the arguments to provide to the target.  Should match those required by <see cref="Target.Parameters"/></param>
        /// <remarks>
        /// Global tasks are not executed.
        /// </remarks>
        public void Execute(
            string targetName,
            bool forceDependencies = true,
            Task caller = null,
            TargetCallStack callStack = null,
            ITargetLogger specialLogger = null,
            IList<CallArgument> arguments = null)
        {
            if (callStack == null && caller == null)
            {
                throw new ArgumentNullException("caller", "Either caller or callStack must not be null");
            }

            // sort the dependency tree, and run everything from the
            // beginning until we hit our targetName.
            // 
            // sorting checks if all the targets (and dependencies)
            // exist, and if there is any cycle in the dependency
            // graph.
            TargetCollection sortedTargets = TopologicalTargetSort(targetName, Targets);
            int currentIndex = 0;
            Target currentTarget;

            do
            {
                // determine target that should be executed
                currentTarget = (Target)sortedTargets[currentIndex++];

                // only execute targets that have not been executed already, if 
                // we are not forcing.
                if (forceDependencies || !currentTarget.Executed || currentTarget.Name == targetName)
                {
                    try
                    {
                        currentTarget.Execute(
                            callStack ?? caller.CallStack, 
                            specialLogger ?? (callStack ?? caller.CallStack).HeadLogger,
                            currentTarget.Name == targetName 
                                ? arguments 
                                : null);
                    }
                    catch (ArgumentException)
                    {
                        if (currentTarget.Name != targetName)
                        {
                            throw new BuildException(
                                String.Format(
                                    @"Target ""{0}"" requires arguments and cannot be in the ""depends"" list of target ""{1}"".  Please <call/> this target instead.",
                                    currentTarget.Name, targetName));
                        }

                        throw;
                    }
                }
            }
            while (currentTarget.Name != targetName);
        }

        /// <summary>
        /// Executes the default target and wraps in error handling and time 
        /// stamping.
        /// </summary>
        /// <returns>
        /// A <see cref="ProjectRunResult"/> with the results of the run
        /// </returns>
        public ProjectRunResult Run(TargetCallStack callStack)
        {
            Exception error = null;

            var specialLogger = callStack.HeadLogger;

            var args = new BuildEventArgs(this, Stopwatch.StartNew());

            try
            {
                OnBuildStarted(this, args);
                specialLogger.OnBuildLoggingStarted(this, args);

                // output build file that we're running
                specialLogger.Log(Level.Info, "Buildfile: {0}", BuildFileUri);

                // output current target framework in build log
                specialLogger.Log(Level.Info, "Target framework: {0}", TargetFramework != null
                    ? TargetFramework.Description : "None");

                // write verbose project information after Initialize to make 
                // sure properties are correctly initialized
                specialLogger.Log(Level.Verbose, "Base Directory: {0}.", BaseDirectory);

                // execute the project
                Execute(callStack);

                // signal build success
                return new ProjectRunResult() { Success = true };
            }
            catch (BuildException e)
            {
                // store exception in error variable in order to include it 
                // in the BuildFinished event.
                error = e;

                // log exception details to log4net
                logger.Error("Build failed.", e);

                // signal build failure
                return new ProjectRunResult() { Exception = e };
            }
            catch (Exception e)
            {
                // store exception in error variable in order to include it 
                // in the BuildFinished event.
                error = e;

                // log exception details to log4net
                logger.Fatal("Build failed.", e);

                // signal build failure
                return new ProjectRunResult() { Exception = e };
            }
            finally
            {
                string endTarget;

                if (error == null)
                {
                    endTarget = this.GetGlobalProperty(NAntPropertyOnSuccess);
                }
                else
                {
                    endTarget = GetGlobalProperty(NAntPropertyOnFailure);
                }

                if (!String.IsNullOrEmpty(endTarget))
                {
                    // executing the target identified by the 'nant.onsuccess' 
                    // or 'nant.onfailure' properties should not affect the 
                    // build outcome
                    CallTask callTask = new CallTask();
                    callTask.Parent = this;
                    callTask.Project = this;
                    callTask.CallStack = callStack;
                    callTask.NamespaceManager = NamespaceManager;
                    callTask.Verbose = Verbose;
                    callTask.FailOnError = false;
                    callTask.TargetName = endTarget;
                    callTask.Execute();
                }

                // fire BuildFinished event with details of build outcome
                args.Stopwatch.Start();
                BuildEventArgs buildFinishedArgs = new BuildEventArgs(this, args.Stopwatch);

                buildFinishedArgs.Exception = error;
                OnBuildFinished(this, buildFinishedArgs);
                specialLogger.OnBuildLoggingStarted(this, buildFinishedArgs);
            }
        }

        /// <summary>
        /// Creates the <see cref="DataTypeBase"/> instance from the passed XML node.
        /// </summary>
        /// <param name="elementNode">The element XML node.</param>
        /// <param name="targetCallStack">The current target call stack.  Needed for accessing thread and target properties.</param>
        /// <returns>The created instance.</returns>
        public DataTypeBase CreateDataTypeBase(XmlNode elementNode, TargetCallStack targetCallStack)
        {
            DataTypeBase type = TypeFactory.CreateDataType(elementNode, this, targetCallStack);

            type.Project = this;
            type.CallStack = targetCallStack;
            type.Parent = this;
            type.NamespaceManager = NamespaceManager;
            type.Initialize(elementNode);
            return type;
        }

        /// <summary>
        /// Creates a new <see ref="Task" /> from the given <see cref="XmlNode" />.
        /// </summary>
        /// <param name="taskNode">The <see cref="Task" /> definition.</param>
        /// <param name="callStack">The current call stack for this task</param>
        /// <returns>The new <see cref="Task" /> instance.</returns>
        public Task CreateTask(XmlNode taskNode, TargetCallStack callStack)
        {
            return CreateTask(taskNode, null, callStack);
        }

        /// <summary>
        /// Creates a new <see cref="Task" /> from the given <see cref="XmlNode" /> 
        /// within a <see cref="Target" />.
        /// </summary>
        /// <param name="taskNode">The <see cref="Task" /> definition.</param>
        /// <param name="target">The owner <see cref="Target" />.</param>
        /// <param name="callStack">The current call stack for this task</param>
        /// <returns>The new <see cref="Task" /> instance.</returns>
        public Task CreateTask(XmlNode taskNode, Target target, TargetCallStack callStack)
        {
            Task task = TypeFactory.CreateTask(taskNode, this, callStack);

            task.Parent = target;
            task.NamespaceManager = NamespaceManager;

            task.Initialize(taskNode);
            return task;
        }

        /// <summary>
        /// Combines the specified path with the <see cref="BaseDirectory"/> of 
        /// the <see cref="Project" /> to form a full path to file or directory.
        /// </summary>
        /// <param name="path">The relative or absolute path.</param>
        /// <returns>
        /// A rooted path, or the <see cref="BaseDirectory" /> of the <see cref="Project" /> 
        /// if the <paramref name="path" /> parameter is a null reference.
        /// </returns>
        public string GetFullPath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return BaseDirectory;
            }

            // check whether path is a file URI
            try
            {
                Uri uri = new Uri(path);
                if (uri.IsFile)
                {
                    path = uri.LocalPath;
                }
                else
                {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1061"),
                        path, uri.Scheme), Location.UnknownLocation);
                }
            }
            catch
            {
                // ignore exception and treat path as normal path
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(BaseDirectory, path));
            }

            return path;
        }

        /// <summary>
        /// Creates the default <see cref="IBuildLogger" /> and attaches it to
        /// the <see cref="Project" />.
        /// </summary>
        public void CreateDefaultLogger()
        {
            IBuildLogger buildLogger = new DefaultLogger();

            // hook up to build events
            BuildStarted += new BuildEventHandler(buildLogger.BuildStarted);
            BuildFinished += new BuildEventHandler(buildLogger.BuildFinished);
            BuildLoggingStarted += new BuildEventHandler(buildLogger.BuildLoggingStarted);
            BuildLoggingFinished += new BuildEventHandler(buildLogger.BuildLoggingFinished);
            TargetStarted += new TargetBuildEventHandler(buildLogger.TargetStarted);
            TargetFinished += new TargetBuildEventHandler(buildLogger.TargetFinished);
            TaskStarted += new TaskBuildEventHandler(buildLogger.TaskStarted);
            TaskFinished += new TaskBuildEventHandler(buildLogger.TaskFinished);
            TargetLoggingStarted += new TargetBuildEventHandler(buildLogger.TargetLoggingStarted);
            TargetLoggingFinished += new TargetBuildEventHandler(buildLogger.TargetLoggingFinished);
            TaskLoggingStarted += new TaskBuildEventHandler(buildLogger.TaskLoggingStarted);
            TaskLoggingFinished += new TaskBuildEventHandler(buildLogger.TaskLoggingFinished);
            MessageLogged += new BuildEventHandler(buildLogger.MessageLogged);

            // set threshold of logger equal to threshold of the project
            buildLogger.Threshold = Threshold;

            // add default logger to list of build listeners
            BuildListeners.Add(buildLogger);
        }

        /// <summary>
        /// Increases the <see cref="IndentationLevel" /> of the <see cref="Project" />.
        /// </summary>
        public void Indent()
        {
            _indentationLevel++;
        }
        /// <summary>
        /// Decreases the <see cref="IndentationLevel" /> of the <see cref="Project" />.
        /// </summary>
        public void Unindent()
        {
            _indentationLevel--;
        }

        /// <summary>
        /// Detaches the currently attached <see cref="IBuildListener" /> instances
        /// from the <see cref="Project" />.
        /// </summary>
        public void DetachBuildListeners()
        {
            foreach (IBuildListener listener in BuildListeners)
            {
                BuildStarted -= new BuildEventHandler(listener.BuildStarted);
                BuildFinished -= new BuildEventHandler(listener.BuildFinished);
                BuildLoggingStarted -= new BuildEventHandler(listener.BuildLoggingStarted);
                BuildLoggingFinished -= new BuildEventHandler(listener.BuildLoggingFinished);
                TargetStarted -= new TargetBuildEventHandler(listener.TargetStarted);
                TargetFinished -= new TargetBuildEventHandler(listener.TargetFinished);
                TaskStarted -= new TaskBuildEventHandler(listener.TaskStarted);
                TaskFinished -= new TaskBuildEventHandler(listener.TaskFinished);
                MessageLogged -= new BuildEventHandler(listener.MessageLogged);
                TargetLoggingStarted -= new TargetBuildEventHandler(listener.TargetLoggingStarted);
                TargetLoggingFinished -= new TargetBuildEventHandler(listener.TargetLoggingFinished);
                TaskLoggingStarted -= new TaskBuildEventHandler(listener.TaskLoggingStarted);
                TaskLoggingFinished -= new TaskBuildEventHandler(listener.TaskLoggingFinished);

                IBuildLogger buildLogger = listener as IBuildLogger;

                if (buildLogger != null)
                {
                    buildLogger.Flush();
                }
            }

            BuildListeners.Clear();
        }

        /// <summary>
        /// Attaches the specified build listeners to the <see cref="Project" />.
        /// </summary>
        /// <param name="listeners">The <see cref="IBuildListener" /> instances to attach to the <see cref="Project" />.</param>
        /// <remarks>
        /// The currently attached <see cref="IBuildListener" /> instances will 
        /// be detached before the new <see cref="IBuildListener" /> instances 
        /// are attached.
        /// </remarks>
        public void AttachBuildListeners(BuildListenerCollection listeners)
        {
            // detach currently attached build listeners
            DetachBuildListeners();
            foreach (IBuildListener listener in listeners)
            {
                // hook up listener to project build events
                BuildStarted += new BuildEventHandler(listener.BuildStarted);
                BuildFinished += new BuildEventHandler(listener.BuildFinished);
                BuildLoggingStarted += new BuildEventHandler(listener.BuildLoggingStarted);
                BuildLoggingFinished += new BuildEventHandler(listener.BuildLoggingFinished);
                TargetStarted += new TargetBuildEventHandler(listener.TargetStarted);
                TargetFinished += new TargetBuildEventHandler(listener.TargetFinished);
                TaskStarted += new TaskBuildEventHandler(listener.TaskStarted);
                TaskFinished += new TaskBuildEventHandler(listener.TaskFinished);
                MessageLogged += new BuildEventHandler(listener.MessageLogged);
                TargetLoggingStarted += new TargetBuildEventHandler(listener.TargetLoggingStarted);
                TargetLoggingFinished += new TargetBuildEventHandler(listener.TargetLoggingFinished);
                TaskLoggingStarted += new TaskBuildEventHandler(listener.TaskLoggingStarted);
                TaskLoggingFinished += new TaskBuildEventHandler(listener.TaskLoggingFinished);

                // add listener to project listener list
                BuildListeners.Add(listener);
            }
        }

        /// <summary>
        /// Inits stuff:
        ///     <para>TypeFactory: Calls Initialize and AddProject </para>
        ///     <para>Log.IndentSize set to 12</para>
        ///     <para>Project properties are initialized ("nant.* stuff set")</para>
        ///     <list type="nant.items">
        ///         <listheader>NAnt Props:</listheader>
        ///         <item>nant.filename</item>
        ///         <item>nant.version</item>
        ///         <item>nant.location</item>
        ///         <item>nant.project.name</item>
        ///         <item>nant.project.buildfile (if doc has baseuri)</item>
        ///         <item>nant.project.basedir</item>
        ///         <item>nant.project.default = defaultTarget</item>
        ///     </list>
        /// </summary>
        /// <param name="doc">An <see cref="XmlDocument" /> representing the project definition.</param>
        /// <param name="threshold">The project message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        /// <param name="optimization">Optimization flags.</param>
        /// <exception cref="ArgumentNullException"><paramref name="doc" /> is <see langword="null" />.</exception>
        private void CtorHelper(XmlDocument doc, Level threshold, int indentLevel, Optimizations optimization)
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            string newBaseDir = null;

            this.GlobalProperties = new PropertyDictionary(this, PropertyScope.Global);
            this.RootTargetCallStack = new TargetCallStack(this);
            this.RootTargetCallStack.PushRoot();

            // set the project definition
            _doc = doc;

            // set the indentation size of the build output
            _indentationSize = 4;

            // set the indentation level of the build output
            _indentationLevel = indentLevel;

            // set the project message threshold
            Threshold = threshold;

            // add default logger
            CreateDefaultLogger();

            // configure platform properties
            ConfigurePlatformProperties();

            // fill the namespace manager up, so we can make qualified xpath 
            // expressions
            if (String.IsNullOrEmpty(doc.DocumentElement.NamespaceURI))
            {
                string defURI;

                XmlAttribute nantNS = doc.DocumentElement.Attributes["xmlns", "nant"];
                if (nantNS == null)
                {
                    defURI = @"http://none";
                }
                else
                {
                    defURI = nantNS.Value;
                }

                XmlAttribute attr = doc.CreateAttribute("xmlns");
                attr.Value = defURI;
                doc.DocumentElement.Attributes.Append(attr);
            }

            NamespaceManager.AddNamespace("nant", doc.DocumentElement.NamespaceURI);

            // check to make sure that the root element in named correctly
            if (!doc.DocumentElement.LocalName.Equals(RootXml))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1059"), doc.BaseURI, RootXml));
            }

            // get project attributes
            if (doc.DocumentElement.HasAttribute(ProjectNameAttribute))
            {
                _projectName = doc.DocumentElement.GetAttribute(ProjectNameAttribute);
            }

            if (doc.DocumentElement.HasAttribute(ProjectBaseDirAttribute))
            {
                newBaseDir = doc.DocumentElement.GetAttribute(ProjectBaseDirAttribute);
            }

            if (doc.DocumentElement.HasAttribute(ProjectDefaultAttribte))
            {
                _defaultTargetName = doc.DocumentElement.GetAttribute(ProjectDefaultAttribte);
            }

            // give the project a meaningful base directory
            if (String.IsNullOrEmpty(newBaseDir))
            {
                if (!String.IsNullOrEmpty(BuildFileLocalName))
                {
                    newBaseDir = Path.GetDirectoryName(BuildFileLocalName);
                }
                else
                {
                    newBaseDir = Environment.CurrentDirectory;
                }
            }
            else
            {
                // if basedir attribute is set to a relative path, then resolve 
                // it relative to the build file path
                if (!String.IsNullOrEmpty(BuildFileLocalName) && !Path.IsPathRooted(newBaseDir))
                {
                    newBaseDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(BuildFileLocalName), newBaseDir));
                }
            }

            newBaseDir = Path.GetFullPath(newBaseDir);

            // base directory must be rooted.
            BaseDirectory = newBaseDir;

            // load project-level extensions assemblies
            bool scan = ((optimization & Optimizations.SkipAutomaticDiscovery) == 0);
            TypeFactory.AddProject(this, scan, this.RootTargetCallStack);

            if ((optimization & Optimizations.SkipFrameworkConfiguration) == 0)
            {
                // load settings out of settings file
                ProjectSettingsLoader psl = new ProjectSettingsLoader(this);
                psl.ProcessSettings();
            }
        }

        /// <summary>
        /// This method is only meant to be used by the <see cref="Project"/> 
        /// class and <see cref="T:NAnt.Core.Tasks.IncludeTask"/>.
        /// </summary>
        internal void InitializeProjectDocument(XmlDocument doc, TargetCallStack callStack)
        {
            if (callStack == null)
            {
                throw new ArgumentNullException("callStack");
            }

            // load line and column number information into position map
            LocationMap.Add(doc);

            // initialize targets first
            foreach (XmlNode childNode in doc.DocumentElement.ChildNodes)
            {
                // skip non-nant namespace elements and special elements like 
                // comments, pis, text, etc.
                if (childNode.LocalName.Equals(TargetXml) && childNode.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant")))
                {
                    Target target = new Target();

                    target.Project = this;
                    target.CallStack = callStack;
                    target.Parent = this;
                    target.NamespaceManager = NamespaceManager;
                    target.Initialize(childNode);
                    Targets.Add(target);
                }
            }
            // initialize datatypes and execute global tasks
            foreach (XmlNode childNode in doc.DocumentElement.ChildNodes)
            {
                // skip targets that were handled above, skip non-nant namespace 
                // elements and special elements like comments, pis, text, etc.
                if (!(childNode.NodeType == XmlNodeType.Element) || !childNode.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant")) || childNode.LocalName.Equals(TargetXml))
                {
                    continue;
                }

                if (TypeFactory.TaskBuilders.Contains(childNode.Name))
                {
                    // create task instance
                    Task task = CreateTask(childNode, callStack);
                    task.Parent = this;
                    // execute task
                    task.Execute();
                }
                else if (TypeFactory.DataTypeBuilders.Contains(childNode.Name))
                {
                    // we are an datatype declaration
                    DataTypeBase dataType = CreateDataTypeBase(childNode, callStack);

                    (this as ITargetLogger).Log(Level.Debug, "Adding a {0} reference with id '{1}'.", childNode.Name, dataType.ID);
                    if (!DataTypeReferences.Contains(dataType.ID))
                    {
                        DataTypeReferences.Add(dataType.ID, dataType);
                    }
                    else
                    {
                        DataTypeReferences[dataType.ID] = dataType; // overwrite with the new reference.
                    }
                }
                else
                {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1071"), childNode.Name),
                        LocationMap.GetLocation(childNode));
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="XmlDocument" /> based on the project 
        /// definition.
        /// </summary>
        /// <param name="uriOrFilePath">
        /// <para>The full path to the build file.</para>
        /// <para>This can be of any form that <see cref="M:XmlDocument.Load(string)" /> accepts.</para>
        /// </param>
        /// <returns>
        /// An <see cref="XmlDocument" /> based on the specified project 
        /// definition.
        /// </returns>
        private XmlDocument LoadBuildFile(string uriOrFilePath)
        {
            string path = uriOrFilePath;

            //if the source is not a valid uri, pass it thru.
            //if the source is a file uri, pass the localpath of it thru.
            try
            {
                Uri testURI = new Uri(uriOrFilePath);

                if (testURI.IsFile)
                {
                    path = testURI.LocalPath;
                }
            }
            catch (Exception ex)
            {
                logger.Debug("Error creating URI in project constructor. Moving on... ", ex);
            }
            finally
            {
                if (path == null)
                {
                    path = uriOrFilePath;
                }
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(path);
            }
            catch (XmlException ex)
            {
                Location location = new Location(path, ex.LineNumber, ex.LinePosition);
                throw new BuildException("Error loading buildfile.", location, ex);
            }
            catch (Exception ex)
            {
                Location location = new Location(path);
                throw new BuildException("Error loading buildfile.", location, ex);
            }
            return doc;
        }

        /// <summary>
        /// Configures the platform properties for the current platform.
        /// </summary>
        /// <exception cref="BuildException">NAnt does not support the current platform.</exception>
        private void ConfigurePlatformProperties()
        {
            this.SetGlobalProperty(NAntPlatformName, PlatformName, true);

            switch (PlatformName)
            {
                case "win32":
                    this.SetGlobalProperty(NAntPlatform + ".unix", "false", true);
                    this.SetGlobalProperty(NAntPlatform + "." + PlatformName, "true", true);
                    break;
                case "unix":
                    this.SetGlobalProperty(NAntPlatform + "." + PlatformName, "true", true);
                    this.SetGlobalProperty(NAntPlatform + ".win32", "false", true);
                    break;
                default:
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1060"),
                        Environment.OSVersion.Platform.ToString(CultureInfo.InvariantCulture),
                        (int)Environment.OSVersion.Platform));
            }
        }

        /// <summary>
        /// Updates dependent properties when the <see cref="TargetFramework" /> 
        /// is set.
        /// </summary>
        private void UpdateTargetFrameworkProperties()
        {
            this.SetGlobalProperty("nant.settings.currentframework", TargetFramework.Name);
            this.SetGlobalProperty("nant.settings.currentframework.version", TargetFramework.Version.ToString());
            this.SetGlobalProperty("nant.settings.currentframework.description", TargetFramework.Description);
            this.SetGlobalProperty("nant.settings.currentframework.frameworkdirectory", TargetFramework.FrameworkDirectory.FullName);
            if (TargetFramework.SdkDirectory != null)
            {
                this.SetGlobalProperty("nant.settings.currentframework.sdkdirectory", TargetFramework.SdkDirectory.FullName);
            }
            else
            {
                this.SetGlobalProperty("nant.settings.currentframework.sdkdirectory", "");
            }

            this.SetGlobalProperty("nant.settings.currentframework.frameworkassemblydirectory", TargetFramework.FrameworkAssemblyDirectory.FullName);
            this.SetGlobalProperty("nant.settings.currentframework.runtimeengine", TargetFramework.RuntimeEngine);
        }

        private XmlNode GetConfigurationNode()
        {
            XmlNode configurationNode = ConfigurationSettings.GetConfig("nant") as XmlNode;
            if (configurationNode == null)
            {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "The NAnt configuration settings in file '{0}' could"
                    + " not be loaded.  Please ensure this file is available"
                    + " and contains a 'nant' settings node.",
                    AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));
            }
            return configurationNode;
        }

        /// <summary>
        /// Topologically sorts a set of targets.
        /// </summary>
        /// <param name="root">The name of the root target. The sort is created in such a way that the sequence of targets up to the root target is the minimum possible such sequence. Must not be <see langword="null" />.</param>
        /// <param name="targets">A collection of <see cref="Target" /> instances.</param>
        /// <returns>
        /// A collection of <see cref="Target" /> instances in sorted order.
        /// </returns>
        /// <exception cref="BuildException">There is a cyclic dependecy among the targets, or a named target does not exist.</exception>
        public TargetCollection TopologicalTargetSort(string root, TargetCollection targets)
        {
            TargetCollection executeTargets = new TargetCollection();
            Hashtable state = new Hashtable();
            Stack visiting = new Stack();

            // We first run a DFS based sort using the root as the starting node.
            // This creates the minimum sequence of Targets to the root node.
            // We then do a sort on any remaining unVISITED targets.
            // This is unnecessary for doing our build, but it catches
            // circular dependencies or missing Targets on the entire
            // dependency tree, not just on the Targets that depend on the
            // build Target.
            TopologicalTargetSort(root, targets, state, visiting, executeTargets);
            (this as ITargetLogger).Log(Level.Debug, "Build sequence for target `{0}' is {1}", root, executeTargets);
            foreach (Target target in targets)
            {
                string st = (string)state[target.Name];

                if (st == null)
                {
                    TopologicalTargetSort(target.Name, targets, state, visiting, executeTargets);
                }
                else if (st == Project.Visiting)
                {
                    throw new Exception("Unexpected node in visiting state: " + target.Name);
                }
            }

            (this as ITargetLogger).Log(Level.Debug, "Complete build sequence is {0}", executeTargets);
            return executeTargets;
        }

        /// <summary>
        /// <para>
        /// Performs a single step in a recursive depth-first-search traversal 
        /// of the target dependency tree.
        /// </para>
        /// <para>
        /// The current target is first set to the "visiting" state, and pushed
        /// onto the "visiting" stack.
        /// </para>
        /// <para>
        /// An exception is then thrown if any child of the current node is in 
        /// the visiting state, as that implies a circular dependency. The 
        /// exception contains details of the cycle, using elements of the 
        /// "visiting" stack.
        /// </para>
        /// <para>
        /// If any child has not already been "visited", this method is called
        /// recursively on it.
        /// </para>
        /// <para>
        /// The current target is then added to the ordered list of targets. 
        /// Note that this is performed after the children have been visited in 
        /// order to get the correct order. The current target is set to the 
        /// "visited" state.
        /// </para>
        /// <para>
        /// By the time this method returns, the ordered list contains the 
        /// sequence of targets up to and including the current target.
        /// </para>
        /// </summary>
        /// <param name="root">The current target to inspect. Must not be <see langword="null" />.</param>
        /// <param name="targets">A collection of <see cref="Target" /> instances.</param>
        /// <param name="state">A mapping from targets to states The states in question are "VISITING" and "VISITED". Must not be <see langword="null" />.</param>
        /// <param name="visiting">A stack of targets which are currently being visited. Must not be <see langword="null" />.</param>
        /// <param name="executeTargets">The list to add target names to. This will end up containing the complete list of depenencies in dependency order. Must not be <see langword="null" />.</param>
        /// <exception cref="BuildException">
        ///   <para>A non-existent target is specified</para>
        ///   <para>-or-</para>
        ///   <para>A circular dependency is detected.</para>
        /// </exception>
        private void TopologicalTargetSort(string root, TargetCollection targets, Hashtable state, Stack visiting, TargetCollection executeTargets)
        {
            state[root] = Project.Visiting;
            visiting.Push(root);

            Target target = (Target)targets.Find(root);
            if (target == null)
            {
                // check if there's a wildcard target defined
                target = (Target)targets.Find(WildTarget);
                if (target != null)
                {
                    // if a wildcard target exists, then treat the wildcard
                    // target as the requested target
                    target = target.Clone();
                    target.Name = root;
                }
                else
                {
                    StringBuilder sb = new StringBuilder("Target '");
                    sb.Append(root);
                    sb.Append("' does not exist in this project.");

                    visiting.Pop();
                    if (visiting.Count > 0)
                    {
                        string parent = (string)visiting.Peek();
                        sb.Append(" ");
                        sb.Append("It is used from target '");
                        sb.Append(parent);
                        sb.Append("'.");
                    }

                    throw new BuildException(sb.ToString());
                }
            }

            foreach (string dependency in target.Dependencies)
            {
                string m = (string)state[dependency];

                if (m == null)
                {
                    // Not been visited
                    TopologicalTargetSort(dependency, targets, state, visiting, executeTargets);
                }
                else if (m == Project.Visiting)
                {
                    // Currently visiting this node, so have a cycle
                    throw CreateCircularException(dependency, visiting);
                }
            }

            string p = (string)visiting.Pop();

            if (root != p)
            {
                throw new Exception("Unexpected internal error: expected to pop " + root + " but got " + p);
            }

            state[root] = Project.Visited;
            executeTargets.Add(target);
        }

        /// <summary>
        /// Builds an appropriate exception detailing a specified circular
        /// dependency.
        /// </summary>
        /// <param name="end">The dependency to stop at. Must not be <see langword="null" />.</param>
        /// <param name="stack">A stack of dependencies. Must not be <see langword="null" />.</param>
        /// <returns>
        /// A <see cref="BuildException" /> detailing the specified circular 
        /// dependency.
        /// </returns>
        private static BuildException CreateCircularException(string end, Stack stack)
        {
            StringBuilder sb = new StringBuilder("Circular dependency: ");
            sb.Append(end);

            string c;

            do
            {
                c = (string)stack.Pop();
                sb.Append(" <- ");
                sb.Append(c);
            } while (!c.Equals(end));

            return new BuildException(sb.ToString());
        }

        public struct ProjectRunResult
        {
            public Exception Exception { get; set; }

            public Boolean Success { get; set; }
        }
    }

    /// <summary>
    /// Allow the project construction to be optimized.
    /// </summary>
    /// <remarks>
    /// Use this with care!
    /// </remarks>
    internal enum Optimizations
    {
        /// <summary>
        /// Do not perform any optimizations.
        /// </summary>
        None = 0,

        /// <summary>
        /// The project base directory must not be automatically scanned 
        /// for extension assemblies.
        /// </summary>
        SkipAutomaticDiscovery = 1,

        /// <summary>
        /// Do not scan the project configuration for frameworks, and 
        /// do not configure the runtime and target framework.
        /// </summary>
        SkipFrameworkConfiguration = 2,
    }
}
