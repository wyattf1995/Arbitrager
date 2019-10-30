using eBayScraper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Arbitrager.Models.HttpeBayClient;

namespace Arbitrager.Models.HttpeBayClient
{
	public class eBayClient : IeBayClient
	{
		private readonly HttpClient _httpClient;

		public eBayClient(HttpClient httpClient)
		{
			_httpClient = httpClient ?? throw new NullReferenceException(nameof(httpClient));
		}

		public async Task<IEnumerable<Book>> GetBooks()
		{
			
			var results = await ExecuteParallelScrape();
			
			return results;
		}

		public async Task<List<Book>> ExecuteParallelScrape()
		{
			var productLinks = await GetProductLinksFromeBayAsync(eBayConstants.eBayBookUrl);
			List<Task<Book>> tasks = new List<Task<Book>>();
		
			foreach (string product in productLinks)
			{
				tasks.Add(ExtractDataFromProductUrlAsync(product));

			}
			var results = await Task.WhenAll(tasks);

			return new List<Book>(results);
		}

		//scrapes individual product URLs from an ebay category URL
		public async Task<List<string>> GetProductLinksFromeBayAsync(string urlToCheck)
		{
			var html = await _httpClient.GetStringAsync(urlToCheck);

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(html);

			var ProductListItems = htmlDocument.DocumentNode.Descendants("li")
				.Where(node => node.GetAttributeValue("class", "")
				.Contains("s-item")).ToList();

			List<string> productUrls = new List<string>();

			foreach (var ProductListItem in ProductListItems)
			{
				var urls = ProductListItem.Descendants("a").FirstOrDefault().GetAttributeValue("href", "");
				productUrls.Add(urls);
			}
			return productUrls;
		}

		//Extracts product info from each product URL
		public async Task<Book> ExtractDataFromProductUrlAsync(string productUrl)
		{
			var html = await _httpClient.GetStringAsync(productUrl);

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(html);

			Book product = new Book();

			//gets the user-listed product title and adds it to the ProductModel as a string
			var listingTitle = htmlDocument.DocumentNode.SelectSingleNode(xpath: "//span[@id='vi-lkhdr-itmTitl']").InnerText;
			product.ListingTitle = listingTitle;

			//gets the current bid price of the item and adds it to the ProductModel as a double
			var currentPrice = htmlDocument.DocumentNode.SelectSingleNode(xpath: "//span[@id='prcIsum_bidPrice']").GetAttributeValue("content", "");
			product.CurrentBid = Convert.ToDouble(currentPrice);

			//gets shipping cost and adds it to the ProductModel as a double
			try
			{
				var shippingCost = htmlDocument.DocumentNode.SelectSingleNode(xpath: "//span[@id='fshippingCost']").InnerText.RemoveNonNumeric();

				//catches special case of shipping being free
				if (shippingCost.ToLower() == "free")
				{
					product.ShippingPrice = 0;
				}
				else
				{
					product.ShippingPrice = Convert.ToDouble(shippingCost);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Could not discern shipping cost: '{e}'");
			}

			//gets item condition and adds it to the ProductModel as a string
			var itemCondition = htmlDocument.DocumentNode.SelectSingleNode(xpath: "//div[@itemprop='itemCondition']").InnerText;
			switch (itemCondition)
			{
				case "New":
					product.Condition = ConditionType.New;
					break;
				case "Like New":
					product.Condition = ConditionType.LikeNew;
					break;
				case "Very Good":
					product.Condition = ConditionType.VeryGood;
					break;
				case "Good":
					product.Condition = ConditionType.Good;
					break;
				case "Acceptable":
					product.Condition = ConditionType.Acceptable;
					break;
			}

			product.Url = productUrl;

			try
			{
				if (htmlDocument.DocumentNode.SelectSingleNode(xpath: "//h2[@itemProp='productID']").InnerText != null)
				{
					var isbn = htmlDocument.DocumentNode.SelectSingleNode(xpath: "//h2[@itemProp='productID']").InnerText;
					product.ISBN = Convert.ToInt64(isbn);
				}
			}
			catch (Exception e)
			{
				//Console.WriteLine($"The ISBN could not be retrieved:");
				//Console.WriteLine();
				//Console.WriteLine($"{e}");
			}

			return product;
		}
	}
	public static class MyExtensions
	{
		public static string RemoveNonNumeric(this string s)
		{
			//trims white space and $ sign
			return s.Trim().TrimStart('$');
		}

		public static string RemoveSpecialCharacters(this string s)
		{
			string result = Regex.Replace(s, @"[^\w\d]", "");

			return result;
		}
	}
}
