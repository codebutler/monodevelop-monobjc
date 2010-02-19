//
// CodeBehind.cs
//
// Author:
//       Laurent Etiemble <laurent.etiemble@monobjc.net>
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
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Monobjc.Tools.InterfaceBuilder;
using Monobjc.Tools.InterfaceBuilder.Visitors;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Projects;

namespace MonoDevelop.Monobjc
{
	static internal class CodeBehind
	{
		public static void GenerateXibDesignCode (ProjectFile file)
		{
			ProjectFile designerFile = GetDesignerFile (file);
			try {
				// Only generate if needed
				if (designerFile != null && IsMoreRecent (file, designerFile)) {
					CodeBehindWriter writer = CodeBehindWriter.CreateForProject (new NullProgressMonitor (), (DotNetProject)file.Project);
					GenerateXibDesignCode (writer, file, designerFile);
					writer.WriteOpenFiles();
				}
			} catch (Exception ex) {
				LoggingService.LogError (String.Format (CultureInfo.CurrentUICulture, "Cannot generate design code for xib file {0}", file.FilePath), ex);
			}
		}

		// TODO: Refactor
		public static void GenerateXibDesignCode (Project project)
		{
			// Filter out xib files
			CodeBehindWriter writer = CodeBehindWriter.CreateForProject (new NullProgressMonitor (), (DotNetProject)project);
			foreach(ProjectFile file in project.Files.Where (BuildUtils.IsXIBFile)) {
				ProjectFile designerFile = GetDesignerFile (file);
				try {
					// Always generate
					if (designerFile != null) {
						GenerateXibDesignCode(writer, file, designerFile);
					}
				} catch (Exception ex) {
					LoggingService.LogError (String.Format (CultureInfo.CurrentUICulture, "Cannot generate design code for xib file {0}", file.FilePath), ex);
				}
			}
			writer.WriteOpenFiles();
		}
		
		public static BuildResult GenerateXibDesignCode (CodeBehindWriter writer, MonobjcProject project, IEnumerable<ProjectFile> files)
		{
			BuildResult result = null;
			
			// Filter out xib files
			foreach(ProjectFile file in files.Where (BuildUtils.IsXIBFile)) {
				ProjectFile designerFile = GetDesignerFile (file);
				try {
					// Only generate if needed
					if (designerFile != null && IsMoreRecent (file, designerFile)) {
						GenerateXibDesignCode(writer, file, designerFile);
					}
				} catch (Exception ex) {
					if (result == null) {
						result = new BuildResult();
					}
					// Collect errors
					result.AddError(file.FilePath, 0, 0, null, ex.Message);
					LoggingService.LogError (String.Format (CultureInfo.CurrentUICulture, "Cannot generate design code for xib file {0}", file.FilePath), ex);
				}
			}
			
			return result;
		}

		private static void GenerateXibDesignCode (CodeBehindWriter writer, ProjectFile file, ProjectFile designerFile)
		{
			// Create the compilation unit
			CodeCompileUnit ccu = new CodeCompileUnit ();
			CodeNamespace ns = new CodeNamespace (((DotNetProject)file.Project).GetDefaultNamespace (designerFile.FilePath));
			ccu.Namespaces.Add (ns);
			
			// Add namespaces
			ns.Imports.AddRange(CodeBehindGenerator.GetNamespaces(file.Project));
			
			// Parse the XIB document to collect the class names and their definitions
			IBDocument document = IBDocument.LoadFromFile (file.FilePath.FullPath);
			ClassDescriptionCollector collector = new ClassDescriptionCollector ();
			document.Root.Accept (collector);
			
			// Iterate over the extracted class names
			foreach (String className in collector.ClassNames) {
				CodeTypeDeclaration ctd = CodeBehindGenerator.GetType(collector, writer, className);
				if (ctd != null) {
					ns.Types.Add(ctd);
				}
			}
			
			// Write the result
			writer.Write (ccu, designerFile.FilePath);
		}

		private static ProjectFile GetDesignerFile (ProjectFile file)
		{
			String name = file.Name + ".designer";
			ProjectFile designerFile = file.DependentChildren.Where (f => f.Name.StartsWith (name)).FirstOrDefault ();
			return designerFile;
		}

		private static bool IsMoreRecent (ProjectFile file1, ProjectFile file2)
		{
			return File.GetLastWriteTime (file1.FilePath.FullPath) > File.GetLastWriteTime (file2.FilePath.FullPath);
		}
	}
}
