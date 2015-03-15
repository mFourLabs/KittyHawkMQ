using System;
using KittyHawk.MqttLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Utilities
{
    [TestClass]
    public class Topic_Tests
    {
        [TestMethod]
        public void ExactTopicNamesMatch()
        {
            const string subscription = "Kitty/Temperature/0";
            const string incommingTopic = "Kitty/Temperature/0";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic));
        }

        [TestMethod]
        public void CaseDifferenceInTopicNamesDoNotMatch()
        {
            const string subscription = "kitty/temperature/0";
            const string incommingTopic = "Kitty/Temperature/0";

            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic));
        }

        [TestMethod]
        public void MultiLevelWildcardMatchesAllTopics()
        {
            const string subscription = "Kitty/Temperature/#";
            const string incommingTopic1 = "Kitty/Temperature";
            const string incommingTopic2 = "Kitty/Temperature/";
            const string incommingTopic3 = "Kitty/Temperature/0";
            const string incommingTopic4 = "Kitty/Temperature/0/1/2";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic2));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic3));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic4));
        }

        [TestMethod]
        public void MultiLevelWildcardDoesNotMatchesAllTopics()
        {
            const string subscription = "Kitty/Temperature/#";
            const string incommingTopic1 = "Kitty/temperature";
            const string incommingTopic2 = "Kitty/Hawk/";
            const string incommingTopic3 = "Witty/Temperature/0";
            const string incommingTopic4 = "Kitty/Temperaturo";

            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic2));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic3));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic4));
        }

        [TestMethod]
        public void SingleLevelWildcardMatchesLastLevel()
        {
            const string subscription = "Kitty/Temperature/+";
            const string incommingTopic1 = "Kitty/Temperature/0";
            const string incommingTopic2 = "Kitty/Temperature/1";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic2));
        }

        [TestMethod]
        public void SingleLevelWildcardDoesNotMatchNoLevel()
        {
            const string subscription = "Kitty/Temperature/+";
            const string incommingTopic1 = "Kitty/Temperature";
            const string incommingTopic2 = "Kitty/Temperature/0/1";
            const string incommingTopic3 = "Kitty/Temperature/0/1/2";

            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic2));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic3));
        }

        [TestMethod]
        public void SingleLevelWildcardMatchesMiddleLevel()
        {
            const string subscription = "Kitty/Temperature/+/Boiler";
            const string incommingTopic1 = "Kitty/Temperature/0/Boiler";
            const string incommingTopic2 = "Kitty/Temperature/1/Boiler";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic2));
        }

        [TestMethod]
        public void SingleLevelWildcardDoesNotMatchesMiddleLevel()
        {
            const string subscription = "Kitty/Temperature/+/Boiler";
            const string incommingTopic1 = "Kitty/Temperature/0/Furnace";
            const string incommingTopic2 = "Kitty/Temperature/1/Boiler/2";
            const string incommingTopic3 = "Kitty/Temperature/1/Boiler/Furnace";

            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic2));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic3));
        }

        [TestMethod]
        public void MixAndMatchWildcardTestShouldMatch1()
        {
            const string subscription = "Kitty/Temperature/+/Boiler/#";
            const string incommingTopic1 = "Kitty/Temperature/0/Boiler";
            const string incommingTopic2 = "Kitty/Temperature/1/Boiler/2";
            const string incommingTopic3 = "Kitty/Temperature/2/Boiler/Furnace/4";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic2));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic3));
        }

        [TestMethod]
        public void MixAndMatchWildcardTestShouldNotMatch1()
        {
            const string subscription = "Kitty/Temperature/+/Boiler/#";
            const string incommingTopic1 = "Kitty/Temperature/0/Furnace";
            const string incommingTopic2 = "Kitty/Temperature";
            const string incommingTopic3 = "Kitty/Temperature/123/Furnace/Boiler/4";

            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic2));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic3));
        }

        [TestMethod]
        public void MixAndMatchWildcardTestShouldMatch2()
        {
            const string subscription = "Kitty/Temperature/+/Boiler/+/Furnace";
            const string incommingTopic1 = "Kitty/Temperature/0/Boiler/12/Furnace";
            const string incommingTopic2 = "Kitty/Temperature/1/Boiler/Blah/Furnace";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic2));
        }

        [TestMethod]
        public void MixAndMatchWildcardTestShouldNotMatch2()
        {
            const string subscription = "Kitty/Temperature/+/Boiler/+/Furnace";
            const string incommingTopic1 = "Kitty/Temperature/0/Boiler";
            const string incommingTopic2 = "Kitty/Temperature/1/Blah/Boiler/Furnace";

            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic2));
        }

        [TestMethod]
        public void MixAndMatchWildcardTestShouldMatch3()
        {
            const string subscription = "Kitty/Temperature/+/Boiler/+/Furnace/#";
            const string incommingTopic1 = "Kitty/Temperature/0/Boiler/12/Furnace";
            const string incommingTopic2 = "Kitty/Temperature/1/Boiler/Blah/Furnace/a/b/c";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic2));
        }

        [TestMethod]
        public void MixAndMatchWildcardTestShouldNotMatch3()
        {
            const string subscription = "Kitty/Temperature/+/Boiler/+/Furnace/#";
            const string incommingTopic1 = "Kitty/Temperature/0/Boiler/12/Fireplace";
            const string incommingTopic2 = "Kitty/Temperature/1/Boiler/Blah/Furnacing";

            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic2));
        }

        [TestMethod]
        public void ShouldMatchEverything()
        {
            const string subscription = "#";
            const string incommingTopic1 = "Kitty/temperature";
            const string incommingTopic2 = "Kitty/Temperature/1/Boiler/Blah/Furnacing";
            const string incommingTopic3 = "Kitty/Temperature/1/Boiler/Blah/Furnace/a/b/c";
            const string incommingTopic4 = "Kitty";
            const string incommingTopic5 = "";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic2));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic3));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic4));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic5));
        }

        [TestMethod]
        public void ShouldMatchEverythingThatIsAtLeastOneLevel()
        {
            const string subscription = "+/#";
            const string incommingTopic1 = "Kitty/temperature";
            const string incommingTopic2 = "Kitty/Temperature/1/Boiler/Blah/Furnacing";
            const string incommingTopic3 = "Kitty/Temperature/1/Boiler/Blah/Furnace/a/b/c";
            const string incommingTopic4 = "Kitty";

            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic1));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic2));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic3));
            Assert.IsTrue(Topic.IsTopicMatch(subscription, incommingTopic4));
        }

        [TestMethod]
        public void ShouldNotMatchThis()
        {
            const string subscription = "+/#";
            const string incommingTopic1 = "";

            Assert.IsFalse(Topic.IsTopicMatch(subscription, incommingTopic1));
        }
    }
}
