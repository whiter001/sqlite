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
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.PowerShell.SQLite.Provider
{
    class SQLitePathNodeProcessor : PathNodeProcessorBase
    {
        private readonly ISQLiteContext _context;

        // regex to isolate connection string details:
        //  ex: \\[Data Source=:memory:]\\
        //  ex: [Data Source=c:\temp\data.sqlite; Enlist=N; ...]\\tablename
        static readonly Regex ConnectionStringPattern = new Regex(@"^\\*\[.+\]");
        
        public SQLitePathNodeProcessor( ISQLiteContext context )
        {            
            _context = context;
        }

        public override System.Collections.Generic.IEnumerable<INodeFactory> ResolvePath(PowerShell.Provider.PathNodeProcessors.IContext context, string path)
        {
            path = ConnectionStringPattern.Replace(path, String.Empty);
            return base.ResolvePath(context, path);
        } 

        protected override INodeFactory Root
        {
            get
            {
                return new SQLiteRootNodeFactory( _context );
            }
        }
    }
}
