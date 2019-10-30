using eBayScraper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arbitrager.Models
{
	public class ProgressReport
	{
		public int PercentageComplete { get; set; } = 0;
		public List<Book> BooksScraped { get; set; } = new List<Book>();
	}
}
