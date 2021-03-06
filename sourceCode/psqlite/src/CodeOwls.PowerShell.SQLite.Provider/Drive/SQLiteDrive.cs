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
using System.Data;
using Community.CsharpSqlite.SQLiteClient;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Threading;

namespace CodeOwls.PowerShell.SQLite.Provider
{
    public class SQLiteDrive : CodeOwls.PowerShell.Provider.Drive
    {
        private readonly SQLiteProvider.DriveParams _driveParams;
        private SqliteConnection _connection;
        private const string InMemoryConnectionString = ":memory:";

        public SQLiteDrive(PSDriveInfo driveInfo, SQLiteProvider.DriveParams driveParams) : base(driveInfo)
        {
            _driveParams = driveParams ?? new SQLiteProvider.DriveParams();
            _driveParams.ConnectionString = Regex.Replace( driveInfo.Root, @"^\[(.+)\].*", "$1" );

            if( _driveParams.ConnectionString.ToLowerInvariant().Contains( InMemoryConnectionString ) )
            {
                _driveParams.PersistentConnection = new SwitchParameter( true );
            }
        }

        public string ConnectionString
        {
            get { return _driveParams.ConnectionString; }
        }

        public bool IsPersistentConnection
        {
            get { return _driveParams.PersistentConnection.ToBool();  }
        }

        internal SqliteConnectionWrapper CreateConnectionWrapper()
        {
            SqliteConnectionWrapper wrapper = null;

            if (IsPersistentConnection)
            {
                if (null == _connection)
                {
                    _connection = CreateNewConnection();
                }

                wrapper = new PersistentSqliteConnectionWrapper( _connection );
            }
            else
            {
                _connection = CreateNewConnection();
                wrapper = new TransientSqliteConnectionWrapper( _connection );
            }

            return wrapper;
        }

        internal void CloseConnection()
        {
            if (!IsPersistentConnection)
            {
                CloseCurrentConnection();
            }
        }
        
        internal void DisposeDrive()
        {
            CloseCurrentConnection();
        }

        private SqliteConnection CreateNewConnection()
        {
            var connection = new SqliteConnectionWithEnlist(ConnectionString);
            connection.Open();
            return connection;
        }
        
        private void CloseCurrentConnection()
        {
            var c = Interlocked.Exchange(ref _connection, null);
            if (null != c)
            {
                if (c.State != ConnectionState.Closed)
                {
                    c.Close();
                }
                c.Dispose();
            }
        }

    }
}
