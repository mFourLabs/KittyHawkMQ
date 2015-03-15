using System;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Collections
{
    [TestClass]
    public class QualityOfServiceCollection_Tests
    {
        [TestMethod]
        public void AddingItemsToCollectionGrowsArray()
        {
            var list = new QualityOfServiceCollection();
            Assert.AreEqual(0, list.Count);

            list.Add(QualityOfService.AtLeastOnce);
            Assert.AreEqual(1, list.Count);

            list.Add(QualityOfService.AtMostOnce);
            Assert.AreEqual(2, list.Count);

            list.Add(QualityOfService.ExactlyOnce);
            Assert.AreEqual(3, list.Count);

            list.Add(QualityOfService.Reserved3);
            Assert.AreEqual(4, list.Count);

            list.Add(QualityOfService.AtLeastOnce);
            Assert.AreEqual(5, list.Count);

            list.Add(QualityOfService.AtLeastOnce);
            Assert.AreEqual(6, list.Count);

            list.Add(QualityOfService.AtLeastOnce);
            Assert.AreEqual(7, list.Count);

            list.Add(QualityOfService.AtLeastOnce);
            Assert.AreEqual(8, list.Count);

            list.Add(QualityOfService.AtLeastOnce);
            Assert.AreEqual(9, list.Count);
        }

        [TestMethod]
        public void CanRetrieveIndexOfPreviouslyAddedItems()
        {
            var list = new QualityOfServiceCollection();
            list.Add(QualityOfService.ExactlyOnce);
            list.Add(QualityOfService.AtMostOnce);
            list.Add(QualityOfService.AtLeastOnce);
            list.Add(QualityOfService.ExactlyOnce);
            list.Add(QualityOfService.AtMostOnce);
            list.Add(QualityOfService.AtLeastOnce);
            list.Add(QualityOfService.ExactlyOnce);

            Assert.AreEqual(0, list.IndexOf(QualityOfService.ExactlyOnce));
            Assert.AreEqual(1, list.IndexOf(QualityOfService.AtMostOnce));
            Assert.AreEqual(2, list.IndexOf(QualityOfService.AtLeastOnce));
        }

        [TestMethod]
        public void CanClearList()
        {
            var list = new QualityOfServiceCollection();
            list.Add(QualityOfService.ExactlyOnce);
            list.Add(QualityOfService.AtMostOnce);
            list.Add(QualityOfService.AtLeastOnce);
            list.Add(QualityOfService.ExactlyOnce);
            list.Add(QualityOfService.AtMostOnce);
            list.Add(QualityOfService.AtLeastOnce);
            list.Add(QualityOfService.ExactlyOnce);

            list.Clear();

            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void CanRetreivePreviouslyAddedItems()
        {
            var list = new QualityOfServiceCollection();
            list.Add(QualityOfService.ExactlyOnce);
            list.Add(QualityOfService.AtMostOnce);
            list.Add(QualityOfService.AtLeastOnce);
            list.Add(QualityOfService.ExactlyOnce);
            list.Add(QualityOfService.AtMostOnce);
            list.Add(QualityOfService.AtLeastOnce);
            list.Add(QualityOfService.ExactlyOnce);

            var item = list.GetAt(0);
            Assert.AreEqual(QualityOfService.ExactlyOnce, item);
            item = list.GetAt(1);
            Assert.AreEqual(QualityOfService.AtMostOnce, item);
            item = list.GetAt(2);
            Assert.AreEqual(QualityOfService.AtLeastOnce, item);
            item = list.GetAt(3);
            Assert.AreEqual(QualityOfService.ExactlyOnce, item);
            item = list.GetAt(4);
            Assert.AreEqual(QualityOfService.AtMostOnce, item);
            item = list.GetAt(5);
            Assert.AreEqual(QualityOfService.AtLeastOnce, item);
            item = list.GetAt(6);
            Assert.AreEqual(QualityOfService.ExactlyOnce, item);
        }
    }
}
