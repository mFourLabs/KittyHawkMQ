
using KittyHawk.MqttLib.Messages;

namespace KittyHawk.MqttLib.Collections
{
    public sealed class QualityOfServiceCollection
    {
        private readonly AutoExpandingArray _list = new AutoExpandingArray();

        internal QualityOfServiceCollection()
        {
            Clear();
        }

        public int Add(QualityOfService qos)
        {
            return _list.Add(qos);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(QualityOfService qos)
        {
            return _list.Contains(qos);
        }

        public int IndexOf(QualityOfService qos)
        {
            return _list.IndexOf(qos);
        }

        public QualityOfService GetAt(int index)
        {
            return (QualityOfService)_list.GetAt(index);
        }

        public int Count
        {
            get { return _list.Count; }
        }
    }
}
