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
using System.Runtime.Serialization;

namespace CodeOwls.PowerShell.Paths.Exceptions
{
    [Serializable]
    public class CopyOrMoveToDifferentContainerTypeException : ProviderException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public CopyOrMoveToDifferentContainerTypeException()
        {
        }

        public CopyOrMoveToDifferentContainerTypeException(string copy, string copyPath)
            : base("The item at [" + copy + "] cannot be copied to the specified location ["+copyPath+"] because the destination contains different types of items.")
        {
        }

        protected CopyOrMoveToDifferentContainerTypeException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
