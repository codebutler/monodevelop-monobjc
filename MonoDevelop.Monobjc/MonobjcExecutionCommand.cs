//
// MonobjcExecutionCommand.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Monobjc
{
	public class MonobjcExecutionCommand : ExecutionCommand
	{
		FilePath appDirectory;
		string appName;
		string arguments;
		bool debugMode;
		
		public MonobjcExecutionCommand (string appDirectory, string appName, string arguments, bool debugMode)
		{
			this.appDirectory = appDirectory;
			this.appName      = appName;
			this.arguments    = arguments;
			this.debugMode    = debugMode;
		}

		public IList<string> UserAssemblyPaths { get; set; }

		public override string CommandString {
			get { return this.appDirectory.Combine("Contents").Combine("MacOS").Combine(this.appName); }
		}
		
		public string AppName {
			get { return appName; }
		}
		
		public string Arguments {
			get { return arguments; }
		}
		
		public bool DebugMode { 
			get { return debugMode; }
		}
	}
}
