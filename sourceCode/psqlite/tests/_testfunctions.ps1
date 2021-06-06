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

function global:new-guid()
{
	return [Guid]::NewGuid().ToString( "N" );
}

function global:assert-true( $script, $msg )
{	
	if( ( & $script ) -ne $true )
	{
		throw $msg;
	}
}

function global:test-exception( $script )
{
	try
	{
		Invoke-Command -scriptblock $script -ErrorAction SilentlyContinue | out-null;
		#& $script  | out-null;
	}
	catch
	{
		#Write-Verbose $_;
		return $true;		
	}
	return $false;
}

function global:test-noException( $script )
{
	try
	{
		Write-Verbose "testing {$script} for no exceptions";
		invoke-command -ScriptBlock $script | out-null;
		#& $script | Out-Null;
		Write-Verbose "testing {$script} for no exceptions complete";
	}
	catch
	{
		Write-verbose "Unexpected Exception: $_";
		$VerbosePreference = 'silentlycontinue';
		return $false;		
	}

	return $true;
}

function global:test-error( $script )
{
	$er = $null;
	invoke-Command -ScriptBlock $script -ErrorVariable er -ErrorAction SilentlyContinue | out-null;
	
	return ( $er -ne $null );
}

function global:test-noError( $script )
{
	$er = $null;
	invoke-Command -ScriptBlock $script -ErrorVariable er -ErrorAction SilentlyContinue | out-null;
	
	return ( $er -eq $null );
}

function global:reset-moduleState( $fixture, $module )
{
	if( get-module $module )
	{
		Write-Debug "removing $module module"
		Remove-Module $module
	}
	
	Import-Module $module;
}

function global:verify-all( $value = $true )
{
	begin
	{
		$local:a = @();
	}
	process
	{
		$local:a += $input;
	}	
	end
	{
		if( $value -and -not $local:a )
		{
			return $false;
		}
		
		foreach( $aa in $local:a )
		{
			if( $aa -ne $value )
			{
				return $false;
			}
		}
		return $true;
	}
}

function global:verify-results
{
	[CmdletBinding()]
	param(
		[Parameter(ValueFromPipeline=$true)] $results, 
		[Parameter()]
		[String[]] $fields
	);
	
	process
	{
		if( -not $results )
		{
			Write-Debug 'no results to process';
			return $false;
		}
		
		Write-Verbose 'obtaining list of result properties';
		$local:resultFields = $results | Get-Member -membertype properties | foreach{ $_.name };
		
		Write-Verbose 'obtaining list of missing properties';
		if( $fields | where{ $local:resultFields -notcontains $_ } )
		{
			return $false;
		}
		
		return $true;
	}
}

function global:compare-objectProperties( $a, $b )
{
	if( -not( $a -and $b ) )
	{
		return $false;
	}
	
	$local:scNames = $a | get-member -membertype Properties | foreach{ $_.Name };
	$local:scNames | Write-Debug;
	
	$b | Get-Member -MemberType Properties | foreach {
		$local:key = $_.name;
		Write-Debug "processing $($_.name)";
		$local:result = $local:scNames -contains $local:key; 
		if( -not $local:result )
		{
			Write-Debug "$local:key is not in list of property names";
			$false;
		}
		else
		{
			write-debug ($b."$local:key" -eq $a."$local:key")
			$b."$local:key" -eq $a."$local:key";
		}
	};
}