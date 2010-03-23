//
// CreatePList.cs
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

namespace MonoDevelop.Monobjc.Build.Tasks
{
	public class CreatePList : Task
	{
		public string FileName {
			get; set;
		}
		
		public string Name {
			get; set;
		}
		
		public string ExeName {
			get; set;
		}
		
		public string DisplayName { 
			get; set;
		}
		
		public string Identifier {
			get; set;
		}
		
		public string Version {
			get; set;
		}
		
		public string Icon {
			get; set;
		}
		
		public string DevelopmentRegion {
			get; set;
		}
		
		public string MainNibFile {
			get; set;
		}
		
		public override bool Execute ()
		{
			string displayName = DisplayName ?? Name;
			string identifier  = Identifier ?? String.Format("com.yourcompany.{0}", Name);
			string version     = Version ?? "1.0";
			string region      = DevelopmentRegion ?? "English";
			
			var doc = new PlistDocument();
			var docRoot = new PlistDictionary();			
  			docRoot["CFBundleExecutable"]            = ExeName;
			docRoot["CFBundleInfoDictionaryVersion"] = "6.0";
			docRoot["CFBundlePackageType"]           = "APPL";
			docRoot["CFBundleName"]                  = displayName;
			docRoot["CFBundleDisplayName"]           = displayName;
			docRoot["CFBundleIdentifier"]            = identifier;
			docRoot["CFBundleVersion"]               = version;
			docRoot["CFBundleDevelopmentRegion"]     = region;
			
			if (!String.IsNullOrEmpty(Icon))
				docRoot["CFBundleIconFile"] = Icon;
			
			if (!String.IsNullOrEmpty(MainNibFile))
				docRoot["NSMainNibFile"] = Path.GetFileNameWithoutExtension(MainNibFile);
			
			doc.Root = docRoot;
			
			using (XmlTextWriter writer = new XmlTextWriter(FileName, Encoding.UTF8)) {
				doc.Write(writer);
			}
			
			Log.LogMessage("Wrote {0}", FileName);
			
			return true;
		}
	}
}
