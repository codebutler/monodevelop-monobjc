//
// MonobjcProjectConfiguration.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.IO;

namespace MonoDevelop.Monobjc
{
	public class MonobjcProjectConfiguration : DotNetProjectConfiguration, ICustomDataItem
	{
		public MonobjcProjectConfiguration () : base ()
		{
			
		}
		
		public MonobjcProjectConfiguration (string name) : base (name)
		{
			
		}
			
		public FilePath AppDirectory {
			get {
				MonobjcProject proj = ParentItem as MonobjcProject;
				if (proj != null)
					return OutputDirectory.Combine(proj.BundleDisplayName + ".app");
				return FilePath.Null;
			}
		}
		
		public FilePath ContentsDirectory {
			get {
				MonobjcProject proj = ParentItem as MonobjcProject;
				if (proj != null)
					return AppDirectory.Combine("Contents");
				return FilePath.Null;
			}
		}
		
		public FilePath ResourcesDirectory {
			get {
				MonobjcProject proj = ParentItem as MonobjcProject;
				if (proj != null)
					return ContentsDirectory.Combine("Resources");
				return FilePath.Null;
			}
		}
		
		public FilePath MacOSDirectory {
			get {
				MonobjcProject proj = ParentItem as MonobjcProject;
				if (proj != null)
					return ContentsDirectory.Combine("MacOS");
				return FilePath.Null;				
			}
		}
		
		public FilePath LauncherScript {
			get {
				MonobjcProject proj = ParentItem as MonobjcProject;
				if (proj != null) {
					string exeName = Path.GetFileNameWithoutExtension(base.CompiledOutputName);
					return MacOSDirectory.Combine(exeName);
				}
				return FilePath.Null;				
			}
		}
		
		public DataCollection Serialize (ITypeSerializer handler) {
			return handler.Serialize(this);
		}
		
		public void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			handler.Deserialize(this, data);	
		}
	}
}
