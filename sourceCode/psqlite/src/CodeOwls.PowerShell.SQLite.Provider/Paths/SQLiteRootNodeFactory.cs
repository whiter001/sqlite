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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Community.CsharpSqlite.SQLiteClient;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.PowerShell.SQLite.Provider
{
    class SQLiteRootNodeFactory : NodeFactoryBase, INewItem, IInvokeItem
    {
        private readonly ISQLiteContext _sqliteContext;

        public SQLiteRootNodeFactory( ISQLiteContext context )
        {
            _sqliteContext = context;
        }

        public override IEnumerable<INodeFactory> GetNodeChildren( IContext context )
        {
            var connection = _sqliteContext.Connection;
            var schema = connection.GetSchema("TABLES");
            foreach( DataRow row in schema.Rows )
            {                
                yield return new SQLiteTableNodeFactory( _sqliteContext, row );
            }

        }
        public override IPathNode GetNodeValue()
        {
            return new PathNode(_sqliteContext.Connection, Name, true);
        }

        public override string Name
        {
            get { return _sqliteContext.Connection.DataSource; }
        }
        
        public IPathNode NewItem(IContext context, string path, string itemTypeName, object newItemValue)
        {
            string ddl = newItemValue as string;
            var schema = newItemValue as Hashtable;
            var parameters = context.DynamicParameters as NewTableParameters;
            
            if( null != ddl )
            {
                ddl = String.Format(@"CREATE TABLE {0} ( {1} )", path, ddl);
            }
            else if (null != schema)
            {
                var coldefs = ConvertToColumnDefinitionDDL(schema);
                ddl = String.Format(@"CREATE TABLE {0} ( {1} )", path, coldefs);
            }
            else if( null != parameters)
            {
                var coldefs = ConvertToColumnDefinitionDDL(parameters);
                ddl = String.Format(@"CREATE TABLE {0} ( {1} )", path, coldefs);
            }

            if ( null == ddl )
            {
                throw new ArgumentException(
                    @"The new table schema must be specified as a DDL string, a hashtable of column names associated with SQLite DDL strings, or as individual parameters.  For instance:
new-item mydb:/tableOfNames -value @{ 
    id = 'INTEGER PRIMARY KEY';
    name = 'TEXT NOT NULL';
    ...
}

or

new-item mydb:/tableOfNames -value 'id INTEGER PRIMARY KEY, name TEXT NOT NULL'

or

new-item mydb:/tableOfNames -id INTEGER PRIMARY KEY -name TEXT NOT NULL
",
                    "newItemValue"
                );
            } 
                        
            context.WriteVerbose(ddl);

            var cmd = _sqliteContext.CreateCommand(ddl);
            cmd.ExecuteNonQuery();

            var node = Resolve(context, path);
            if( null == node || ! node.Any() )
            {
                throw new InvalidOperationException( "Failed to create new table " + path );
            }
            return node.First().GetNodeValue();
        }

        private object ConvertToColumnDefinitionDDL(NewTableParameters parameters)
        {
            List<string> ddl = new List<string>();
            string current = "";
            var pkre = new Regex(@"\s+primary\s+key($|\s+)", RegexOptions.IgnoreCase);
            var hasPk = false;

            foreach (string item in parameters.Columns )
            {
                if( item.StartsWith("-"))
                {
                    if( !String.IsNullOrEmpty(current))
                    {
                        hasPk = hasPk || pkre.IsMatch(current);
                        ddl.Add( current );
                    }
                    current = item.TrimStart('-');
                }
                else
                {
                    current += " " + item;
                }
            }

            if (!String.IsNullOrEmpty(current))
            {
                hasPk = hasPk || pkre.IsMatch(current);
                ddl.Add(current);
            }

            if (! hasPk )
            {
                ddl.Add("_id integer primary key");
            }

            return String.Join(", ", ddl.ToArray());
        }

        private string ConvertToColumnDefinitionDDL(Hashtable schema)
        {
            var pkre = new Regex(@"\s+primary\s+key($|\s+)", RegexOptions.IgnoreCase);
            var hasPk = false;
            List<string> ddl = new List<string>();
            foreach( string key in schema.Keys )
            {
                string value = schema[key].ToString();
                hasPk = hasPk || pkre.IsMatch(value);
                ddl.Add( String.Format( "{0} {1}", key, value ) );
            }

            if( ! hasPk )
            {
                ddl.Add( "_id integer primary key");
            }

            return String.Join(", ", ddl.ToArray());
        }

        public IEnumerable<string> NewItemTypeNames
        {
            get { return null; }
        }

        public object NewItemParameters
        {
            get
            {
                return new NewTableParameters();
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

            var command = new SqliteCommand(sql, _sqliteContext.Connection);
            context.WriteVerbose(sql);
            using( var reader = command.ExecuteReader() )
            {
                while (reader.Read())
                {
                    PSObject record = new PSObject();
                    for( int c = 0; c < reader.FieldCount; ++c )
                    {
                        var name = reader.GetName(c);
                        var value = reader.GetValue(c);
                        record.Properties.Add( new PSNoteProperty(name, value));
                    }

                    context.WriteItemObject( record, path, false );
                    //yield return new SQLiteRecordNodeFactory(_sqliteContext, reader.);
                }
            }

            return null;
        }
    }

    public class InvokeItemParameters
    {
        [Parameter(Mandatory = true)]
        public string Sql { get; set; }    
    }

    public class NewTableParameters
    {
        [Parameter(Mandatory = false,ValueFromRemainingArguments = true)]
        public string[] Columns { get; set; }
    }
}
