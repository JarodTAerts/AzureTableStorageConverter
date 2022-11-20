using System;
using System.ComponentModel.DataAnnotations;
using Azure.Data.Tables;

namespace AzureTableStorageConverterTests
{
	public abstract class TestsBase : IDisposable
	{
		private static TableClient tableClient;
        private static string tableName = "TestBooks";

        public TestsBase()
		{
            // Delete and recreate the test table
            tableClient = new TableClient("UseDevelopmentStorage=true", tableName);
            tableClient.Delete();
            tableClient.CreateIfNotExists();
        }

        public void Dispose()
		{
            tableClient.Delete();
        }
	}

    public class Book
    {
        [Required]
        public string PartitionKey { get; set; }
        [Required]
        public string RowKey { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Author { get; set; }
        public DateTime PublishedDate { get; set; }
        public bool IsBestSeller { get; set; }
        public List<Chapter> Chapters { get; set; }

        public override string ToString()
        {
            var chaptersString = this.Chapters != null ? string.Join(", ", this.Chapters) : string.Empty;

            return $"ParitionKey: {this.PartitionKey},\n\t" +
            $"RowKey: {this.RowKey}" +
            $"\n\tTitle: {Title}," +
            $"\n\tAuthor: {Author}," +
            $"\n\tPublishedDate: {this.PublishedDate}," +
            $"\n\tIsBestSeller: {this.IsBestSeller}" +
            $"\n\tChapters: {chaptersString}";
        }
    }

    public class Chapter
    {
        [Required]
        public string Title { get; set; }
        public int NumberOfPages { get; set; }

        public override string ToString()
        {
            return $"Title:{this.Title}-Pages:{this.NumberOfPages}";
        }
    }
}

