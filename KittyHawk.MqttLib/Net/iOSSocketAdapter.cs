using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Plugins.Logging;
using CoreFoundation;
using Foundation;

namespace KittyHawk.MqttLib.Net
{
    internal class NSStreamPair : IDisposable
    {
        public NSInputStream Input { get; private set; }
        public NSOutputStream Output { get; private set; }

        public NSStreamPair(NSInputStream input, NSOutputStream output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Input.Dispose ();
            Output.Dispose ();

            Input = null;
            Output = null;
        }
    }

    internal class iOSSocketAdapter : ISocketAdapter
    {
        private readonly ILogger _logger;
        private readonly iOSSocketWorker _socketWorker;

        public iOSSocketAdapter(ILogger logger)
        {
            _logger = logger;
            _socketWorker = new iOSSocketWorker(_logger);
        }

        public bool IsEncrypted(string clientUid)
        {
            return _socketWorker.IsEncrypted(clientUid);
        }

        // ISocketAdapter helpers
        public bool IsConnected(string clientUid)
        {
            return _socketWorker.IsConnected(clientUid);
        }

        public void JoinDisconnect(string clientUid)
        {
            // No impl for Win32
        }

        public void ConnectAsync(string ipOrHost, int port, SocketEventArgs args)
        {
            CFReadStream cfRead;
            CFWriteStream cfWrite;

            CFStream.CreatePairWithSocketToHost(ipOrHost, port, out cfRead, out cfWrite);

            // Toll-Free binding from CFStream to a NSStream.
            var inStream = (NSInputStream)ObjCRuntime.Runtime.GetNSObject(cfRead.Handle);
            var outStream = (NSOutputStream)ObjCRuntime.Runtime.GetNSObject(cfWrite.Handle);
            var pair = new NSStreamPair (inStream, outStream);

            inStream.Schedule (_socketWorker.RunLoop, NSRunLoop.NSDefaultRunLoopMode);
            outStream.Schedule (_socketWorker.RunLoop, NSRunLoop.NSDefaultRunLoopMode);

            if (args.EncryptionLevel != SocketEncryption.None)
            {
                SetEncryptionOnStreams (args.EncryptionLevel, inStream, outStream);
            }

            var inReady = false;
            var outReady = false;

            Action complete = () =>
            {
                _socketWorker.ConnectTcpClient (pair, port, args.EncryptionLevel, args.ClientUid);
                args.Complete ();
            };

            EventHandler<NSStreamEventArgs> inReadyHandler = null;
            inReadyHandler = (_, e) =>
            {
                inStream.OnEvent -= inReadyHandler;
                inReady = true;

                if (inReady && outReady)
                    complete ();
            };

            EventHandler<NSStreamEventArgs> outReadyHandler = null;
            outReadyHandler = (_, e) =>
            {
                outStream.OnEvent -= outReadyHandler;
                outReady = true;

                if (inReady && outReady)
                    complete ();
            };

            inStream.OnEvent += inReadyHandler;
            outStream.OnEvent += outReadyHandler;

            inStream.Open ();
            outStream.Open ();
        }

        public void WriteAsync(SocketEventArgs args)
        {
            _socketWorker.WriteAsync(args);
        }

        public void Dispose()
        {
            _socketWorker.DisconnectAll();
            _socketWorker.Dispose();
        }

        public void Disconnect(string clientUid)
        {
            _socketWorker.Disconnect(clientUid);
            JoinDisconnect(clientUid);
        }

        public void OnMessageReceived(NetworkReceiverEventHandler handler)
        {
            _socketWorker.OnMessageReceived(handler);
        }

        private void SetEncryptionOnStreams(SocketEncryption encryption, NSInputStream inStream, NSOutputStream outStream)
        {
            if (encryption == SocketEncryption.Ssl)
            {
                inStream.SocketSecurityLevel = NSStreamSocketSecurityLevel.SslV3;
                outStream.SocketSecurityLevel = NSStreamSocketSecurityLevel.SslV3;
            }
            else if (encryption == SocketEncryption.Tls10)
            {
                inStream.SocketSecurityLevel = NSStreamSocketSecurityLevel.TlsV1;
                outStream.SocketSecurityLevel = NSStreamSocketSecurityLevel.TlsV1;
            }
            else if (encryption == SocketEncryption.Tls11)
            {
                inStream.SocketSecurityLevel = NSStreamSocketSecurityLevel.TlsV1;
                outStream.SocketSecurityLevel = NSStreamSocketSecurityLevel.TlsV1;
            }
            else if (encryption == SocketEncryption.Tls12)
            {
                inStream.SocketSecurityLevel = NSStreamSocketSecurityLevel.TlsV1;
                outStream.SocketSecurityLevel = NSStreamSocketSecurityLevel.TlsV1;

                //var key = ObjCRuntime.Dlfcn.GetStringConstant("kCFStreamPropertySSLSettings", Libraries.CoreFoundation.Handle);
            }
        }
    }
}
