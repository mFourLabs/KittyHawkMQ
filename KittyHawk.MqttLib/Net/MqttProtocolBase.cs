
using System;
using KittyHawk.MqttLib.Exceptions;
using KittyHawk.MqttLib.Plugins.Logging;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;

namespace KittyHawk.MqttLib.Net
{
    internal abstract class MqttProtocolBase : IDisposable
    {
        protected readonly ISocketAdapter Socket;
        protected readonly ILogger Logger;

        // Used to save event data with each message and tie it back to the event when a response is received
        private readonly MessageStore _messageStore = new MessageStore();

        public event MqttCommunicationEventHandler ConnectComplete;
        public event MqttCommunicationEventHandler SubscribeComplete;
        public event MqttCommunicationEventHandler SendMessageComplete;
        public event MqttCommunicationEventHandler PublishReceived;
        public event MqttMessageEventHandler NetworkError;

        protected MqttProtocolBase(ILogger logger, ISocketAdapter socket)
        {
            Logger = logger;
            Socket = socket;
            Socket.OnMessageReceived(SocketMessageReceived);

            _messageStore.MessageTimeout += MessageStoreOnMessageTimeout;
        }

        public bool IsConnected(string clientUid)
        {
            return Socket.IsConnected(clientUid);
        }

        public void ConnectAsync(MqttConnectMessageBuilder bldr, string ipOrHost, int port, SocketEncryption encryption, object eventData)
        {
            var args = new SocketEventArgs
            {
                EncryptionLevel = encryption,
                ClientUid = GenerateClientUid(bldr)
            };

            args.OnOperationComplete((eventArgs) =>
            {
                OnTcpConnectAsyncCompleted(eventArgs, eventData);

                if (eventArgs.SocketException == null)
                {
                    SendMessageAsync(bldr, eventData, eventArgs.ClientUid);
                }
                else
                {
                    FireConnectComplete(new MqttNetEventArgs
                    {
                        Message = bldr.GetMessage(),
                        Exception = eventArgs.SocketException,
                        AdditionalErrorInfo = eventArgs.AdditionalErrorInfo,
                        EventData = eventData,
                        ClientUid = args.ClientUid
                    });
                }
            });

            Socket.ConnectAsync(ipOrHost, port, args);
        }

        /// <summary>
        /// Let derived classes handle the ConnectAsyncComplete callback.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <param name="eventData"></param>
        /// <returns></returns>
        protected virtual void OnTcpConnectAsyncCompleted(SocketEventArgs eventArgs, object eventData)
        {
        }

        public void DisconnectAsync(object eventData, string clientUid)
        {
            SendMessageAsync(new MqttDisconnectMessageBuilder(), eventData, clientUid);
        }

        internal void SendMessageAsync(IMqttMessageBuilder bldr, object eventData, string clientUid)
        {
            SendMessageAsync(bldr.GetMessage(), eventData, clientUid);
        }

        public void SendMessageAsync(IMqttMessage msg, object eventData, string clientUid)
        {
            Logger.LogMessage("Protocol", LogLevel.Verbose, "SendMessageAsync(" + msg.MessageType + ")");

            var args = new SocketEventArgs
            {
                MessageToSend = msg,
                ClientUid = clientUid
            };

            // If we expect a response, push the event data on our stack and retrieve it with the response
            if (args.MessageToSend.ExpectedResponse != MessageType.None)
            {
                _messageStore.Add(args.MessageToSend, eventData, clientUid);
            }

            args.OnOperationComplete((eventArgs) =>
            {
                MessageType messageType = eventArgs.MessageToSend.MessageType;

                string exceptionText = eventArgs.SocketException == null
                    ? "Success."
                    : "Error: " + eventArgs.SocketException.ToString();
                Logger.LogMessage("Protocol", LogLevel.Verbose, "SendMessageAsync(" + messageType + ") completed callback. " + exceptionText);

                if (eventArgs.SocketException != null)
                {
                    // Clean up pending message queue
                    _messageStore.Remove(args.MessageToSend.ExpectedResponse, MqttMessageBase.GetMessageIdOrDefault(args.MessageToSend), clientUid);
                }

                OnSendMessageAsyncCompleted(clientUid, eventArgs.MessageToSend, eventArgs.SocketException);

                if (messageType == MessageType.Connect && eventArgs.SocketException != null)
                {
                    FireConnectComplete(new MqttNetEventArgs
                    {
                        Message = args.MessageToSend,
                        Exception = eventArgs.SocketException,
                        AdditionalErrorInfo = eventArgs.AdditionalErrorInfo,
                        EventData = eventData,
                        ClientUid = clientUid
                    });
                }
                else if (messageType == MessageType.Disconnect)
                {
                    CloseConnection(clientUid);
                    FireSendMessageComplete(new MqttNetEventArgs
                    {
                        Message = args.MessageToSend,
                        Exception = eventArgs.SocketException,
                        AdditionalErrorInfo = eventArgs.AdditionalErrorInfo,
                        EventData = eventData,
                        ClientUid = clientUid
                    });
                }
                else if (messageType == MessageType.Subscribe && eventArgs.SocketException != null)
                {
                    FireSubscribeMessageComplete(new MqttNetEventArgs
                    {
                        Message = args.MessageToSend,
                        Exception = eventArgs.SocketException,
                        AdditionalErrorInfo = eventArgs.AdditionalErrorInfo,
                        EventData = eventData,
                        ClientUid = clientUid
                    });
                }
                else if (args.MessageToSend.ExpectedResponse == MessageType.None || eventArgs.SocketException != null)
                {
                    FireSendMessageComplete(new MqttNetEventArgs
                    {
                        Message = args.MessageToSend,
                        Exception = eventArgs.SocketException,
                        AdditionalErrorInfo = eventArgs.AdditionalErrorInfo,
                        EventData = eventData,
                        ClientUid = clientUid
                    });
                }
            });

            Socket.WriteAsync(args);
        }

        /// <summary>
        /// Called when an send message has completed but before any expected responsed.
        /// </summary>
        /// <param name="clientUid"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected virtual void OnSendMessageAsyncCompleted(string clientUid, IMqttMessage message, Exception exception)
        {
        }

        /// <summary>
        /// Called when an MQTT message has completed processing.
        /// I.e. at the end of a publish qos=2 sequence, at the end of a connect/conack sequence, etc.
        /// </summary>
        /// <param name="clientUid"></param>
        /// <param name="message"></param>
        /// <param name="eventData"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected virtual void OnMqttMessageCompleted(string clientUid, IMqttMessage message, object eventData, Exception exception)
        {
        }

        protected virtual void OnPublishReceived(MqttNetEventArgs args)
        {
            FireMessageReceived(args);
        }

        protected void FireConnectComplete(MqttNetEventArgs args)
        {
            OnMqttMessageCompleted(args.ClientUid, args.Message, args.EventData, args.Exception);
            if (ConnectComplete != null)
            {
                ConnectComplete(this, args);
            }
        }

        protected void FireSubscribeMessageComplete(MqttNetEventArgs args)
        {
            OnMqttMessageCompleted(args.ClientUid, args.Message, args.EventData, args.Exception);
            if (SubscribeComplete != null)
            {
                SubscribeComplete(this, args);
            }
        }

        protected void FireSendMessageComplete(MqttNetEventArgs args)
        {
            //string dataStr = args.EventData == null ? "(null)" : args.EventData.GetType().ToString();
            //string exceptionStr = args.Exception == null ? "(null)" : args.Exception.ToString();
            //string messageType = args.Message == null ? "(null)" : args.Message.MessageType.ToString();
            //Logger.LogMessage("Protocol", LogLevel.Verbose, "Firing SendMessageCompete event. ClientID=" + args.ClientUid + " Message=" + messageType + " EventData=" + dataStr + ", Exception=" + exceptionStr);

            OnMqttMessageCompleted(args.ClientUid, args.Message, args.EventData, args.Exception);
            if (SendMessageComplete != null)
            {
                SendMessageComplete(this, args);
            }
        }

        protected void FireMessageReceived(MqttNetEventArgs args)
        {
            if (PublishReceived != null)
            {
                PublishReceived(this, args);
            }
        }

        protected void FireNetworkError(IMqttMessage message, Exception ex)
        {
            if (NetworkError != null)
            {
                NetworkError(this, new MqttMessageEventArgs
                {
                    Message = message,
                    Exception = ex
                });
            }
        }

        private void SocketMessageReceived(MqttNetEventArgs args)
        {
            if (args.Message != null)
            {
                Logger.LogMessage("Protocol", LogLevel.Verbose, "Protocol notified of message received " + args.Message.MessageType + " Client Identifier=" + args.ClientUid);
                OnMessageReceived(args);

                switch (args.Message.MessageType)
                {
                    case MessageType.ConnAck:
                        args.EventData = _messageStore.Remove(args.Message, args.ClientUid);
                        var conAck = args.Message as MqttConnectAckMessage;
                        if (conAck != null && conAck.ConnectReturnCode != ConnectReturnCode.Accepted)
                        {
                            Socket.Disconnect(args.ClientUid);
                        }
                        FireConnectComplete(args);
                        break;

                    case MessageType.SubAck:
                        args.EventData = _messageStore.Remove(args.Message, args.ClientUid);
                        FireSubscribeMessageComplete(args);
                        break;

                    case MessageType.PubAck:
                    case MessageType.PubComp:
                    case MessageType.UnsubAck:
                        args.EventData = _messageStore.Remove(args.Message, args.ClientUid);
                        FireSendMessageComplete(args);
                        break;

                    case MessageType.Publish:
                        DoClientPublishWorkflow(args);
                        break;

                    case MessageType.PubRec:
                        DoClientPublishRecWorkflow(args);
                        break;

                    case MessageType.PubRel:
                        DoClientPubRelWorkflow(args);
                        break;

                    case MessageType.PingResp:
                        args.EventData = _messageStore.Remove(args.Message, args.ClientUid);
                        FireSendMessageComplete(args);
                        break;
                }
            }
            else if (args.Exception != null)
            {
                Logger.LogException("Protocol", LogLevel.Verbose, "Exception occured while receiving message from server.", args.Exception);
            }
        }

        protected virtual void OnMessageReceived(MqttNetEventArgs args)
        {
            
        }

        private void DoClientPublishWorkflow(MqttNetEventArgs args)
        {
            var publishMsg = (MqttPublishMessage)args.Message;

            switch (publishMsg.QualityOfService)
            {
                // Just fire event and we're done
                case QualityOfService.AtMostOnce:
                    OnPublishReceived(args);
                    break;

                // Must send a PubAck message
                case QualityOfService.AtLeastOnce:
                    var pubAck = new MqttPublishAckMessageBuilder
                    {
                        MessageId = publishMsg.MessageId,
                    };
                    SendMessageAsync(pubAck, null, args.ClientUid);
                    OnPublishReceived(args);
                    break;

                // Must send a PubRec message
                case QualityOfService.ExactlyOnce:
                    var pubRec = new MqttPublishReceivedMessageBuilder
                    {
                        MessageId = publishMsg.MessageId,
                    };
                    SendMessageAsync(pubRec, publishMsg, args.ClientUid);
                    break;
            }
        }

        private void DoClientPublishRecWorkflow(MqttNetEventArgs args)
        {
            var publishMsg = (IMqttIdMessage)args.Message;

            object eventData = _messageStore.Remove(args.Message, args.ClientUid);
            var pubRel = new MqttPublishReleaseMessageBuilder
            {
                MessageId = publishMsg.MessageId,
            };
            SendMessageAsync(pubRel, eventData, args.ClientUid);
        }

        private void DoClientPubRelWorkflow(MqttNetEventArgs args)
        {
            var publishRelMsg = (IMqttIdMessage)args.Message;

            // Pop the original publish message to pass to the client
            object data = _messageStore.Remove(args.Message, args.ClientUid);
            args.Message = data as MqttPublishMessage;
            if (args.Message != null)
            {
                var pubComp = new MqttPublishCompleteMessageBuilder
                {
                    MessageId = publishRelMsg.MessageId,
                };
                SendMessageAsync(pubComp, null, args.ClientUid);
                OnPublishReceived(args);
            }
            else
            {
                Logger.LogMessage("Protocol", LogLevel.Warning, "Received PubRel message but no pending publishes were found. Client Identifier=" + args.ClientUid);
            }
        }

        private void MessageStoreOnMessageTimeout(object sender, MessageStoreEventArgs args)
        {
            var idMsg = args.FailedMessage as IMqttIdMessage;
            string id = idMsg == null ? "(no ID)" : idMsg.MessageId.ToString();

            Logger.LogMessage("Protocol", LogLevel.Verbose, "Message timeout: Type=" + args.FailedMessage.MessageType + ", QOS=" + args.FailedMessage.QualityOfService + ", ID=" + id);
            OnMessageTimeout(args);

            switch (args.FailedMessage.MessageType)
            {
                case MessageType.Connect:
                    CloseConnection(args.ClientUid);
                    FireConnectComplete(new MqttNetEventArgs
                    {
                        Exception = new NotRespondingException("The remote MQTT device did not respond to a Connect request. The connection has been closed."),
                        Message = args.FailedMessage,
                        EventData = args.EventData,
                        ClientUid = args.ClientUid
                    });
                    FireNetworkError(
                        args.FailedMessage,
                        new NotRespondingException("The remote MQTT device did not respond to a Connect request. The connection has been closed."));
                    break;

                case MessageType.PingReq:
                    CloseConnection(args.ClientUid);
                    FireNetworkError(
                        args.FailedMessage,
                        new NotRespondingException("The remote MQTT device did not respond to a Ping request. The connection has been closed."));
                    break;

                default:
                    FireNetworkError(
                        args.FailedMessage,
                        new NotRespondingException("The remote MQTT device did not respond to a " +
                            args.FailedMessage.MessageType + " message, ID=" + id + "."));
                    break;
            }
        }

        protected virtual void OnMessageTimeout(MessageStoreEventArgs args)
        {
            
        }

        public virtual void CloseConnection(string clientUid)
        {
            Socket.Disconnect(clientUid);
            Socket.JoinDisconnect(clientUid);
            _messageStore.Clear(clientUid);
        }

        public void Dispose()
        {
            _messageStore.Clear();
            Socket.Dispose();
        }

        protected string GenerateClientUid(MqttConnectMessage msg)
        {
            return GenerateClientUid(msg.ClientId, msg.UserName);
        }

        protected string GenerateClientUid(MqttConnectMessageBuilder msg)
        {
            return GenerateClientUid(msg.ClientId, msg.UserName);
        }

        /// <summary>
        /// Generate the unique key that will identify this a connected client. Note that some scenarios (Azure queues
        /// for example) require this name to be in all lower case.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        private string GenerateClientUid(string clientId, string username)
        {
            string uid;

            if (username != null && username.Length > 0)
            {
                uid = clientId + "-" + username;
            }
            else
            {
                uid = clientId;
            }

            return uid;
        }
    }
}
