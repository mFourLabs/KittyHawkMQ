
using System;
using KittyHawk.MqttLib.Collections;

namespace KittyHawk.MqttLib.Client
{
    /// <summary>
    /// Tracks all active clients that have been created. Must be thread safe.
    /// </summary>
    internal class ActiveClientCollection
    {
        // The absolute maximum number of client instance that can be handed out
        private const int MaxNumberOfClients = 1;

        private static readonly ActiveClientCollection _instance = new ActiveClientCollection();
        public static ActiveClientCollection Instance
        {
            get { return _instance; }
        }

        private readonly AutoExpandingArray _clientsArray = new AutoExpandingArray();
        private readonly object _syncLock = new object();

        private ActiveClientCollection()
        {
            
        }

        public int Count
        {
            get
            {
                lock (_syncLock)
                {
                    return _clientsArray.Count;
                }
            }
        }

        public void AddClient(MqttClient client)
        {
            lock (_syncLock)
            {
                DoAddValidation(client);
                _clientsArray.Add(client);
            }
        }

        public void RemoveClient(MqttClient client)
        {
            lock (_syncLock)
            {
                _clientsArray.Remove(client);
            }
        }

        public void TestCanAddNewClient()
        {
            if (Count >= MaxNumberOfClients)
            {
                throw new InvalidOperationException("No more MqttClient instances can be created. Dispose an existing MqttClient before creating another one.");
            }
        }

        /// <summary>
        /// Perform any validation required before adding a client to the collection.
        /// Throw exceptions to indicate errors.
        /// </summary>
        /// <param name="client"></param>
        private void DoAddValidation(MqttClient client)
        {
            TestCanAddNewClient();
        }
    }
}
