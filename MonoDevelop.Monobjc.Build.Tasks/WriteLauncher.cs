//
// WriteLauncher.cs
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
using System.IO;
using System.Reflection;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Mono.Unix;

namespace MonoDevelop.Monobjc.Build.Tasks
{
	public class WriteLauncher : Task
	{
		public string FileName {
			get; set;
		}
			
		public override bool Execute ()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LaunchScript.sh");
			using (StreamReader reader = new StreamReader(stream)) {
				using (StreamWriter writer = new StreamWriter(FileName)) {
					writer.Write(reader.ReadToEnd());
				}
			}
			
			new UnixFileInfo(FileName).FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute | 
				FileAccessPermissions.GroupRead | FileAccessPermissions.GroupExecute |
				FileAccessPermissions.OtherRead | FileAccessPermissions.OtherExecute;
			
			return true;
		}
	}
}
