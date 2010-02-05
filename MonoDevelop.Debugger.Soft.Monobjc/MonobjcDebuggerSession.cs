// 
// MonobjcDebuggerSession.cs
//  
// Author:
//       Eric Butler <eric@codebutler.com>
// 
// Copyright (c) 2010 Eric Butler
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
using Mono.Debugger;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Monobjc;
using System.IO;
using MonoDevelop.Core;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.Debugger.Soft.Monobjc
{
	public class MonobjcDebuggerSession : RemoteSoftDebuggerSession
	{
		System.Diagnostics.Process process;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			if (process != null)
				throw new InvalidOperationException("Already debugging!");
			
			var dsi = (MonobjcDebuggerStartInfo) startInfo;
			var cmd = dsi.ExecutionCommand;		
		
			StartListening(dsi);
			
			var psi = new System.Diagnostics.ProcessStartInfo(cmd.CommandString) {
				Arguments = cmd.Arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				UseShellExecute = false
			};
			psi.EnvironmentVariables["MONO_OPTIONS"] = String.Format("--debug --debugger-agent=transport=dt_socket,address={0}:{1}", dsi.Address, dsi.DebugPort);
			
			process = System.Diagnostics.Process.Start(psi);
			
			ConnectOutput(process.StandardOutput, false);
			ConnectOutput(process.StandardError, true);
			
			process.EnableRaisingEvents = true;
			process.Exited += delegate {
				EndSession();
				process = null;
			};
		}
		
		protected override void OnConnected ()
		{
			base.OnConnected ();
		}
				
		protected override void OnExit ()
		{
			// FIXME: base.OnExit() calls vm.Exit(0) which does nothing!
			// Eventually it calls EnsureExited() which forcefully kills the process.
			base.OnExit ();
		}

		protected override void EnsureExited ()
		{
			try {
				if (process != null && !process.HasExited)
					process.Kill ();
			} catch (Exception ex) {
				LoggingService.LogError ("Error force-terminating soft debugger process", ex);
			}
		}

		protected override string GetListenMessage (RemoteDebuggerStartInfo dsi)
		{
			return String.Format("Waiting for app to connect to: {0}:{1}", dsi.Address, dsi.DebugPort);
		}
	}
	
	class MonobjcDebuggerStartInfo : RemoteDebuggerStartInfo
	{
		public MonobjcExecutionCommand ExecutionCommand { get; private set; }
		
		public MonobjcDebuggerStartInfo (IPAddress address, int debugPort, MonobjcExecutionCommand cmd)
			: base (cmd.AppName, address, debugPort)
		{
			ExecutionCommand = cmd;
		}
	}
}
