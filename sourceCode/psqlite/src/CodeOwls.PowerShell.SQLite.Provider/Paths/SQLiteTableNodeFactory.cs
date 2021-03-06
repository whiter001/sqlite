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
using System.Data;
using Community.CsharpSqlite.SQLiteClient;
using System.Linq;
using System.Management.Automation;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.PowerShell.SQLite.Provider
{
    class SQLiteTableNodeFactory : NodeFactoryBase, IRemoveItem, INewItem, IInvokeItem
    {
        private readonly ISQLiteContext _context;
        private readonly DataRow _schema;

        public SQLiteTableNodeFactory( ISQLiteContext context, DataRow schema )
        {
            _context = context;
            _schema = schema;
        }

        public override IEnumerable<INodeFactory> GetNodeChildren( IContext context )
        {
            var filter = context.Filter;
            var sql = "SELECT * FROM " + Name;
            if( ! String.IsNullOrEmpty( filter ) )
            {
                sql += " WHERE ( " + filter + " )";
            }
            var command = new SqliteCommand(sql , _context.Connection);
            var table = new DataTable( Name );
            
            context.WriteVerbose( sql );
            using( var data = new SqliteDataAdapter(command) )
            {
                data.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                context.WriteDebug( "filling data table" );
                data.Fill(table);
                context.WriteDebug("data table fill complete: " + table.Rows.Count + " rows of data");
            }

            foreach( DataRow row in table.Rows )
            {
                context.WriteDebug( "adding data row");
                yield return new SQLiteRecordNodeFactory( _context, row);
            }
        }

        public override IPathNode GetNodeValue()
        {
            return new PathNode( _schema, Name, true );
        }

        public override string Name
        {
            get { return _schema["tbl_name"].ToString(); }
        }

        public void RemoveItem(IContext context, string path, bool recurse)
        {
            var ddl = "DROP TABLE " + path;
            context.WriteVerbose( ddl );

            var cmd = _context.CreateCommand(ddl);
            cmd.ExecuteNonQuery();
        }

        public object RemoveItemParameters
        {
            get { return null; }
        }

        public IPathNode NewItem(IContext context, string path, string itemTypeName, object newItemValue)
        {
            var parameterDictionary = context.DynamicParameters as RuntimeDefinedParameterDictionary;
            var value = newItemValue.ToPSObject();
            
            var factory = new DynamicParametersFactory(_context, Name);
            var command = factory.ToInsert();
            
            context.WriteVerbose( "INSERT SQL: " + command.CommandText );

            factory.Parameterize(command, value, parameterDictionary);

            foreach (SqliteParameter param in command.Parameters)
            {
                context.WriteVerbose("PARAMETERS: @" + param.ParameterName + " = [" + param.Value + "]");
            }

            var id = command.ExecuteScalar();
            
            return Resolve(context, id.ToString()).First().GetNodeValue();
        }

        public IEnumerable<string> NewItemTypeNames
        {
            get { return null; }
        }

        public object NewItemParameters
        {
            get 
            {
                var factory = new DynamicParametersFactory(_context, Name);
                return factory.CreateForNewItem();
            }
        }

        public object InvokeItemParameters
        {
            get { return new InvokeItemParameters(); }
        }

        public IEnumerable<object> InvokeItem(IContext context, string path)
        {
            InvokeItemParameters parameters =
                context.DynamicParameters as InvokeItemParameters;

            var filter = context.Filter;
            var sql = parameters.Sql;
            if (String.IsNullOrEmpty(sql))
            {
                return null;
            }

            var command = new SqliteCommand(sql, _context.Connection);
            context.WriteVerbose(sql);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    PSObject record = new PSObject();
                    for (int c = 0; c < reader.FieldCount; ++c)
                    {
                        var name = reader.GetName(c);
                        var value = reader.GetValue(c);
                        record.Properties.Add(new PSNoteProperty(name, value));
                    }

                    context.WriteItemObject(record, path, false);
                    //yield return new SQLiteRecordNodeFactory(_sqliteContext, reader.);
                }
            }

            return null;
        }
    }
}
