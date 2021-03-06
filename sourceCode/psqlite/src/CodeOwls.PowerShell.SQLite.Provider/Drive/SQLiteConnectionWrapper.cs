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
using System.Data.Common;
using System.Transactions;
using Community.CsharpSqlite.SQLiteClient;

namespace CodeOwls.PowerShell.SQLite.Provider
{
    class SqliteConnectionWithEnlist : SqliteConnection
    {
        private DbTransaction _currentTransaction;

        public SqliteConnectionWithEnlist(string connectionString) : base(connectionString)
        {            
        }

        public override void EnlistTransaction(System.Transactions.Transaction transaction)
        {
            if (null != _currentTransaction)
            {
                return;
            }
            _currentTransaction = BeginTransaction();
            transaction.TransactionCompleted += TransactionCompleted;
        }

        void TransactionCompleted(object sender, System.Transactions.TransactionEventArgs e)
        {
            if (null == _currentTransaction)
            {
                return;
            }

            if (e.Transaction.TransactionInformation.Status == TransactionStatus.Committed)
            {
                _currentTransaction.Commit();    
            }
            else
            {
                _currentTransaction.Rollback();
            }
            _currentTransaction = null;
        }   
    }

    abstract class SqliteConnectionWrapper : IDisposable
    {
        protected SqliteConnectionWrapper( SqliteConnection connection )
        {
            Connection = connection;
        }
        
        public SqliteConnection Connection { get; protected set; }

        public abstract void Dispose();

        public static implicit operator SqliteConnection( SqliteConnectionWrapper wrapper )
        {
            return wrapper.Connection;
        }
    }
}
