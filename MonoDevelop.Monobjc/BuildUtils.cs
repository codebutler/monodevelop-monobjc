//
// BuildUtils.cs
//
// Author:
//       Eric Butler <eric@codebutler.com>
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Monobjc
{
	public static class BuildUtils
	{
		public static IEnumerable<FilePair> GetIBFilePairs (IEnumerable<ProjectFile> allItems, string outputRoot)
		{
			return allItems
				.OfType<ProjectFile>()
				.Where(pf => pf.BuildAction == BuildAction.Page && pf.FilePath.Extension == ".xib")
				.Select(pf => {
					string[] splits = ((string)pf.RelativePath).Split(Path.DirectorySeparatorChar);
					FilePath name = splits.Last();
					if (splits.Length > 1 && splits[0].EndsWith(".lproj"))
						name = new FilePath(splits[0]).Combine(name);
					return new FilePair(pf.FilePath, name.ChangeExtension(".nib").ToAbsolute(outputRoot));
				}
			);
		}

		public static IEnumerable<FilePair> GetContentFilePairs (IEnumerable<ProjectFile> allItems, string outputRoot)
		{
			return allItems
				.OfType<ProjectFile>()
				.Where(pf => pf.BuildAction == BuildAction.Content)
				.Select(pf => new FilePair(pf.FilePath, pf.RelativePath.ToAbsolute(outputRoot)));
		}

		public static IEnumerable<FilePair> GetReferencesFilePairs (MonobjcProject proj, ConfigurationSelector configuration)
		{
			var conf = (MonobjcProjectConfiguration)proj.GetConfiguration(configuration);
			return proj.References
				.Where(r => r.LocalCopy)
				.Where(r => r.ReferenceType == ReferenceType.Assembly || r.ReferenceType == ReferenceType.Project)
				.Select(r => {
					var filePath = new FilePath(r.GetReferencedFileNames(configuration)[0]);
					var destFileName = conf.ResourcesDirectory.Combine(filePath.FileName);
					return new FilePair(filePath, destFileName);
				}
			);
		}

		public static void CompileXibs (IProgressMonitor monitor, BuildData buildData, BuildResult result)
		{
			var cfg = (MonobjcProjectConfiguration)buildData.Configuration;
			
			string appDir = ((MonobjcProjectConfiguration)buildData.Configuration).ResourcesDirectory;
			
			var ibfiles = GetIBFilePairs(buildData.Items.OfType<ProjectFile>(), appDir).Where(f => f.NeedsBuilding()).ToList();
			
			if (ibfiles.Count > 0) {
				monitor.BeginTask(GettextCatalog.GetString("Compiling interface definitions"), 0);
				foreach (var file in ibfiles) {
					file.EnsureOutputDirectory();
					var psi = new ProcessStartInfo("ibtool", String.Format("\"{0}\" --compile \"{1}\"", file.Input, file.Output));
					monitor.Log.WriteLine(psi.FileName + " " + psi.Arguments);
					psi.WorkingDirectory = cfg.OutputDirectory;
					string errorOutput;
					int code = BuildUtils.ExecuteCommand(monitor, psi, out errorOutput);
					if (code != 0) {
						//FIXME: parse the plist that ibtool returns
						result.AddError(null, 0, 0, null, "ibtool returned error code " + code);
					}
				}
				monitor.EndTask();
			}
		}

		public static void CopyContentFiles (MonobjcProject proj, IProgressMonitor monitor, BuildData buildData, BuildResult result)
		{
			var cfg = (MonobjcProjectConfiguration)buildData.Configuration;
			
			string resDir = ((MonobjcProjectConfiguration)buildData.Configuration).ResourcesDirectory;
			
			// Copy 'content' files into 'Resources' directory
			var contentFiles = BuildUtils.GetContentFilePairs(buildData.Items.OfType<ProjectFile>(), resDir)
				.Where(f => f.NeedsBuilding())
				.ToList();
			
			if (!proj.BundleIcon.IsNullOrEmpty) {
				FilePair icon = new FilePair(proj.BundleIcon, cfg.ResourcesDirectory.Combine(proj.BundleIcon.FileName));
				if (!File.Exists(proj.BundleIcon)) {
					result.AddError(null, 0, 0, null, String.Format("Application icon '{0}' is missing.", proj.BundleIcon));
					return;
				} else {
					contentFiles.Add(icon);
				}
			}
			
			if (contentFiles.Count > 0) {
				monitor.BeginTask(GettextCatalog.GetString("Copying content files"), contentFiles.Count);
				foreach (var file in contentFiles) {
					file.EnsureOutputDirectory();
					monitor.Log.WriteLine(GettextCatalog.GetString("Copying '{0}' to '{1}'", file.Input, file.Output));
					File.Copy(file.Input, file.Output, true);
					monitor.Step(1);
				}
				monitor.EndTask();
			}
		}

		//copied from MoonlightBuildExtension
		static int ExecuteCommand (IProgressMonitor monitor, System.Diagnostics.ProcessStartInfo startInfo, out string errorOutput)
		{
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			
			errorOutput = string.Empty;
			int exitCode = -1;
			
			var swError = new StringWriter();
			var chainedError = new LogTextWriter();
			chainedError.ChainWriter(monitor.Log);
			chainedError.ChainWriter(swError);
			
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor(monitor);
			
			try {
				var p = Runtime.ProcessService.StartProcess(startInfo, monitor.Log, chainedError, null);
				operationMonitor.AddOperation(p); //handles cancellation
				p.WaitForOutput();
				errorOutput = swError.ToString();
				exitCode = p.ExitCode;
				p.Dispose();
				
				if (monitor.IsCancelRequested) {
					monitor.Log.WriteLine(GettextCatalog.GetString("Build cancelled"));
					monitor.ReportError(GettextCatalog.GetString("Build cancelled"), null);
					if (exitCode == 0)
						exitCode = -1;
				}
			} finally {
				chainedError.Close();
				swError.Close();
				operationMonitor.Dispose();
			}
			
			return exitCode;
		}
	}

	public struct FilePair
	{
		public FilePair (FilePath input, FilePath output)
		{
			this.Input = input;
			this.Output = output;
		}
		public FilePath Input, Output;

		public bool NeedsBuilding ()
		{
			return !File.Exists(Output) || File.GetLastWriteTime(Input) > File.GetLastWriteTime(Output);
		}

		public void EnsureOutputDirectory ()
		{
			if (!Directory.Exists(Output.ParentDirectory))
				Directory.CreateDirectory(Output.ParentDirectory);
		}
	}
}
