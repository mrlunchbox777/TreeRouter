﻿using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Controllers;
using TreeRouter;
using TreeRouter.Http;

namespace Tests
{
	public class Base
	{
		protected Router _router;
		private IServiceProvider _services;

		protected Base()
		{
			var services = new ServiceCollection();
			services.AddTransient<EchoController>();
			services.AddTransient<ResourcesController>();
			_router = new Router();
			services.AddSingleton<IRouter>(_router);
			_services = services.BuildServiceProvider();
		}
		
		protected static HttpContext MakeContext(string path, string method)
		{
			var request = new Mock<HttpRequest>();
			request.Setup(x => x.Path).Returns(path);
			request.Setup(x => x.Method).Returns(method);

			var response = new Mock<HttpResponse>();
			var ms = new MemoryStream();
			response.Setup(x => x.Body).Returns(ms);

			var context = new Mock<HttpContext>();
			context.Setup(x => x.Request).Returns(request.Object);
			context.Setup(x => x.Response).Returns(response.Object);

			return context.Object;
		}

		protected string DispatchAndRead(HttpContext context)
		{
			try
			{
				var middleware = new TreeRouter.Http.Middleware(null, _router, _services.GetService<IServiceScopeFactory>());
				middleware.Invoke(context).Wait();
			}
			catch (AggregateException e)
			{
				ExceptionDispatchInfo.Capture(e.InnerExceptions.First()).Throw();
			}
			
			var memStream = context.Response.Body;
			memStream.Position = 0;
			return new StreamReader(memStream).ReadToEnd();
		}

		protected string DispatchAndRead(string path, string method)
		{
			var context = MakeContext(path, method);
			return DispatchAndRead(context);
		}
	}
}