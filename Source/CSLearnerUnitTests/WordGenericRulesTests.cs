//**********************************************************************************************
// File:   WordGenericRulesTests.cs
// Author: Andrés del Campo Novales
//**********************************************************************************************
//**********************************************************************************************
// Copyright 2017 Andrés del Campo Novales
//
// This file is part of CSLearner.

// CSLearner is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// CSLearner is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with CSLearner.  If not, see<http://www.gnu.org/licenses/>.
//**********************************************************************************************

using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSLearnerLib;

namespace CSLearnerUnitTests
{
    [TestClass]
    public class WordGenericRulesTests
    {
        [TestMethod]
        public void TestAbstractAndApplyRule1()
        {
            var rules = new WordGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT xyz", "xyz", "CONSTANT ab", "ab", string.Empty);
            Assert.IsTrue(newRule);
            string output = rules.ApplyMatchingRule("CONSTANT bkjf");
            Assert.AreEqual("bkjf", output);
        }

        [TestMethod]
        public void TestAbstractAndApplyRuleCompound()
        {
            var rules = new WordGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT ab cde", "cdeab", "CONSTANT xyz ab", "abxyz", string.Empty);
            Assert.IsTrue(newRule);
            string output = rules.ApplyMatchingRule("CONSTANT 1234 5");
            Assert.AreEqual("51234", output);
        }

        [TestMethod]
        public void TestAbstractNotPossible1()
        {
            var rules = new WordGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT xyz", "yz", "CONSTANT abc", "abc", string.Empty);
            Assert.IsFalse(newRule);
        }

        [TestMethod]
        public void TestAbstractNotPossible2()
        {
            var rules = new WordGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT1 xyz", "xyz", "CONSTANT2 ab", "ab", string.Empty);
            Assert.IsFalse(newRule);
        }

        [TestMethod]
        public void TestAbstractNotPossible3()
        {
            var rules = new WordGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT1 xyz", "xyz", "CONSTANT2 abc", "abc", string.Empty);
            Assert.IsFalse(newRule);
        }
    }
}
