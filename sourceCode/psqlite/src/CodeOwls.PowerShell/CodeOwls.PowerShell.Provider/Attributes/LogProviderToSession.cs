/*
	Copyright (c) 2012 Code Owls LLC

	Permission is hereby granted, free of charge, to any person obtaining a copy 
	of this software and associated documentation files (the "Software"), to 
	deal in the Software without restriction, including without limitation the 
	rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
	sell copies of the Software, and to permit persons to whom the Software is 
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in 
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
	IN THE SOFTWARE. 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace CodeOwls.PowerShell.Provider.Attributes
{
    [Serializable]
    [MulticastAttributeUsage(
        MulticastTargets.Method,
        TargetMemberAttributes = MulticastAttributes.Protected,
        Inheritance = MulticastInheritance.Multicast)]
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class LogProviderToSession : OnMethodBoundaryAspect
    {
        public override bool CompileTimeValidate(System.Reflection.MethodBase method)
        {
            return (null != method.DeclaringType.GetProperty("SessionState"));
        }

        public override void OnEntry(MethodExecutionArgs args)
        {
            var cmdlet = args.Instance as CmdletProvider;
            if( null == cmdlet )
            {
                return;
            }

            string parameters = "";
            if( null != args.Arguments && args.Arguments.Any() )
            {
                parameters = String.Join("; ", args.Arguments.ToList().ConvertAll(a => (a ?? "null").ToString()).ToArray());
            }
            try
            {
                var s = String.Format(
                    "[{0}] >> Entering [{1}] ( [{2}] )",
                    args.Instance.GetType().FullName,
                    args.Method.Name,
                    parameters);
                cmdlet.WriteDebug(s);
            }
            catch
            {               
            }
        }

        public override void OnExit(MethodExecutionArgs args)
        {            
            var cmdlet = args.Instance as CmdletProvider;
            if (null == cmdlet )
            {
                return;
            }

            try
            {
                var s = String.Format(
                    "[{0}] << Returning [{2}] from [{1}]",
                    args.Instance.GetType().FullName,
                    args.Method.Name,
                    args.ReturnValue ?? "null");
                cmdlet.WriteDebug( s );
            }
            catch
            {

            }
        }

        public override void OnException(MethodExecutionArgs args)
        {
            var cmdlet = args.Instance as CmdletProvider;
            if (null == cmdlet)
            {
                return;
            }

            try
            {
                cmdlet.WriteDebug(
                    String.Format(
                        "[{0}] !! Exception in [{1}]: [{2}]",
                        args.Instance.GetType().FullName,
                        args.Method.Name,
                        args.Exception));
            }
            catch
            {                
            }
        }
    }
}
