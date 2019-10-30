using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arbitrager.Models.HttpeBayClient
{
	public class eBayClientFactory
	{
		private readonly IServiceProvider _serviceProvider;

		public eBayClientFactory(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public eBayClient Create()
		{
			return _serviceProvider.GetRequiredService<eBayClient>();
		}
	}
}
