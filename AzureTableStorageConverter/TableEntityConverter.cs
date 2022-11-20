using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Azure.Data.Tables;

namespace AzureTableStorageConverter;
public class TableEntityConverter
{
    private static string _partitionKey = "PartitionKey";
    private static string _rowKey = "RowKey";

    public static TableEntity ConvertObjectToTableEntity(object obj)
    {
        if (obj == null)
        {
            throw new Exception("Passed in object cannot be null.");
        }

        var type = obj.GetType();
        var typeProperties = type.GetProperties();
        ValidatePartitionAndRowKeys(typeProperties, type.Name);

        TableEntity returnTableEntity = new TableEntity();

        foreach (var prop in type.GetProperties())
        {
            if (prop.Name.Equals(_partitionKey) && prop.GetValue(obj) == null)
            {
                // PartitionKey is not set so we assume this is a first time create for this table entity and willset it with YYYY-MM
                returnTableEntity.PartitionKey = $"{DateTime.Now.Year}-{DateTime.Now.Month}";
                continue;
            }
            else if (prop.Name.Equals(_rowKey) && prop.GetValue(obj) == null)
            {
                // RowKey is not set so we assume this is a first time create for this table entity and willset it with a guid
                returnTableEntity.RowKey = Guid.NewGuid().ToString();
                continue;
            }

            var propValue = prop.GetValue(obj);
            CheckRequiredProperty(propValue, prop, type.ToString());

            if (propValue != null)
            {
                if (prop.PropertyType == typeof(DateTime))
                {
                    // Convert datetime to UTC if it is not already. This is required by Azure
                    DateTime utcDateTime = ((DateTime)propValue).ToUniversalTime();
                    DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                    propValue = utcDateTime;
                }

                if (!IsAzureTableDataType(prop.PropertyType))
                {
                    // If this is not one of the Azure tables supported types then we will serialize it into json to be stored there
                    propValue = JsonSerializer.Serialize(propValue);
                }

                returnTableEntity.Add(prop.Name.ToLower(), propValue);
            }
        }

        return returnTableEntity;
    }

    public static T? ConvertTableEntityToObject<T>(TableEntity tableEntity)
    {
        var type = typeof(T);
        T returnObj = (T)Activator.CreateInstance(typeof(T));
        var typeProperties = type.GetProperties();
        ValidatePartitionAndRowKeys(typeProperties, type.Name);

        foreach (var prop in typeProperties)
        {
            if (prop.Name.Equals(_partitionKey))
            {
                // PartitionKey is not set so we assume this is a first time create for this table entity and willset it with YYYY-MM
                prop.SetValue(returnObj, tableEntity.PartitionKey);
                continue;
            }
            else if (prop.Name.Equals(_rowKey))
            {
                // RowKey is not set so we assume this is a first time create for this table entity and willset it with a guid
                prop.SetValue(returnObj, tableEntity.RowKey);
                continue;
            }

            var propValueFromTable = GetObjectFromTableColumn(tableEntity, prop);

            CheckRequiredProperty(propValueFromTable, prop, objectType: typeof(T).ToString());
            prop.SetValue(returnObj, propValueFromTable);
        }

        return returnObj;
    }

    private static void ValidatePartitionAndRowKeys(PropertyInfo[] typeProperties, string typeName = "")
    {
        if (!typeProperties.Any(prop => prop.Name.Equals(_partitionKey)) ||
            !typeProperties.Any(prop => prop.Name.Equals(_rowKey)))
        {
            throw new Exception($"Type {typeName} does not have {_partitionKey} and {_rowKey} properties, which are required for this conversion.");
        }
    }

    private static bool IsAzureTableDataType(Type type)
    {
        return type == typeof(BinaryData) || type == typeof(bool) || type == typeof(DateTime)
            || type == typeof(double) || type == typeof(Guid) || type == typeof(int)
            || type == typeof(long) || type == typeof(string);
    }

    private static void CheckRequiredProperty(object propValue, PropertyInfo prop, string objectType = "")
    {
        var propIsRequired = Attribute.IsDefined(prop, typeof(RequiredAttribute));
        if (propValue == null && propIsRequired)
        {
            // Property is required on the target object but value from the table is null or empty
            throw new Exception($"Required property {prop.Name} on type {objectType} is set to null in the database value.");
        }
    }

    private static object? GetObjectFromTableColumn(TableEntity tableEntity, System.Reflection.PropertyInfo prop)
    {
        string columnName = prop.Name.ToLower();
        if (!tableEntity.ContainsKey(columnName))
        {
            return null;
        }

        var propType = prop.PropertyType;

        if (propType == typeof(string))
        {
            return tableEntity.GetString(columnName);
        }
        else if (propType == typeof(int))
        {
            return tableEntity.GetInt32(columnName);
        }
        else if (propType == typeof(double))
        {
            return tableEntity.GetDouble(columnName);
        }
        else if (propType == typeof(bool))
        {
            return tableEntity.GetBoolean(columnName);
        }
        else if (propType == typeof(DateTime))
        {
            return tableEntity.GetDateTime(columnName);
        }
        else
        {
            try
            {
                // Assume we stored an object as json string
                var jsonString = tableEntity.GetString(columnName);
                MethodInfo castIntoMethod = typeof(TableEntityConverter).GetMethod("DeserializeJson").MakeGenericMethod(propType);
                var convertedObj = castIntoMethod.Invoke(null, new[] { jsonString });

                return convertedObj;
            }
            catch (Exception e)
            {
                // Failed to deserilize. Most likely that json was not correct for the property type
                throw new Exception($"[[GetObjectFromTableColumn]]:: Failed to deserialized column ({columnName}) value into type {propType}. {e}");
            }
        }
    }

    public static T? DeserializeJson<T>(string jsonString)
    {
        return JsonSerializer.Deserialize<T>(jsonString);
    }
}

