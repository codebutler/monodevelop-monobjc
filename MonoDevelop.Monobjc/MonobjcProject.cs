//
// MonobjcProject.cs
//
// Author:
//	     Eric Butler <eric@codebutler.com>
//
// Based on MonoDevelop.IPhone
//
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Xml;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Monobjc
{
	public class MonobjcProject : DotNetProject
	{
		#region Properties
		
		[ProjectPathItemProperty ("MainNibFile")]
		string mainNibFile;
		
		[ItemProperty ("BundleDevelopmentRegion")]
		string bundleDevelopmentRegion;
		
		[ItemProperty ("BundleIdentifier")]
		string bundleIdentifier;
		
		[ItemProperty ("BundleVersion")]
		string bundleVersion;
		
		[ItemProperty ("BundleDisplayName")]
		string bundleDisplayName;
		
		[ProjectPathItemProperty ("BundleIcon")]
		string bundleIcon;
		
		public override string ProjectType {
			get { return "Monobjc"; }
		}
		
		public FilePath MainNibFile {
			get { return mainNibFile; }
			set {
				if (value == (FilePath) mainNibFile)
					return;
				NotifyModified ("MainNibFile");
				mainNibFile = value;
			}
		}
		
		public string BundleDevelopmentRegion {
			get { return bundleDevelopmentRegion; }
			set {
				if (value == "English" || value == "")
					value = null;
				if (value == bundleDevelopmentRegion)
					return;
				NotifyModified ("BundleDevelopmentRegion");
				bundleDevelopmentRegion = value;
			}
		}
		
		public string BundleIdentifier {
			get { return bundleIdentifier; }
			set {
				if (value == "")
					value = null;
				if (value == bundleIdentifier)
					return;
				NotifyModified ("BundleIdentifier");
				bundleIdentifier = value;
			}
		}
		
		public string BundleVersion {
			get { return bundleVersion; }
			set {
				if (value == "")
					value = null;
				if (value == bundleVersion)
					return;
				NotifyModified ("BundleVersion");
				bundleVersion = value;
			}
		}
		
		public string BundleDisplayName {
			get { return bundleDisplayName; }
			set {
				if (value == "")
					value = null;
				if (value == bundleDisplayName)
					return;
				NotifyModified ("BundleDisplayName");
				bundleDisplayName = value;
			}
		}
		
		public FilePath BundleIcon {
			get { return bundleIcon; }
			set {
				if (value == (FilePath) bundleIcon)
					return;
				NotifyModified ("BundleIcon");
				bundleIcon = value;
			}
		}
		
		#endregion
		
		public MonobjcProject () : base ()
		{
			
		}
		
		public MonobjcProject (string languageName) : base (languageName)
		{
			
		}
		
		public MonobjcProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			var tags = new [,] { { "ProjectName", info.ProjectName } };
			
			bundleIdentifier = projectOptions.SelectSingleNode("BundleIdentifier").InnerText;
			bundleIdentifier = StringParserService.Parse(bundleIdentifier, tags);
			
			bundleDisplayName = projectOptions.SelectSingleNode("BundleDisplayName").InnerText;
			bundleDisplayName = StringParserService.Parse(bundleDisplayName, tags);
			
			bundleDevelopmentRegion = projectOptions.SelectSingleNode("BundleDevelopmentRegion").InnerText;
			
			bundleVersion = projectOptions.SelectSingleNode("BundleVersion").InnerText;
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new MonobjcProjectConfiguration(name);
			
			if (LanguageBinding != null)
				conf.CompilationParameters = LanguageBinding.CreateCompilationParameters(null);
			
			return conf;
		}

		#region Execution
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration)
		{
			var conf = (MonobjcProjectConfiguration)configuration;
			string bundleName = Path.GetFileNameWithoutExtension(conf.CompiledOutputName);
			var cmd = new MonobjcExecutionCommand(conf.AppDirectory, bundleName, conf.CommandLineParameters, conf.EnvironmentVariables, conf.DebugMode);
			
			// FIXME: Should this be the assembly inside the .app instead?
			cmd.UserAssemblyPaths = GetUserAssemblyPaths(configSel);
			
			return cmd;
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			base.OnExecute (monitor, context, configuration);
		}
		
		#endregion
		
		#region CodeBehind files
		
		public override string GetDefaultBuildAction (string fileName)
		{
			if (Path.GetExtension(fileName) == ".xib")
				return BuildAction.Page;
			return base.GetDefaultBuildAction (fileName);
		}
				
		//based on MoonlightProject
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (Loading) {
				base.OnFileAddedToProject (e);
				return;
			}
			
			if (String.IsNullOrEmpty (MainNibFile) && Path.GetFileName (e.ProjectFile.FilePath) == "MainMenu.xib") {
				MainNibFile = e.ProjectFile.FilePath;
			}
			
			base.OnFileAddedToProject (e);
		}
		
		#endregion
	}
}
