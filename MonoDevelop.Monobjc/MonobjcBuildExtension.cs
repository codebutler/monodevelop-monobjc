//
// MonobjcBuildExtension.cs
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
using System.Reflection;
using System.Text;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using PropertyList;
using Mono.Unix;

namespace MonoDevelop.Monobjc
{
	public class MonobjcBuildExtension : ProjectServiceExtension
	{
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			MonobjcProject proj = item as MonobjcProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return base.Build(monitor, item, configuration);
			
			BuildResult result = base.Build(monitor, item, configuration);
			if (result.ErrorCount > 0)
				return result;
			
			var conf = (MonobjcProjectConfiguration) proj.GetConfiguration(configuration);
			
			// Create directories
			monitor.BeginTask("Creating application bundle", 0);
			foreach (var path in new [] { conf.ResourcesDirectory, conf.MacOSDirectory }) {
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);
			}
			monitor.EndTask();
			
			string exeName = Path.GetFileNameWithoutExtension(conf.CompiledOutputName);
			
			// Write Info.plist into 'Contents' directory
			
			monitor.BeginTask("Updating application manifest", 0);
			var doc = new PlistDocument();
			var docRoot = new PlistDictionary();			
  			docRoot["CFBundleExecutable"]            = exeName;
			docRoot["CFBundleInfoDictionaryVersion"] = "6.0";
			docRoot["CFBundlePackageType"]           = "APPL";
			docRoot["CFBundleName"]                  = proj.BundleDisplayName ?? proj.Name;
			docRoot["CFBundleDisplayName"]           = proj.BundleDisplayName ?? proj.Name;
			docRoot["CFBundleIdentifier"]            = proj.BundleIdentifier ?? String.Format("com.yourcompany.{0}", proj.Name);
			docRoot["CFBundleVersion"]               = proj.BundleVersion ?? "1.0";
			docRoot["CFBundleDevelopmentRegion"]     = proj.BundleDevelopmentRegion ?? "English";
			
			FilePath icon = proj.BundleIcon.ToRelative (proj.BaseDirectory);
			if (!(icon.IsNullOrEmpty || icon.ToString () == "."))
				docRoot["CFBundleIconFile"] = icon.FileName;
			
			if (!String.IsNullOrEmpty (proj.MainNibFile.ToString ()))
				docRoot["NSMainNibFile"] = Path.GetFileNameWithoutExtension(proj.MainNibFile.ToString ());
			
			doc.Root = docRoot;
			
			var plistOut = conf.ContentsDirectory.Combine("Info.plist");
			using (XmlTextWriter writer = new XmlTextWriter(plistOut, Encoding.UTF8)) {
				doc.Write(writer);
			}
			monitor.EndTask();
			
			// Copy project binary into 'Resources' directory
			monitor.BeginTask("Copying application binary", 0);
			File.Copy(conf.CompiledOutputName, conf.ResourcesDirectory.Combine(conf.CompiledOutputName.FileName), true);
			if (File.Exists(conf.CompiledOutputName + ".mdb"))
				File.Copy(conf.CompiledOutputName + ".mdb", conf.ResourcesDirectory.Combine(conf.CompiledOutputName.FileName + ".mdb"), true);			
			monitor.EndTask();
			
			// Copy references into 'Resources' directory
			var references = BuildUtils.GetReferencesFilePairs(proj, configuration);
			foreach (var pair in references) {
				pair.EnsureOutputDirectory();
				monitor.Log.WriteLine("Copying '{0}' to '{1}'", pair.Input, pair.Output);
				File.Copy(pair.Input, pair.Output, true);
				monitor.Step(1);
			}
			monitor.EndTask();
						
			// Write launcher script into 'MacOS' directory
			monitor.BeginTask("Writing launcher script", 0);
			string scriptPath = conf.MacOSDirectory.Combine(exeName);
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LaunchScript.sh");
			using (StreamReader reader = new StreamReader(stream)) {
				using (StreamWriter writer = new StreamWriter(scriptPath)) {
					writer.Write(reader.ReadToEnd());
				}
			}
			
			// Make executable (755)
			new UnixFileInfo(scriptPath).FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute | 
				FileAccessPermissions.GroupRead | FileAccessPermissions.GroupExecute |
				FileAccessPermissions.OtherRead | FileAccessPermissions.OtherExecute;
			
			monitor.EndTask();
			
			return result;
		}
		
		protected override bool GetNeedsBuilding (SolutionEntityItem item, ConfigurationSelector configuration)
		{
			if (base.GetNeedsBuilding (item, configuration))
				return true;
			
			var proj = item as MonobjcProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return false;
			
			var conf = (MonobjcProjectConfiguration)proj.GetConfiguration(configuration);
			
			if (!Directory.Exists(conf.AppDirectory))
				return true;
			
			// Info.plist
			if (new FilePair(proj.FileName, conf.AppDirectory.Combine("Contents").Combine("Info.plist")).NeedsBuilding())
				return true;
			
			// Launcher script
			if (!File.Exists(conf.LauncherScript))
				return true;
			
			// Compiled binary
			if (new FilePair(conf.CompiledOutputName, conf.ResourcesDirectory.Combine(conf.CompiledOutputName.FileName)).NeedsBuilding())
				return true;
			
			// References
			if (BuildUtils.GetReferencesFilePairs(proj, configuration).Where(f => f.NeedsBuilding()).Any())
				return true;		
			
			//Interface Builder files
			if (BuildUtils.GetIBFilePairs (proj.Files, conf.ResourcesDirectory).Where (f => f.NeedsBuilding()).Any ())
		        return true;
			
			//Content files
			if (BuildUtils.GetContentFilePairs (proj.Files, conf.AppDirectory).Where (f => f.NeedsBuilding()).Any ())
				return true;
			
			if (!File.Exists(conf.AppDirectory.Combine("Contents").Combine("Info.plist")))
				return true;
			
			return false;
		}
		
		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			base.Clean(monitor, item, configuration);
			
			var proj = item as MonobjcProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return;
			
			var conf = (MonobjcProjectConfiguration)proj.GetConfiguration(configuration);
			
			if (Directory.Exists(conf.AppDirectory))
				Directory.Delete(conf.AppDirectory, true);
		}

		protected override BuildResult Compile (IProgressMonitor monitor, SolutionEntityItem item, BuildData buildData)
		{
			MonobjcProject proj = item as MonobjcProject;
			if (proj == null || proj.CompileTarget != CompileTarget.Exe)
				return base.Compile(monitor, item, buildData);

			BuildResult result = base.Compile(monitor, item, buildData);
			if (result.ErrorCount > 0)
				return result;
			
			BuildUtils.CompileXibs(monitor, buildData, result);
			
			BuildUtils.CopyContentFiles(monitor, buildData);
			
			return result;
		}
	}
}
