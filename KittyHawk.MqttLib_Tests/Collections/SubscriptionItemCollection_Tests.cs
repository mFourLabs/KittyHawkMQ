using System;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class SubscriptionItemCollection_Tests
    {
        [TestMethod]
        public void AddingItemsToCollectionGrowsArray()
        {
            var list = new SubscriptionItemCollection();
            Assert.AreEqual(0, list.Count);

            list.Add(new SubscriptionItem() {QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b0"});
            Assert.AreEqual(1, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b1" });
            Assert.AreEqual(2, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b2" });
            Assert.AreEqual(3, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b3" });
            Assert.AreEqual(4, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b4" });
            Assert.AreEqual(5, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b5" });
            Assert.AreEqual(6, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b6" });
            Assert.AreEqual(7, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b7" });
            Assert.AreEqual(8, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b8" });
            Assert.AreEqual(9, list.Count);
        }

        [TestMethod]
        public void AddingRemovingItemsAdjustsCount()
        {
            var list = new SubscriptionItemCollection();
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b0" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b1" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b2" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b3" });
            Assert.AreEqual(4, list.Count);

            list.Remove("a/b0");
            Assert.AreEqual(3, list.Count);

            list.Remove("a/b2");
            Assert.AreEqual(2, list.Count);

            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b4" });
            Assert.AreEqual(3, list.Count);

            list.Remove("a/b4");
            Assert.AreEqual(2, list.Count);
        }

        [TestMethod]
        public void CanFindPreviouslyAddedItems()
        {
            var list = new SubscriptionItemCollection();
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b0" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b1" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b2" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b3" });

            Assert.IsTrue(list.Contains("a/b0"));
            Assert.IsTrue(list.Contains("a/b1"));
            Assert.IsTrue(list.Contains("a/b2"));
            Assert.IsTrue(list.Contains("a/b3"));
            Assert.IsFalse(list.Contains("a/c0"));
        }

        [TestMethod]
        public void CanRetrieveIndexOfPreviouslyAddedItems()
        {
            var list = new SubscriptionItemCollection();
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b0" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b1" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b2" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b3" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b4" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b5" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b6" });

            Assert.AreEqual(0, list.IndexOf("a/b0"));
            Assert.AreEqual(1, list.IndexOf("a/b1"));
            Assert.AreEqual(2, list.IndexOf("a/b2"));
            Assert.AreEqual(3, list.IndexOf("a/b3"));
            Assert.AreEqual(4, list.IndexOf("a/b4"));
            Assert.AreEqual(5, list.IndexOf("a/b5"));
            Assert.AreEqual(6, list.IndexOf("a/b6"));
        }

        [TestMethod]
        public void CanRetrieveIndexOfPreviouslyAddedItemsAfterRemovalOfItems()
        {
            var list = new SubscriptionItemCollection();
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b0" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b1" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b2" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b3" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b4" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b5" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b6" });

            list.Remove("a/b0");
            list.Remove("a/b2");
            list.Remove("a/b4");
            list.Remove("a/b6");

            Assert.AreEqual(0, list.IndexOf("a/b1"));
            Assert.AreEqual(1, list.IndexOf("a/b3"));
            Assert.AreEqual(2, list.IndexOf("a/b5"));
        }

        [TestMethod]
        public void AddingSameItemMoreThanOnceFails()
        {
            var list = new SubscriptionItemCollection();
            list.Add(new SubscriptionItem()
            {
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = "a/b0"
            });

            try
            {
                list.Add(new SubscriptionItem()
                {
                    QualityOfService = QualityOfService.AtLeastOnce,
                    TopicName = "a/b0"
                });
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (Exception)
            {
                Assert.Fail("Wrong exception type thrown.");
            }

            Assert.Fail("No exception thrown for invalid argument.");
        }

        [TestMethod]
        public void CanClearList()
        {
            var list = new SubscriptionItemCollection();
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b0" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b1" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b2" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b3" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b4" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b5" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b6" });

            list.Clear();

            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void CanRemoveLastItemInList()
        {
            var list = new SubscriptionItemCollection();
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b0" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b1" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b2" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b3" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b4" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b5" });

            list.Remove("a/b0");
            list.Remove("a/b1");
            list.Remove("a/b2");
            list.Remove("a/b3");
            list.Remove("a/b4");

            Assert.AreEqual(1, list.Count);

            list.Remove("a/b5");

            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void CanRetreivePreviouslyAddedItems()
        {
            var list = new SubscriptionItemCollection();
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b0" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b1" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b2" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b3" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b4" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b5" });
            list.Add(new SubscriptionItem() { QualityOfService = QualityOfService.AtLeastOnce, TopicName = "a/b6" });

            var item = list.GetAt(0);
            Assert.AreEqual("a/b0", item.TopicName);
            item = list.GetAt(1);
            Assert.AreEqual("a/b1", item.TopicName);
            item = list.GetAt(2);
            Assert.AreEqual("a/b2", item.TopicName);
            item = list.GetAt(3);
            Assert.AreEqual("a/b3", item.TopicName);
            item = list.GetAt(4);
            Assert.AreEqual("a/b4", item.TopicName);
            item = list.GetAt(5);
            Assert.AreEqual("a/b5", item.TopicName);
            item = list.GetAt(6);
            Assert.AreEqual("a/b6", item.TopicName);
        }
    }
}
