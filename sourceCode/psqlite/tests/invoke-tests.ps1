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

[CmdletBinding()]
param( 
	
	[Parameter()] 
	[string]
	# the pattern of fixtures to run
	$filter = "test-*.*"
	
)

$modules = @( 'Pester' );

Write-Verbose "importing module list $modules";
Import-Module $modules -global;

try
{
	. "./_testfunctions.ps1";
 		
	Invoke-Pester -filepattern $filter 
}
finally
{
	Remove-Module $modules;		
}