//
// CompileXibs.cs
//
// Author:
//	     Eric Butler <eric@codebutler.com>
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
using System.Text;
using System.IO;
using MonoDevelop.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using PropertyList;
using System.Diagnostics;

namespace MonoDevelop.Monobjc.Build.Tasks
{
	public class CompileXibs : Task
	{
		public string[] Files {
			get; set;
		}
		
		public string DestinationFolder {
			get; set;
		}
		
		public override bool Execute ()
		{	
			foreach (var file in Files) {
				if (Path.GetExtension(file) == ".xib") {
					var fileOutput = Path.Combine(DestinationFolder, Path.GetFileNameWithoutExtension(file) + ".nib");
					var psi = new ProcessStartInfo("ibtool", String.Format("\"{0}\" --compile \"{1}\"", file, fileOutput));
					
					Log.LogMessage(psi.FileName + " " + psi.Arguments);					               
					
					var process = Process.Start(psi);
					process.WaitForExit();					
					if (process.ExitCode != 0) {
						Log.LogError("ibtool returned error code {0}", process.ExitCode);
						return false;
					}
				}
			}
			return true;
		}
	}
}
