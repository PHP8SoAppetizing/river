﻿using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Http;
using River.Socks;

namespace River.Test.Api
{
	[TestClass]
	public class ClientApiTests : TestClass
	{
		[TestMethod]
		public void Should_socks4_have_a_ctor_with_proxy_and_host()
		{
			var server = new DemoTcpServer().Track();
			var proxyPort = GetFreePort();
			var proxy = new SocksServer("socks://0.0.0.0:" + proxyPort).Track();
			var proxyClient = new Socks4ClientStream("127.0.0.1", proxyPort, "127.0.0.1", server.Port).Track();

			TestDemoServer(proxyClient);

		}

		[TestMethod]
		public void Should_socks5_have_a_ctor_with_proxy_and_host()
		{
			var server = new DemoTcpServer().Track();
			var proxyPort = GetFreePort();
			var proxy = new SocksServer("socks://0.0.0.0:" + proxyPort).Track();
			var proxyClient = new Socks5ClientStream("127.0.0.1", proxyPort, "127.0.0.1", server.Port).Track();

			TestDemoServer(proxyClient);
		}

		[TestMethod]
		public void Should_use_1080_as_a_default_socks_port_for_client()
		{
			var server = new DemoTcpServer().Track();
			var proxyPort = 1080;
			var proxy = new SocksServer("socks://0.0.0.0:" + proxyPort).Track();
			var proxyClient = new Socks5ClientStream().Track();
			proxyClient.Plug(new Uri("socks://127.0.0.1/")); // 1080 implied
			proxyClient.Route("127.0.0.1", server.Port);

			TestDemoServer(proxyClient);

			proxy.Dispose(); // force dispose of well-known port to avoid wars
		}

		[TestMethod]
		public void Should_use_1080_as_a_default_socks_port_for_server()
		{
			Assert.Inconclusive("not supported yet");
			var server = new DemoTcpServer().Track();
			var proxy = new SocksServer("socks://0.0.0.0").Track(); // 1080 implied
			var proxyClient = new Socks5ClientStream().Track();
			proxyClient.Plug(new Uri("socks://127.0.0.1:1080/"));
			proxyClient.Route("127.0.0.1", server.Port);

			TestDemoServer(proxyClient);

			proxy.Dispose(); // force dispose of well-known port to avoid wars
		}

		private static void TestDemoServer(Stream proxyClient)
		{
			var data = new byte[] { 1, 2, 3, 4 };
			proxyClient.Write(data);
			var buf = new byte[16 * 1024];
			var d = proxyClient.Read(buf, 0, buf.Length);

			Assert.AreEqual(4, d, "Should read 4 bytes in a single packet");
			// demo server is XOR 37
			CollectionAssert.AreEqual(data.Select(x => (byte)(x ^ 37)).ToArray(), buf.Take(d).ToArray());
		}

		[TestMethod]
		public void Should_socks4_have_a_plug_with_proxy_and_host()
		{
			var server = new DemoTcpServer().Track();
			var proxyPort = GetFreePort();
			var proxy = new SocksServer("socks://0.0.0.0:" + proxyPort).Track();
			var proxyClient = new Socks4ClientStream().Track();
			proxyClient.Plug("127.0.0.1", proxyPort);
			proxyClient.Route("127.0.0.1", server.Port);

			TestDemoServer(proxyClient);
		}

		[TestMethod]
		public void Should_socks5_have_a_plug_with_proxy_and_host()
		{
			var server = new DemoTcpServer().Track();
			var proxyPort = GetFreePort();
			var proxy = new SocksServer("socks://0.0.0.0:" + proxyPort).Track();
			var proxyClient = new Socks5ClientStream().Track();
			proxyClient.Plug("127.0.0.1", proxyPort);
			proxyClient.Route("127.0.0.1", server.Port);

			TestDemoServer(proxyClient);
		}

		[TestMethod]
		public void Should_http_have_a_ctor_with_proxy_and_host()
		{
			var proxyPort = GetFreePort();
			var proxy = new HttpProxyServer("http://0.0.0.0:" + proxyPort).Track();
			// this constructor performs plug only (connect is not sent, a client will send a request)
			var proxyClient = new HttpProxyClientStream("127.0.0.1", proxyPort).Track();

			TestConnction(proxyClient);
		}

		[TestMethod]
		public void Should_http_have_a_ctor_with_proxy_and_host_for_bin_protocol()
		{
			var server = new DemoTcpServer().Track();
			var proxyPort = GetFreePort();
			var proxy = new HttpProxyServer("http://0.0.0.0:" + proxyPort).Track();
			// this constructor performs plug & route (send connect verb)
			var proxyClient = new HttpProxyClientStream("127.0.0.1", proxyPort, "127.0.0.1", server.Port).Track();

			TestDemoServer(proxyClient);
		}

		[TestMethod]
		[ExpectedException(typeof(ConnectionClosingException))]
		[Timeout(6000)]
		public void Should_http_have_a_ctor_with_proxy_and_host_for_bin_protocol_error()
		{
			var proxyPort = GetFreePort();
			var proxy = new HttpProxyServer("http://0.0.0.0:" + proxyPort).Track();

			// this constructor performs plug & route (send connect verb)
			// BUT DESTINATION IS UNREACHABLE
			var proxyClient = new HttpProxyClientStream("127.0.0.1", proxyPort, "177.177.177.177", 177).Track();

			// buffer might require some data to actually proceed with route
			var data = new byte[] { 1, 2, 3, 4 };
			proxyClient.Write(data);
		}
	}
}
