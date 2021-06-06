# SQLite
The SQLite PowerShell Provider allows you to mount a SQLite database as a drive in your PowerShell session, enabling access to tables and data as if they were files on your hard drive. This is a great mechanism for persisting or sharing configuration or state between PowerShell sessions. SQLite is a powerful, lightweight, and open-source relational database. See http://www.sqlite.org for more information about SQLite and its features.

## Overview
The SQLite PowerShell Provider enables you to use SQLite databases from your PowerShell session by mounting the database as a drive. You can then use the standard provider cmdlets to perform CRUD operations on the database tables and records.

The provider supports both persistent (on-disk) and transient (memory-only) SQLite databases. In addition, the provider is transaction-aware.

For more information and examples, please refer to the User's Guide in the Documentation.


## Summary
Below is a summary of SQLite PowerShell Provider operations:

PS> import-module SQLite
PS> new-psdrive -name db -psp SQLite -root "Data Source=data.sqlite"

Name Provider Root
---- -------- ----
db   SQLite   [Data Source=data.sqlite]


PS> new-item db:/MyTable -id integer primary key -username text -userid integer not null


PSPath           : SQLite::[Data Source=data.sqlite]\MyTable
PSParentPath     : SQLite::[Data Source=data.sqlite]
PSChildName      : MyTable
PSDrive          : db
PSProvider       : SQLite
PSIsContainer    : True
TABLE_CATALOG    : main
TABLE_SCHEMA     :
TABLE_NAME       : MyTable
TABLE_TYPE       : table
TABLE_ID         : 1
TABLE_ROOTPAGE   : 2
TABLE_DEFINITION : CREATE TABLE MyTable ( id integer primary key, username text, userid integer not null )



PS> new-item db:/MyTable -username 'beef' -userid 1


PSPath        : SQLite::[Data Source=data.sqlite]\MyTable\1
PSParentPath  : SQLite::[Data Source=data.sqlite]\MyTable
PSChildName   : 1
PSDrive       : db
PSProvider    : SQLite
PSIsContainer : False
id            : 1
username      : beef
userid        : 1



PS> 2..200 | new-item db:/MyTable -username { "User$_" } -userid {$_}


PSPath        : SQLite::[Data Source=data.sqlite]\MyTable\2
PSParentPath  : SQLite::[Data Source=data.sqlite]\MyTable
PSChildName   : 2
PSDrive       : db
PSProvider    : SQLite
PSIsContainer : False
id            : 2
username      : User2
userid        : 2

PSPath        : SQLite::[Data Source=data.sqlite]\MyTable\3
PSParentPath  : SQLite::[Data Source=data.sqlite]\MyTable
PSChildName   : 3
PSDrive       : db
PSProvider    : SQLite
PSIsContainer : False
id            : 3
username      : User3
userid        : 3

# ...

PSPath        : SQLite::[Data Source=data.sqlite]\MyTable\200
PSParentPath  : SQLite::[Data Source=data.sqlite]\MyTable
PSChildName   : 200
PSDrive       : db
PSProvider    : SQLite
PSIsContainer : False
id            : 200
username      : User200
userid        : 200



PS> ls db:/MyTable | select username, id

username     id
--------     --
beef         1
User2        2
# ...
User200      200


PS> ls db:/MyTable -filter "username='beef'" | select username, id

username     id
--------     --
beef         1


PS> set-item db:/MyTable -filter "username='beef'" -value @{ username='jimbo' }
PS> ls db:/MyTable -filter "id=1" | select username, id

username     id
--------     --
jimbo        1
