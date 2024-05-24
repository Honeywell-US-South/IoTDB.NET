﻿
# IoTDBdotNET Library
## Current Version: 1.0.0-beta

## Overview

IoTDBdotNET is optimized for C# applications, offering a lightweight alternative to traditional databases. IoTDBdotNET design based LiteDB for tables and TeaFiles for time series. 

## Versioning
-Version format of X.Y.Z (Major.Minor.Patch).
-X.Y.Z-beta (version under beta test).
-X.Y.Z-rc.1 (version under release candidate 1).
-X.Y.Z-rc.2 (version under release candidate 2).
-NuGet package release after rc 2.

## Goals

- Easy to use
- Light weight
- Quick and easy for IoT development and deployment

## Before You Continue

- This is beta release. 

## Installation

To use the IoTDBdotNET library in your project, follow these steps:

1. .NET environment compatible with C# .NET 7
2. Install IoTDBdotNET NuGet Package (not available until version 1.0.0).

## Quick Start

### Initializing the Database
IoTDB.NET use flat files to store the data. Make sure your application has write permission to the database path.

```csharp
using IoTDB.NET;

// Specify database name and path
var dbName = "MyIoTDatabase";
var dbPath = @"c:\temp";

// Create an instance of IoTData
var iotData = new IoTDatabase(dbName, dbPath);
```
This create an empty database in your c:\temp directory

![image](https://github.com/d42y/IoTDB.NET/assets/29101692/51d59c34-2c5f-4728-aa95-72769172b832)

### Using IoTDB Table
#### 1. Create a public class for your data structure
```csharp
public class Friend
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```
Above class is the model for your table.
All table must have an Id property. The Id property must be of type: Guid, int, or double

#### 2. Access the table
```csharp
static void Main(string[] args)
{
    // Specify database name and path
    var dbName = "MyIoTDatabase";
    var dbPath = @"c:\temp";

    // Create an instance of IoTData
    var iotData = new IoTDatabase(dbName, dbPath);

    // Create a table with class name as table name
    var friendTbl = iotData.Tables<Friend>();
}
```
This create an empty table in the Tables folder.

![image](https://github.com/d42y/IoTDB.NET/assets/29101692/b614ae68-d48d-4ab7-ab6d-936b70d22b06)

What if you also want to create a table called BestFriend?
Create another derived class from friend.

![image](https://github.com/d42y/IoTDB.NET/assets/29101692/3a3f650f-fd27-4328-98c1-0bd9c4cf7b0e)

#### 3. Find and create new record
```csharp
//check if database has friend name Bob
var friend = friendTbl.FindOne(x=>x.Name.Equals("bob", StringComparison.OrdinalIgnoreCase));
if (friend == null )
{
    //create new friend
    friend = new Friend() { Name = "Bob" };
    //insert friend to database
    var id = friendTbl.Insert(friend);
    if (id.IsNull)
    {
        Console.WriteLine("Failed to insert.");
        return;
    } 
}

//display record
Console.WriteLine($"Success: Id [{friend.Id}] Name [{friend.Name}]");
```

![image](https://github.com/d42y/IoTDB.NET/assets/29101692/0f78a0b8-4c37-4fc4-9aa8-67dc73e9fb12)


#### 4. Foreign Key
```csharp
public class Address
{
    public Guid Id { get; set; }
    [TableForeignKey(typeof(Friend), TableConstraint.Cascading, RelationshipOneTo.One, "Each friend only have one address." )]
    public Guid FriendId { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}
```
The address class reference the Friend table. 
IoTDB support FK constraint that allows you to cascade deletion and other actions. You can also set One to One or One to Many relationship.

```csharp
// Address table
var addressTbl = iotData.Tables<Address>();

//check if database has friend name Bob
var address = addressTbl.FindOne(x => x.FriendId == friend.Id);

if (address == null)
{
    //create new friend
    address = new Address() { 
        FriendId = friend.Id,
        Street = "123 Main St.",
        City = "Friend Town",
        State = "TX",
        ZipCode = "75001-0001"
    };
    try
    {
        //insert friend to database
        var id = addressTbl.Insert(address);
        if (id.IsNull)
        {
            Console.WriteLine("Failed to insert.");
            return;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error insert address: [{ex.Message}]");
        return;
    }
}

//display record
Console.WriteLine($"Success: Id [{address.Id}] FriendId [{friend.Id}] Street [{address.Street}]");
```

![image](https://github.com/d42y/IoTDB.NET/assets/29101692/c138ecb6-4afd-477f-bdd5-ac1e306e8eff)

#### 5. Foreign Key Constraint Error
IoTDB throw error for all contraint errors.
```csharp
//This throw exception becuase of One to One relationship. Only one address allow for each FK reference.
//[TableForeignKey(typeof(Friend), TableConstraint.Cascading, RelationshipOneTo.One, "Each friend only have one address." )]
//public Guid FriendId { get; set; }
var address2 = new Address()
{
    FriendId = friend.Id,
    Street = "789 ABC Street",
    City = "Friend Town",
    State = "TX",
    ZipCode = "75001-0001"
};
try
{
    //insert friend to database
    var id = addressTbl.Insert(address);
    if (id.IsNull)
    {
        Console.WriteLine("Failed to insert.");
        return;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error insert address: [{ex.Message}]");
    return;
}
```
![image](https://github.com/d42y/IoTDB.NET/assets/29101692/a68cb6a1-0d9e-415b-9799-083a0047c0f4)


### Initialize Database Tables
Database initialization is highly recomended for IoTDB application with FK. 
Initilize parent table first.
```csharp
iotData.Tables<Friend>();
iotData.Tables<Address>();
```

## Closing or Unloading IoTDB
Unloading or closing IoTDB is not necessary. The library handles closure and recovery automatically. However, if your program ends or crashes during a data write, any incomplete or unwritten data will be lost.


## Contributing

Creating a robust and user-friendly database equivalent library requires significant effort and contributions from the public. However, during the current initial beta testing phase, I do not plan to accept outside contributions yet.


## License
This library is licensed under the MIT License. This means you are free to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the software, provided you include the following conditions in your distribution:

1. A copy of the original MIT License and copyright notice must be included with the software.
2. The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose, and non-infringement. In no event shall the authors or copyright holders be liable for any claim, damages, or other liability, whether in an action of contract, tort or otherwise, arising from, out of, or in connection with the software or the use or other dealings in the software.


This permissive license encourages open and collaborative software development while providing protection for the original authors. For more details, please refer to the full MIT License text.

## Third-Party Licenses and Acknowledgments

This software includes and/or depends on the following third-party software component, which is subject to its own license:
- **LiteDb**:  A .NET NoSQL Document Store database in a single data file. License MIT: For specific license terms, please refer to the [LiteDB github](https://github.com/mbdavid/LiteDB/blob/master/LICENSE).
- **TeaFile**: TeaFile is used for efficient time series data storage and access. License MIT: For specific license terms, please refer to the [TeaFile github](https://github.com/discretelogics/TeaFiles.Net-Time-Series-Storage-in-Files/blob/master/LICENSE).

We express our gratitude to the contributors and maintainers of LiteDB and TeaFile for their work.

