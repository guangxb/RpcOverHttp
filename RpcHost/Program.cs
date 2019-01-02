﻿using RpcOverHttp;
using RpcOverHttp.Serialization;
using RpcServiceCollection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RpcOverHttp.WebHost;

namespace RpcHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var url = ConfigurationManager.AppSettings["urlPrefix"];
            RpcServer server = new RpcServer();
            server.Register<IRpcServiceSample, RpcServiceSample>();
            server.Start(url);
            Console.ReadLine();
        }
    }

    public class RpcWebHostHttpModule : RpcServerHttpModule
    {
        public RpcWebHostHttpModule()
        {
        }
        public override void InitRpcServer(IRpcServer server)
        {
            server.Register<IRpcServiceSample, RpcServiceSample>();
        }
    }
}
