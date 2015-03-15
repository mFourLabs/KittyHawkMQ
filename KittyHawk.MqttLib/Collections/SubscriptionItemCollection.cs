
using System;
#if WIN_PCL
using Windows.Foundation.Metadata;
#endif
using KittyHawk.MqttLib.Messages;

namespace KittyHawk.MqttLib.Collections
{
    public sealed class SubscriptionItem
    {
        public QualityOfService QualityOfService { get; set; }
        public string TopicName { get; set; }
        public override bool Equals(object obj)
        {
            var compareTo = obj as SubscriptionItem;
            if (compareTo == null)
            {
                return false;
            }
            return TopicName.Equals(compareTo.TopicName);
        }

        public override int GetHashCode()
        {
            return TopicName.GetHashCode();
        }
    }

    public sealed class SubscriptionItemCollection
    {
        private readonly AutoExpandingArray _list = new AutoExpandingArray();

        internal SubscriptionItemCollection()
        {
            Clear();
        }

        public int Add(SubscriptionItem item)
        {
            if (Contains(item))
            {
                throw new ArgumentException("An item with this topic name already exists.");
            }

            return _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

#if WIN_PCL
        [DefaultOverload]
#endif
        public bool Contains(string topic)
        {
            return _list.Contains(new SubscriptionItem
            {
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = topic
            });
        }

        public bool Contains(SubscriptionItem item)
        {
            return _list.Contains(item);
        }

#if WIN_PCL
        [DefaultOverload]
#endif
        public int IndexOf(string topic)
        {
            return _list.IndexOf(new SubscriptionItem
            {
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = topic
            });
        }

        public int IndexOf(SubscriptionItem item)
        {
            return _list.IndexOf(item);
        }

        public SubscriptionItem GetAt(int index)
        {
            return (SubscriptionItem)_list.GetAt(index);
        }

#if WIN_PCL
        [DefaultOverload]
#endif
        public void Remove(string topic)
        {
            _list.Remove(new SubscriptionItem
            {
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = topic
            });
        }

        public void Remove(SubscriptionItem item)
        {
            _list.Remove(item);
        }

        public int Count
        {
            get { return _list.Count; }
        }
    }
}
