#
#   Copyright (c) 2012 Code Owls LLC, All Rights Reserved.
#
#   Licensed under the Microsoft Reciprocal License (Ms-RL) (the "License");
#   you may not use this file except in compliance with the License.
#   You may obtain a copy of the License at
#
#     http://www.opensource.org/licenses/ms-rl
#
#   Unless required by applicable law or agreed to in writing, software
#   distributed under the License is distributed on an "AS IS" BASIS,
#   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
#   See the License for the specific language governing permissions and
#   limitations under the License.
# 
# 	pester tests for SQLite provider

Describe "SQLite Provider" {

	Import-Module SQLite;
	[Environment]::CurrentDirectory = $pwd; #because powershell is stupid wrt the system current directory and $pwd.

	function get-sqlitefilename
	{
		return ( "file:$pwd\_" + [IO.Path]::GetRandomFileName() + '.sqlitetest,Version=3,New=true' );
	}
	
	function remove-sqliteFiles
	{
		ls *.sqlitetest | Remove-Item -Force;
	}
	
	It "can be imported" {
		( Get-Module SQLite ) -ne $null;
	}
	
	It "mounts a new database" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		Test-Path db:/
		Remove-PSDrive -Name db | Out-Null;
		remove-sqliteFiles
	}
	
	It "mounts an existing database" {
		$filename = get-sqliteFileName;

		New-PSDrive -PSProvider SQLite  -Name db1 -root "Data Source=$filename" | Out-Null;
		$results = @(Test-Path db1:/);
		Remove-PSDrive -Name db1 | Out-Null;
		$results += -not( test-path db1:/ );
		
		$results | verify-all;
		remove-sqliteFiles
	}
	
	It "adds a new table" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			a1 = "TEXT";
			a2 = "INTEGER NOT NULL";
			a3 = "INTEGER NOT NULL";
			a4 = "INTEGER NOT NULL";
		} | Out-Null;
				
		Test-Path db:/mydb;
		
		Remove-PSDrive -Name db | Out-Null;
		remove-sqliteFiles
	}
	
	It "adds a new table using DDL" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @'
			id INTEGER PRIMARY KEY,
			a1 TEXT,
			a2 INTEGER NOT NULL,
			a3 INTEGER NOT NULL,
			a4 INTEGER NOT NULL
'@ | Out-Null;
				
		Test-Path db:/mydb;
		
		Remove-PSDrive -Name db | Out-Null;
		remove-sqliteFiles
	}
	
	It "adds a new table using param slurping" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -id INTEGER PRIMARY KEY -a1 TEXT -a2 INTEGER NOT NULL -a3 INTEGER NOT NULL -a4 INTEGER NOT NULL | Out-Null;
				
		Test-Path db:/mydb;
		
		Remove-PSDrive -Name db | Out-Null;
		remove-sqliteFiles
	}


	It "removes an existing table" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			a1 = "TEXT";
			a2 = "INTEGER NOT NULL";
			a3 = "INTEGER NOT NULL";
			a4 = "INTEGER NOT NULL";
		} | Out-Null;
		
		$result = @( Test-Path db:/mydb );
		
		Remove-Item -Path db:/mydb -Force;
		
		$result += -not( Test-Path db:/mydb );
		
		Remove-PSDrive -Name db | Out-Null;
		
		$result | verify-all;
		remove-sqliteFiles
	}
	
	It "raises error when removing an nonexisting table" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		$er = $null;
		Remove-Item -Path db:/mydb -Force -ErrorAction SilentlyContinue -ErrorVariable er;
		[bool]($er -ne $null );
		
		Remove-PSDrive -Name db | Out-Null;		
		remove-sqliteFiles
	}

	It "creates a new record" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			username = "TEXT";
			age = "INTEGER";
		} | Out-Null;
		
		$result = @( Test-Path db:/mydb );
		
		New-Item -path db:/mydb -username "Jimbo" -age 38 | Out-Null;
		
		$result += ( Test-Path db:/mydb/1 );
		$result += [bool]( 1 -eq ( ls db:/mydb | measure | select -ExpandProperty count) );
		$result += [bool]( 'Jimbo' -eq ( gi db:/mydb/1 ).username );
		
		Remove-PSDrive -Name db | Out-Null;
		
		$result | verify-all;
		remove-sqliteFiles
	}

	It "creates a new record using hashtable" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			username = "TEXT";
			age = "INTEGER";
		} | Out-Null;
		
		$result = @( Test-Path db:/mydb );
		
		New-Item -path db:/mydb -value @{ username = "Jimbo"; age= 38 }  | Out-Null;
		
		$result += ( Test-Path db:/mydb/1 );
		$result += [bool]( 1 -eq ( ls db:/mydb | measure | select -ExpandProperty count) );
		$result += [bool]( 'Jimbo' -eq ( gi db:/mydb/1 ).username );
		
		Remove-PSDrive -Name db | Out-Null;
		
		$result | verify-all;
		remove-sqliteFiles
	}

	It "updates an existing record using dynamic parameters" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			username = "TEXT";
			age = "INTEGER";
		} | Out-Null;
		
		$result = @( Test-Path db:/mydb );
		
		New-Item -path db:/mydb -username "Beth" -age 39 | Out-Null;
		
		$result += ( Test-Path db:/mydb/1 );
		$beth = Get-Item -Path db:/mydb/1;
		#$beth.age = 38;
		Set-Item -Path db:/mydb/1 -age 38;
		
		$result += ( 38 -eq ( Get-Item db:/mydb/1 ).age );
		
		Remove-PSDrive -Name db | Out-Null;
		
		$result | verify-all;
		remove-sqliteFiles
	}
	
	It "updates an existing record using dbnull value dynamic object" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			username = "TEXT";
			age = "INTEGER";
		} | Out-Null;
		
		$result = @( Test-Path db:/mydb );
		
		New-Item -path db:/mydb -username "Beth" -age 39 | Out-Null;
		
		$result += ( Test-Path db:/mydb/1 );
		$beth = Get-Item -Path db:/mydb/1;
		#$beth.age = 38;
		Set-Item -Path db:/mydb/1 -age $null;
		
		$result += ( [DBNull]::Value -eq ( Get-Item db:/mydb/1 ).age );
		
		Remove-PSDrive -Name db | Out-Null;
		
		$result | verify-all;
		remove-sqliteFiles
	}

	It "updates an existing record using datarow object" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			username = "TEXT";
			age = "INTEGER";
		} | Out-Null;
		
		$result = @( Test-Path db:/mydb );
		
		New-Item -path db:/mydb -username "Beth" -age 39 | Out-Null;
		
		$result += ( Test-Path db:/mydb/1 );
		$beth = Get-Item -Path db:/mydb/1;
		$beth.age = 38;
		Set-Item -Path db:/mydb/1 -value $beth;
		
		$result += ( 38 -eq ( Get-Item db:/mydb/1 ).age );
		
		Remove-PSDrive -Name db | Out-Null;
		
		$result | verify-all;
		remove-sqliteFiles
	}

	It "removes an existng record" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			username = "TEXT";
			age = "INTEGER";
		} | Out-Null;
		
		$result = @( Test-Path db:/mydb );
		
		New-Item -path db:/mydb -username "Beth" -age 39 | Out-Null;
		
		$result += ( Test-Path db:/mydb/1 );
		Remove-Item -Path db:/mydb/1;
		$result += -not( Test-Path db:/mydb/1 );
		
		Remove-PSDrive -Name db | Out-Null;
		
		$result | verify-all;
		remove-sqliteFiles
	}
	
	It "mounts a table as a drive" {
		$filename = get-sqliteFileName;
		New-PSDrive -PSProvider SQLite  -Name db -root "Data Source=$filename" | Out-Null;
		
		New-Item -Path db:/MyDb -Value @{
			id = "INTEGER PRIMARY KEY";
			username = "TEXT";
			age = "INTEGER";
		} | Out-Null;
		
		$result = @( Test-Path db:/mydb );
		
		New-Item -path db:/mydb -username "Beth" -age 39 | Out-Null;
		
		$result += ( Test-Path db:/mydb/1 );
		
		New-PSDrive -PSProvider SQLite  -Name dbt -root "[Data Source=$filename]\mydb" | Out-Null;
		$result += ( Test-Path dbt:/1 );
		
		$item = Get-Item dbt:/1;
		$result += $item.username -eq "Beth"
		
		Remove-PSDrive -Name db | Out-Null;
		Remove-PSDrive -Name dbt | Out-Null;
		
		$result | verify-all;
		remove-sqliteFiles
	}

	Remove-Module SQLite;
}