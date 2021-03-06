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
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using Community.CsharpSqlite.SQLiteClient;
using System.Linq;
using System.Management.Automation;
using System.Text;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.SQLite.Provider
{
    class DynamicParametersFactory
    {
        private readonly ISQLiteContext _context;
        private readonly string _tableName;
        private readonly IEnumerable<DataRow> _columns;
        private const string DynamicParameterSetName = "SimplifiedSyntax";

        public DynamicParametersFactory( ISQLiteContext context, string tableName )
        {
            _context = context;
            _tableName = tableName;
            _columns = from DataRow row in context.Connection.GetSchema("COLUMNS", new []{tableName}).Rows                       
                       select row;
        }

        public RuntimeDefinedParameterDictionary CreateForNewItem()
        {
            var parameters = new RuntimeDefinedParameterDictionary();

            var attrs = from row in _columns
                        let nullable =  0 == (long)row["notnull"] 
                        let primaryKey = 1 == (long)row["pk"]
                        let columnName = row["name"].ToString()
                        let dataType = ConvertSQLiteTypeToType( row["type"].ToString() )
                        select new RuntimeDefinedParameter(
                            columnName,
                            dataType,
                            new Collection<Attribute>
                                    {
                                        new ParameterAttribute()
                                            {
                                                Mandatory = ! nullable && ! primaryKey,
                                                ValueFromPipelineByPropertyName = true,                                                
                                            },
                                            
                                    }
                            );

            attrs.ToList().ForEach(attr => parameters.Add(attr.Name, attr));
            return parameters;
        }

        public RuntimeDefinedParameterDictionary CreateForSetItem()
        {
            var parameters = new RuntimeDefinedParameterDictionary();
            
            var attrs = from row in _columns
                        let primaryKey = 1 ==  (long)row["pk"]
                        let columnName = row["name"].ToString()
                        let dataType = ConvertSQLiteTypeToType(row["type"].ToString())
                        where ! primaryKey
                        select new RuntimeDefinedParameter(
                            columnName,
                            dataType,
                            new Collection<Attribute>
                                    {
                                        new ParameterAttribute()
                                            {
                                                ValueFromPipelineByPropertyName = true,
                                            }
                                    }
                            );

            attrs.ToList().ForEach(attr => parameters.Add(attr.Name, attr));
            return parameters;
        }
        
        public SqliteCommand ToInsert()
        {
            var ddl = "INSERT INTO {0} ( '{1}' ) VALUES( {2} ); SELECT MAX( {3} ) FROM {0};";
            var schema = GetSchema();

            var parameters = ( from DataColumn field in schema.Columns
                             select new SqliteParameter("@" + field.ColumnName, null) );
            var fields = ( from DataColumn field in schema.Columns
                             select field.ColumnName );

            var parameterList = String.Join(", ", (from p in parameters select p.ParameterName).ToArray());
            var fieldList = String.Join("', '", fields.ToArray());

            ddl = String.Format(
                ddl,
                _tableName,
                fieldList,
                parameterList,
                PrimaryKeyField
                );

            var cmd = _context.CreateCommand( ddl );
            cmd.Parameters.AddRange( parameters.ToArray() );

            return cmd;
        }

        public SqliteCommand ToUpdate()
        {
            var ddlBuilder = new StringBuilder( "BEGIN TRANSACTION;" + Environment.NewLine );

            const string ddl = "UPDATE {0} SET {1}={2} WHERE ( {5} AND {3}={4} );{6}";
            var schema = GetSchema();
            var keyFieldName = PrimaryKeyField;
            var keyFieldParamName = "@" + keyFieldName;

            var fields = (from DataColumn column in schema.Columns
                          where column.ColumnName!= keyFieldName
                          let fieldName = column.ColumnName
                          let paramName = "@" + fieldName
                          let paramSpecifiedName = paramName + "IsSpecified"
                          select new
                                     {
                                         FormatFields = new object[]
                                                            {

                                                                _tableName,
                                                                fieldName,
                                                                paramName,
                                                                keyFieldName,
                                                                keyFieldParamName,
                                                                paramSpecifiedName,
                                                                Environment.NewLine
                                                            },
                                         Parameter = new SqliteParameter( paramName, null ),
                                         SpecifiedParameter = new SqliteParameter( paramSpecifiedName, false )
                                     }).ToList();

            fields.ForEach(set =>
                           ddlBuilder.AppendFormat(
                               ddl,
                               set.FormatFields
                               ));

            ddlBuilder.Append("COMMIT TRANSACTION");

            var cmd = _context.CreateCommand( ddlBuilder.ToString() );
            
            cmd.Parameters.AddRange( fields.Select(f=>f.Parameter).ToArray() );
            cmd.Parameters.AddRange(fields.Select(f => f.SpecifiedParameter).ToArray());
            return cmd;   
        }

        private DataTable GetSchema()
        {
            var schema = new DataTable();
            var adapter = new SqliteDataAdapter("SELECT * FROM " + _tableName + " LIMIT 1", _context.Connection);
            adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            adapter.FillSchema(schema, SchemaType.Source);
            return schema;
        }

        public DataAdapter ToDataAdapter()
        {
            SqliteDataAdapter adapter = new SqliteDataAdapter();
            
            adapter.SelectCommand = _context.CreateCommand( ToSelect() );
            return adapter;
        }

        private string ToSelect()
        {
            return "SELECT * FROM " + _tableName;
        }

        void SpecifyParameter( SqliteParameterCollection parameters, string parameterName, object value )
        {
            parameters[parameterName].ResetDbType();
            parameters[parameterName].Value = value;
            var isSpecifiedName = parameterName + "IsSpecified";
            if( parameters.Contains( isSpecifiedName ))
            {
                parameters[isSpecifiedName].Value = true;
            }
        }

        void Parameterize( SqliteParameterCollection command, DataRow record )
        {
            if (null != record)
            {
                foreach (SqliteParameter parameter in command)
                {
                    string name = parameter.ParameterName.TrimStart('@', '$');
                    if (!record.Table.Columns.Contains(name))
                    {
                        continue;
                    }
                    SpecifyParameter( command, parameter.ParameterName, record[name] );
                }
            }
        }

        void Parameterize(SqliteParameterCollection command, PSObject item)
        {
            if (null != item)
            {
                foreach (SqliteParameter parameter in command )
                {
                    string name = parameter.ParameterName.TrimStart('@', '$');
                    var prop = item.Properties.Match(name).FirstOrDefault();
                    if (null == prop)
                    {
                        continue;
                    }
                    SpecifyParameter(command, parameter.ParameterName, prop.Value);
                }
            }
        }
        
        void Parameterize(SqliteParameterCollection command, RuntimeDefinedParameterDictionary parameterDictionary)
        {
            if (null != parameterDictionary)
            {
                foreach (SqliteParameter parameter in command)
                {
                    string name = parameter.ParameterName.TrimStart('@', '$');
                    if (! (parameterDictionary.ContainsKey(name) && parameterDictionary[name].IsSet) )
                    {
                        continue;
                    }                    
                    SpecifyParameter(command, parameter.ParameterName, parameterDictionary[name].Value);
                }
            }
        }

        void Parameterize(SqliteParameterCollection command, object idValue)
        {
            if (null != idValue)
            {
                var pkf = PrimaryKeyField;
                if (!command.Contains(pkf))
                {
                    var p = new SqliteParameter("@" + pkf, idValue);                    
                    command.Add(p);
                }
                else
                {
                    command[PrimaryKeyField].ResetDbType();
                    command[PrimaryKeyField].Value = idValue;
                }
            }
        }

        public SqliteCommand Parameterize( SqliteCommand cmd, object idValue, object value, RuntimeDefinedParameterDictionary parameters )
        {
            Parameterize(cmd, value, parameters);
            Parameterize(cmd.Parameters, idValue);

            return cmd;            
        }

        public SqliteCommand Parameterize( SqliteCommand cmd, object value, RuntimeDefinedParameterDictionary parameters )
        {
            DataRow dataRow = null;
            var pso = value.ToPSObject();

            if (null != pso)
            {
                dataRow = pso.BaseObject as DataRow;
                Parameterize(cmd.Parameters, pso);
            }

            if (null != dataRow)
            {
                Parameterize(cmd.Parameters, dataRow);
            }
            else
            {
                Parameterize(cmd.Parameters, parameters);
            }
            
            return cmd;            
        }

        public string ToDeleteRecord(string idValue)
        {
            return String.Format("DELETE FROM {0} WHERE {1}={2}", _tableName, PrimaryKeyField, idValue);
        }

        private string PrimaryKeyField
        {
            get
            {
                _context.Provider.WriteDebug( "determining primary key field name");
                var idField = (from row in _columns
                               where 1 == (long) row["pk"]
                               select row["name"]).First().ToString();

                _context.Provider.WriteDebug("primary key field name: " + idField);
                return idField;
            }
        }


        private static Type ConvertSQLiteTypeToType(string sqliteDataType)
        {
            Type type;
            if( ! SQLiteTypeMap.TryGetValue( sqliteDataType, out type ) )
            {
                return typeof (object);
            }
            return type;
        }

        private static Dictionary<string, Type> SQLiteTypeMap = new Dictionary<string, Type>
                                                                    {
                                                                        {"text", typeof (string)},
                                                                        {"integer", typeof (int?)},
                                                                        {"float", typeof (float?)},
                                                                        {"blob", typeof (byte[])}
                                                                    };

        public object GetPrimaryKey(DataRow record)
        {
            var idField = PrimaryKeyField;
            return record[idField];
        }
    }
}
