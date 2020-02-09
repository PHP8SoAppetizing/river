﻿using River.Internal;
using River.ShadowSocks;
using River.Socks;
using System;
using System.Collections.Generic;
using System.Text;

namespace River
{
	public static class RiverInit
	{
		public static void RegAll()
		{
			Resolver.RegisterSchema<SocksServer, Socks4ClientStream>("socks4");
			Resolver.RegisterSchema<SocksServer, Socks5ClientStream>("socks5");
			Resolver.RegisterSchema<ShadowSocksServer, ShadowSocksClientStream>("ss");
			Resolver.RegisterSchemaServer<SocksServer>("socks");
		}
	}
}
