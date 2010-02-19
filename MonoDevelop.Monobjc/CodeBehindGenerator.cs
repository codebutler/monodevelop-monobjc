//
// CodeBehindGenerator.cs
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
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using Monobjc.Tools.InterfaceBuilder;
using Monobjc.Tools.InterfaceBuilder.Visitors;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Projects;

namespace MonoDevelop.Monobjc
{		
	internal static class CodeBehindGenerator
	{
		public static CodeNamespaceImport[] GetNamespaces(Project project)
		{
			// FIXME: Add each Monobjc reference to the imports, quick and dirty...
			// TODO: What if I need to reference another component from a non-Monobjc reference ?
			// Maybe we need to use the ParserService to resolve reference...
			DotNetProject p = (DotNetProject) project;
			IEnumerable<ProjectReference> references = p.References.Where(BuildUtils.IsMonobjcReference);
			List<CodeNamespaceImport> namespaces = new List<CodeNamespaceImport>();
			foreach(ProjectReference reference in references) {
				AssemblyName name = new AssemblyName(reference.Reference);
				namespaces.Add(new CodeNamespaceImport(name.Name));
			}
			return namespaces.ToArray();
		}
		
		public static CodeTypeDeclaration GetType(ClassDescriptionCollector collector, CodeBehindWriter writer, String className)
		{
			List<IBPartialClassDescription> descriptions = collector[className];
			
			// Don't generate the partial class if there are neither actions nor outlets
			int actions = descriptions.Sum (description => description.Actions.Count);
			int outlets = descriptions.Sum (description => description.Outlets.Count);
			if (actions == 0 && outlets == 0) {
				return null;
			}
			
			// Create the partial class
			CodeTypeDeclaration type = new CodeTypeDeclaration(className);
			type.IsClass = true;
			type.IsPartial = true;
			
			// Create fields for outlets
			foreach (IBPartialClassDescription.OutletDescription outlet in descriptions.SelectMany(description => description.Outlets)) {
				type.Members.Add(GetOutletField(outlet));
			}
			
			// Create methods for actions
			foreach (IBPartialClassDescription.ActionDescription action in descriptions.SelectMany(description => description.Actions)) {
				type.Members.Add(GetPartialMethod(action, writer));
				type.Members.Add(GetExposedMethod(action));
			}
			
			return type;
		}
		
		public static CodeTypeMember GetOutletField(IBPartialClassDescription.OutletDescription outlet)
		{
			CodeMemberField field = new CodeMemberField(outlet.ClassName, outlet.Name);
			field.Attributes = MemberAttributes.Public;
			
			// Add custom attributes
			field.CustomAttributes.Add(new CodeAttributeDeclaration("IBOutlet"));
			field.CustomAttributes.Add(new CodeAttributeDeclaration("ObjectiveCField"));
			
			return field;
		}
		
		public static CodeTypeMember GetPartialMethod(IBPartialClassDescription.ActionDescription action, CodeBehindWriter writer)
		{
			String selector = action.Message;
			String name = GenerateMethodName(selector);
				
			// Partial method are only possible by using a snippet of code as CodeDom does not handle them
			CodeSnippetTypeMember partialMethod;
			if (writer.Provider is CSharpCodeProvider) {
				partialMethod = new CodeSnippetTypeMember("partial void " + name + "(Id sender);" + Environment.NewLine);
			} else if (writer.Provider is VBCodeProvider) {
				// TODO: Check the generated code
				partialMethod = new CodeSnippetTypeMember("Partial Private Sub " + name + "(Id sender)" + Environment.NewLine + "End Sub" + Environment.NewLine);
			} else {
				throw new NotSupportedException("Provider not supported yet : " + writer.Provider);
			}
			
			return partialMethod;
		}
		
		public static CodeTypeMember GetExposedMethod(IBPartialClassDescription.ActionDescription action)
		{
			String selector = action.Message;
			String name = GenerateMethodName(selector);
			
			// Create a public method
			CodeMemberMethod exposedMethod = new CodeMemberMethod();
			exposedMethod.Attributes = MemberAttributes.Public;
			exposedMethod.Name = "__" + name;
			exposedMethod.ReturnType = new CodeTypeReference(typeof(void));
			exposedMethod.Parameters.Add(new CodeParameterDeclarationExpression("Id", "sender"));
			
			// Add custom attributes
			exposedMethod.CustomAttributes.Add(new CodeAttributeDeclaration("IBAction"));
			exposedMethod.CustomAttributes.Add(new CodeAttributeDeclaration("ObjectiveCMessage", new CodeAttributeArgument(new CodePrimitiveExpression(selector))));
			
			// Add body to call partial method
			exposedMethod.Statements.Add(new CodeMethodInvokeExpression(
			                                                            new CodeThisReferenceExpression(), 
			                                                            name, 
			                                                            new CodeArgumentReferenceExpression("sender")
			                                                            )
			                             );
			
			return exposedMethod;
		}
		
		private static String GenerateMethodName(String selector)
		{
			String name = selector.TrimEnd(':');
			name = name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
			return name;
		}
	}
}
