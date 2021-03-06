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
using System.Data;
using Community.CsharpSqlite.SQLiteClient;
using System.Management.Automation;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.PowerShell.SQLite.Provider
{
    class SQLiteRecordNodeFactory : NodeFactoryBase, IRemoveItem, ISetItem
    {
        private readonly ISQLiteContext _context;
        private readonly DataRow _record;

        public SQLiteRecordNodeFactory( ISQLiteContext context, DataRow record )
        {
            _context = context;
            _record = record;
        }

        public override IPathNode GetNodeValue()
        {
            return new PathNode( _record, Name, false );
        }

        public override string Name
        {
            get { return PrimaryKey; }
        }

        string PrimaryKey
        {
            get
            {
                DynamicParametersFactory factory = new DynamicParametersFactory(_context, _record.Table.TableName);
                return factory.GetPrimaryKey(_record).ToString();
            }
        }
        public void RemoveItem(IContext context, string path, bool recurse)
        {
            var factory = new DynamicParametersFactory(_context, _record.Table.TableName);
            var sql = factory.ToDeleteRecord( PrimaryKey );

            context.WriteVerbose("DELETE SQL: " + sql);
            _context.Execute(sql);
        }

        public object RemoveItemParameters
        {
            get { return null; }
        }

        public IPathNode SetItem(IContext context, string path, object value)
        {
            var factory = new DynamicParametersFactory(_context, _record.Table.TableName);

            //TODO: invert sql generation and parameter snooping to update only specified fields
            var cmd = factory.ToUpdate();

            context.WriteVerbose("UPDATE SQL: " + cmd.CommandText);
            
            factory.Parameterize(cmd, PrimaryKey, value, context.DynamicParameters as RuntimeDefinedParameterDictionary);

            cmd.ExecuteNonQuery();
            return GetNodeValue();
        }

        public object SetItemParameters
        {
            get
            { 
                var factory = new DynamicParametersFactory(_context, _record.Table.TableName);
                return factory.CreateForSetItem();
            }
        }
    }
}
