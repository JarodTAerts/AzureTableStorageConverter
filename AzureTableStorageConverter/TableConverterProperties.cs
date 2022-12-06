using System;
namespace AzureTableStorageConverter
{
	public class TableConverterProperties : Attribute
	{
		public bool Ignore {get; set;}

        public TableConverterProperties(bool ignore)
		{
			this.Ignore = ignore;
		}
	}
}

