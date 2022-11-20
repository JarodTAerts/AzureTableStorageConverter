using Azure.Data.Tables;
using AzureTableStorageConverter;

namespace AzureTableStorageConverterTests;
public class ObjectToTableTests : TestsBase
{
    [Fact]
    public void FullBook_Converts()
    {
        var testBook = new Book
        {
            Title = "Grant",
            Author = "Ron Chernow",
            PublishedDate = DateTime.Parse("October 10, 2017"),
            IsBestSeller = false,
            Chapters = new List<Chapter> {
                    new Chapter {Title = "Chapter 1", NumberOfPages=10},
                    new Chapter {Title = "Chapter 2", NumberOfPages=14}
                }
        };

        var bookTableEntity = TableEntityConverter.ConvertObjectToTableEntity(testBook);
    }
}
