#
# Copyright (c) 2012 Code Owls LLC
#
# Permission is hereby granted, free of charge, to any person obtaining a copy 
# of this software and associated documentation files (the "Software"), to 
# deal in the Software without restriction, including without limitation the 
# rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
# sell copies of the Software, and to permit persons to whom the Software is 
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in 
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
# FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
# IN THE SOFTWARE. 
# 

Describe "SQLite Transactions" {

	Import-Module SQLite;
	function mount-db {
		New-PSDrive -PSProvider SQLite  -Name db -Root "Data Source=file::memory:,Version=3,New=True" -Scope global | Out-Null;		
		New-Item -path db:/data -Value @{ id='INTEGER PRIMARY KEY'; username="TEXT"; userid="INTEGER" } | Out-Null;				
		$script:results = @();
	}
	
	function unmount-db {		
		Remove-PSDrive -Name db | Out-Null;	
	}
	
	
	It "supports no transaction" {
		mount-db;
		New-Item db:/data -username 'beef' -userid 123 | out-null
		$items = ls db:/data -Filter "username='beef'";
		$results = $items.userid -eq 123;
		$results | verify-all;		
		unmount-db;
	}	
	
	It "supports commit for add" {
		mount-db;
		Start-Transaction;
		New-Item db:/data -username 'beef' -userid 123 -UseTransaction | Out-Null
		complete-transaction;
		
		$items = ls db:/data -Filter "username='beef'";
		
		$results += $items.userid -eq 123;
		
		$results | verify-all;		
		unmount-db;
	}
	
	It "supports commit for remove" {	
		mount-db;
		
		New-Item db:/data -username 'beef' -userid 123 | Out-Null
		$results = @();
        $results += test-path db:/data/1
		
		Start-Transaction;
	    Remove-Item -Path db:/data/1 -UseTransaction
			
		complete-transaction;
		
		$results += -not ( test-path db:/data/1 )		
		
		$results | verify-all;		
		unmount-db;
	}
	
	It "supports rollback for add" {
		mount-db;
		
		Start-Transaction;
		
			New-Item db:/data -username 'beef' -userid 123 -UseTransaction | Out-Null
			$ti = ls db:/data -Filter "username='beef'";
		
		Undo-Transaction;
		
		$ri = ls db:/data -Filter "username='beef'";
		
		$results = @();
		$results += $ti.userid -eq 123;
		$results += -not $ri;
		
		$results | verify-all;		
		unmount-db;
	}

		
	It "supports rollback for remove" {
		mount-db;
		$i = New-Item db:/data -username 'beef' -userid 123;
		
		Start-Transaction;
		
			Remove-Item -path "db:/data/$($i.id)" -UseTransaction
			$ti = ls db:/data -Filter "username='beef'";
		
		Undo-Transaction;
		
		$ri = ls db:/data -Filter "username='beef'";
		
		$results = @();
		$results += $ri.userid -eq 123;
		$results += -not $ti;
		
		$results | verify-all;		
		unmount-db;
	}
	
	It "supports mixed transaction use" {
		mount-db;
		
		
		Start-Transaction;
			New-Item db:/data -username 'beef' -userid 123 | Out-Null;
			New-Item db:/data -username 'arino' -userid 234 -UseTransaction | out-null;
			
			$ti = ls db:/data -Filter "username='beef'";
			$ti1 = ls db:/data -Filter "username='arino'";
		
		Undo-Transaction;
		
		$ri = ls db:/data -Filter "username='beef'";
		$ri1 = ls db:/data -Filter "username='arino'";
		
		$results = @( [bool]$ti, [bool]$ti1, [bool]$ri, -not [bool]$ri2);
		
		$results | verify-all;		
		
		unmount-db;
	}
	
	Remove-Module SQLite;
}