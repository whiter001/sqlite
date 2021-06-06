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

Describe "SQLite Filtering" {

	Import-Module SQLite;

	function mount-db {
		New-PSDrive -PSProvider SQLite -Name db -Root "Data Source=file::memory:,Version=3,New=true" -Scope global | Out-Null;		
		New-Item -path db:/data -Value @{ id='INTEGER PRIMARY KEY'; username="TEXT"; userid="INTEGER" } | Out-Null;
		
		New-Item -path db:/data -username "USER3" -userid 3 | Out-Null
	}
	
	function unmount-db {		
		Remove-PSDrive -Name db | Out-Null;	
	}
	
	
	It "filters on get-childitem" {
		mount-db;
		$items = ls db:/data -Filter "USERNAME='USER3'";
		$results = $items.userid -eq 3;
		$results | verify-all;		
		unmount-db;
	}
	
	It "filters on set-item" {
		mount-db;
		set-item db:/data -Filter "USERNAME='USER3'" -Value @{ userid=1 };
		$results = ( get-item db:/data/1 ).userid -eq 1;
		$results | verify-all;		
		unmount-db;
	}
	
	It "filters on remove-item" {
		mount-db;
		remove-item db:/data -Filter "USERNAME='USER3'" 
		$results = -not( get-item db:/data/1 -ErrorAction 'silentlycontinue' )
		$results | verify-all;		
		unmount-db;
	}
	
	Remove-Module SQLite;
}