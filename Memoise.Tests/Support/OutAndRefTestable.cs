using System;
using System.Collections.Generic;

namespace Memoise.Tests.Support
{
    public class OutAndRefTestable : IOutAndRefTestable
    {
        public Dictionary<string, int> TrySomethingCalls = new Dictionary<string, int>();
        public bool TrySomething(string s, out int a)
        {
            IncrementDictValue(TrySomethingCalls, s);

            a = s.GetHashCode();
            return true;
        }

        public Dictionary<string, int> TrySomethingElseCalls = new Dictionary<string, int>();
        public bool TrySomethingElse(string s, ref int a)
        {
            IncrementDictValue(TrySomethingElseCalls, s);

            a = s.GetHashCode();
            return true;
        }

        private void IncrementDictValue(Dictionary<string, int> dict, string s)
        {
            if (!dict.ContainsKey(s))
            {
                dict[s] = 0;
            }
            dict[s] += 1;
        }
    }
}