﻿using Newtonsoft.Json.Linq;
using RpcOverHttp.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public class IocContainerWrapper
    {
        internal TinyIoC.TinyIoCContainer iocContainer;
        public IocContainerWrapper(TinyIoC.TinyIoCContainer iocContainer)
        {
            this.iocContainer = iocContainer;
        }
        /// <summary>
        /// 注册自定义类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Register<T>() where T : class
        {
            this.iocContainer.Register<T>();
        }

        /// <summary>
        /// 注册自定义类型实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public void Register<T>(T instance) where T : class
        {
            this.iocContainer.Register<T>(instance);
        }

        /// <summary>
        /// 注册自定义类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Register<T, TImplementation>() where T : class where TImplementation : class, T
        {
            this.iocContainer.Register<T, TImplementation>();
        }

        /// <summary>
        /// 注册自定义类型实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="instance"></param>
        public void Register<T, TImplementation>(TImplementation instance) where T : class where TImplementation : class, T
        {
            this.iocContainer.Register<T, TImplementation>(instance);
        }
    }
    /// <summary>
    /// use RpcClient.Initialize to create a rpc client.
    /// </summary>
    public class RpcClient : IocContainerWrapper
    {
        private RpcDynamicProxyFactory _proxyFactory;
        /// <summary>
        /// 实例化接口代理对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Rpc<T>()
        {
            return _proxyFactory.GetProxy<T>();
        }

        /// <summary>
        /// 实例化接口代理对象,使用鉴权的token
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="token"></param>
        /// <returns></returns>
        public T Rpc<T>(string token)
        {
            return _proxyFactory.GetProxy<T>(token);
        }

        public Server.IRpcServiceAdministration Administration { get; private set; }
        /// <summary>
        /// HTTPS
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cerFilePath"></param>
        public static RpcClient Initialize(string url, string cerFilePath = null, WebProxy proxy = null)
        {
            return Initialize(new Uri(url), cerFilePath, proxy);
        }

        /// <summary>
        /// 启用管理功能，启用后可以使用RpcClient.Administration获取接口
        /// </summary>
        /// <param name="token"></param>
        public void EnableAdministration(string token)
        {
            if (this.Administration == null)
                this.Administration = this.Rpc<Server.IRpcServiceAdministration>(token);
        }

        /// <summary>
        /// HTTP ONLY
        /// </summary>
        /// <param name="url"></param>
        public static RpcClient Initialize(Uri url, string cerFilePath = null, WebProxy proxy = null)
        {
            if (!string.IsNullOrEmpty(cerFilePath))
            {
                if (!System.IO.File.Exists(cerFilePath))
                    throw new Exception("the cer file you provided is not exists. " + Path.GetFileName(cerFilePath));
                AddCertForHttps(cerFilePath);
            }
            if (url.Scheme == ("https") && !Certs.Any())
            {
                throw new Exception("you should provide a cert when using https.");
            }
            var iocContainer = new TinyIoC.TinyIoCContainer();
            iocContainer.Register<IRpcDataSerializer, ProtoBufRpcDataSerializer>(new ProtoBufRpcDataSerializer(), "default");
            iocContainer.Register<IRpcHeadSerializer, JsonRpcHeadSerializer>(new JsonRpcHeadSerializer(), "default");
            var _proxyFactory = new RpcDynamicProxyFactory(url, proxy, iocContainer);
            var client = new RpcClient(_proxyFactory, proxy, iocContainer);
            return client;
        }

        internal static List<byte[]> Certs = new List<byte[]>();
        private WebProxy _webProxy;

        internal RpcClient(RpcDynamicProxyFactory _proxyFactory, WebProxy proxy, TinyIoC.TinyIoCContainer iocContainer) : base(iocContainer)
        {
            this._proxyFactory = _proxyFactory;
            this._webProxy = proxy;
            this.iocContainer = iocContainer;
            ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
        }
        bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return Certs.Any(x => x.SequenceEqual(certificate.GetRawCertData()));
        }

        static void AddCertForHttps(string fileName)
        {
            try
            {
                var cert = new X509Certificate2(fileName);
                var pk = cert.GetRawCertData();
                if (Certs.Any(x => x.SequenceEqual(pk)))
                {
                    return;
                }
                Certs.Add(pk);
            }
            catch (Exception ex)
            {
                throw new Exception("error to read the cert file. " + ex.Message);
            }
        }
    }
}
