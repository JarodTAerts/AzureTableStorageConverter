# AzureTableStorageConverter
This is a nuget package project which is intended to make storing object data in Azure Table Storage seamless. It is as easy as just passing your object files to be added and retrieved from Azure Tables. The columns, types, etc will be automatically discovered and set for you so you can do away with all the boilerplate of storing things in a database and get to the meat and potatoes of your app.

_Note: I think its important to note that Azure Table Storage is not the fastest, most reliable or best way to do database storage. It is very cheap though, which is a huge plus for smaller projects where speed and scaleability are less important. If you are in a situation where you need scaleability and or speed, I would recommend using Cosmos DB or even a full fledged SQL DB. The APIs for Azure Table Storage and Cosmos are very similiar so it is likely this package could be modified to support both, but I will refrain from doing that until I have a need._

## Examples
To use the package you must first install it to your project and then you can create an instance of an Azure Table with the object of your choosing. The object that you use with the TableInterface must have at the very least a PartitionKey and RowKey property. And example object is below:

```cs
class Animal 
{
    public string ParitionKey { get; set; }
    public string RowKey { get; set; }
    public string Name { get; get; }
    public bool IsFriendOfMan { get; set; }
}

string connectionString = "{your-azure-storage connection string}";
string tableName = "{table-name you will use for this table}";

TableInterface<Animal> animalsTable = new TableInterface<Animal>(connectionString, tableName);
```

You can instansiate these TableInterface objects on the fly as they are pretty lightweight, but it might also make sense to create static instances of them in a static helper class or something like that.

Once you have your TableInterface, you can use it in several ways. It should be noted that these are not async calls. I will probably add that in the future, but didn't have a need for it in the inital implementation:
```cs
// When creating a new object we do not need to set the partitionkey and rowkey. They will automatically set with the following pattern: PartitionKey="yyyy-mm", RowKey="{guid}". Where yyyy-mm is the year/month the record is being added on. If you want to manually set the partition and row keys in a way that works better for your solution you can do that when creating the object.
Animal animal = new Animal { Name = "Dog", IsFriendOfMan=true};

// This will return the object with the parition and row keys
Animal addedAnimal = animalsTable.Add(animal);

List<Animal> allAnimals = animalsTable.GetAll();

// Parition and Row keys must be set on an object before updating. Always good to only do update calls with objects that were previously GOTten from the table.
addedAnimal.Name = "Doodle";
Animal updatedAnimal = animalsTable.Update(addedAnimal);

// We can also get a specific instance by reference of parition and row keys
Animal doodleDog = animalsTable.Get(updatedAnimal.PartitionKey, updatedAnimal.RowKey);

// Finally we can delete an object from table storage with the same parition/row key combo
animalsTable.Delete(updatedAnimal.PartitionKey, updatedAnimal.RowKey);
```