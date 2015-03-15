using System;

namespace KittyHawk.MqttLib.Utilities
{
    internal static class Topic
    {
        /// <summary>
        /// Returns true if the specific subscription is a match for the topic name.
        /// For example:
        ///     subscription="a/b/+"    topic="a/b/c"   match=true
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="topicName"></param>
        /// <returns></returns>
        public static bool IsTopicMatch(string subscription, string topicName)
        {
            // NOTE: Topic names ARE case sensitive

            // Case 1: Direct mach
            if (String.Equals(subscription, topicName))
            {
                return true;
            }

            string[] subTopicsTest = subscription.Split(new[] {'/'});
            string[] subTopicsIn = topicName.Split(new[] { '/' });

            // In general is test length is greater than incoming topic length, its not a match. Except for
            // Test = "a/b/+/#" Incoming = "a/b/c"
            if ((subTopicsTest.Length - 1) > subTopicsIn.Length)
            {
                return false;
            }

            for (int i = 0; i < subTopicsTest.Length; i++)
            {
                if (subTopicsTest[i] == "#")        // Multi-level wildcard, we're done
                {
                    return true;
                }
                if (subTopicsIn.Length <= i)   // No more levels to compare to, failed test
                {
                    return false;
                }
                if (subTopicsTest[i] == "+")   // Matches anything on this level, keep going
                {
                    if (subTopicsIn[i].Length == 0) // OK, almost anything. Let's not match an empty string
                    {
                        return false;
                    }
                    continue;
                }
                if (subTopicsTest[i] != subTopicsIn[i])    // Levels do not match, failed test
                {
                    return false;
                }
            }

            if (subTopicsIn.Length > subTopicsTest.Length)  // Incomming topic too specific with no matching wildcards
            {
                return false;
            }

            return true;
        }
    }
}
