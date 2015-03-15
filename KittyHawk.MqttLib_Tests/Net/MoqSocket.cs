using System.Collections.Generic;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Net;

namespace KittyHawk.MqttLib_Tests.Net
{
    internal class MoqSocket : ISocketAdapter
    {
        private bool _isConnected = false;
        public List<MessageType> SentMessages = new List<MessageType>();
        public bool DoNotRespond { get; set; }

        public bool IsEncrypted(string clientUid)
        {
            return false;
        }

        public void ConnectAsync(string ipAddress, int port, SocketEventArgs args)
        {
            _isConnected = true;
            args.Complete();
        }

        public void WriteAsync(SocketEventArgs args)
        {
            SentMessages.Add(args.MessageToSend.MessageType);
            args.Complete();

            // Mock a server that does not send appropriate response
            if (DoNotRespond)
            {
                return;
            }

            // This section mocks the expected response behavior from an MQTT broker
            switch (args.MessageToSend.MessageType)
            {
                case MessageType.Connect:
                    var conAck = new MqttConnectAckMessageBuilder();
                    MessageReceived(new MqttNetEventArgs
                    {
                        Message = conAck.GetMessage(),
                        ClientUid = args.ClientUid
                    });
                    break;

                case MessageType.Subscribe:
                    var subMsg = args.MessageToSend as IMqttIdMessage;
                    var subAck = new MqttSubscribeAckMessageBuilder
                    {
                        MessageId = subMsg.MessageId
                   };
                    MessageReceived(new MqttNetEventArgs
                    {
                        Message = subAck.GetMessage(),
                        ClientUid = args.ClientUid
                    });
                    break;

                case MessageType.Unsubscribe:
                    var unsubMsg = args.MessageToSend as IMqttIdMessage;
                    var unsubAck = new MqttUnsubscribeAckMessageBuilder
                    {
                        MessageId = unsubMsg.MessageId
                    };
                    MessageReceived(new MqttNetEventArgs
                    {
                        Message = unsubAck.GetMessage(),
                        ClientUid = args.ClientUid
                    });
                    break;

                case MessageType.PingReq:
                    var pingResp = new MqttPingResponseMessageBuilder();
                    MessageReceived(new MqttNetEventArgs
                    {
                        Message = pingResp.GetMessage(),
                        ClientUid = args.ClientUid
                    });
                    break;

                case MessageType.Publish:
                    var publishMsg = args.MessageToSend as IMqttIdMessage;
                    // Mock publish response behavior
                    if (args.MessageToSend.QualityOfService == QualityOfService.AtLeastOnce)
                    {
                        var msgRcv = new MqttPublishAckMessageBuilder
                        {
                            MessageId = publishMsg.MessageId
                        };
                        MessageReceived(new MqttNetEventArgs
                        {
                            Message = msgRcv.GetMessage(),
                            ClientUid = args.ClientUid
                        });
                    }
                    else if (args.MessageToSend.QualityOfService == QualityOfService.ExactlyOnce)
                    {
                        var msgRcv = new MqttPublishReceivedMessageBuilder()
                        {
                            MessageId = publishMsg.MessageId
                        };
                        MessageReceived(new MqttNetEventArgs
                        {
                            Message = msgRcv.GetMessage(),
                            ClientUid = args.ClientUid
                        });
                    }
                    break;

                case MessageType.PubRec:
                    var pubRec = args.MessageToSend as IMqttIdMessage;
                    var pubRel1 = new MqttPublishReleaseMessageBuilder
                    {
                        MessageId = pubRec.MessageId
                    };
                    MessageReceived(new MqttNetEventArgs
                    {
                        Message = pubRel1.GetMessage(),
                        ClientUid = args.ClientUid
                    });
                    break;

                case MessageType.PubRel:
                    var pubRel2 = args.MessageToSend as IMqttIdMessage;
                    var pubComp = new MqttPublishCompleteMessageBuilder
                    {
                        MessageId = pubRel2.MessageId
                    };
                    MessageReceived(new MqttNetEventArgs
                    {
                        Message = pubComp.GetMessage(),
                        ClientUid = args.ClientUid
                    });
                    break;
            }
        }

        private NetworkReceiverEventHandler _messageReceivedHandler;
        public void OnMessageReceived(NetworkReceiverEventHandler handler)
        {
            _messageReceivedHandler = handler;
        }

        public bool IsConnected(string clientUid)
        {
            return _isConnected;
        }

        public void JoinDisconnect(string clientUid)
        {
            return;
        }

        public void Disconnect(string clientUid)
        {
            _isConnected = false;
        }

        private void MessageReceived(MqttNetEventArgs args)
        {
            _messageReceivedHandler(args);
        }

        public void Dispose()
        {
        }

        public void ReceiveMessage(IMqttMessageBuilder mb)
        {
            MessageReceived(new MqttNetEventArgs
            {
                Message = mb.GetMessage()
            });
        }
    }
}
