﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.HttpWrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Test
{
	[TestClass]
	public class WrapHttpTests : TestClass
	{

		[TestMethod]
		public void Should_wrap_http()
		{

			var port = GetFreePort();
			var proxy = new HttpWrapServer("hw://chacha20:123test@0.0.0.0:" + port);

			var cli = new HttpWrapClientStream("chacha20", "123test", "localhost", port, Host, 80);
			TestConnction(cli, Host);

			cli.Dispose();
			proxy.Dispose();
		}

		[TestMethod]
		public void Should_wrap_http_mult_dispose()
		{
			for (var i = 0; i < 10; i++)
			{
				var port = GetFreePort();
				var proxy = new HttpWrapServer("hw://chacha20:123test@0.0.0.0:" + port);

				var cli = new HttpWrapClientStream("chacha20", "123test", "localhost", port, Host, 80);
				TestConnction(cli, Host);

				cli.Dispose();
				proxy.Dispose();
			}
		}

		[TestMethod]
		public void Should_wrap_http_mult_track()
		{
			for (var i = 0; i < 10; i++)
			{
				var port = GetFreePort();
				var proxy = new HttpWrapServer("hw://chacha20:123test@0.0.0.0:" + port).Track();

				var cli = new HttpWrapClientStream("chacha20", "123test", "localhost", port, Host, 80).Track();
				TestConnction(cli, Host);
			}
		}
	}
}
