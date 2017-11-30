//**********************************************************************************************
// File:   CharGenericRulesTests.cs
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
    public class CharGenericRulesTests
    {
        [TestMethod]
        public void TestAbstractAndApplyRule1()
        {
            var rules = new CharGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT xyz", "zyx", "CONSTANT abc", "cba", string.Empty);
            Assert.IsTrue(newRule);
            string output = rules.ApplyMatchingRule("CONSTANT bkj");
            Assert.AreEqual("jkb", output);
        }

        [TestMethod]
        public void TestAbstractAndApplyRuleWithRepeatedChars()
        {
            var rules = new CharGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT abcd", "abcd", "CONSTANT abba", "abba", string.Empty);
            Assert.IsTrue(newRule);
            string output = rules.ApplyMatchingRule("CONSTANT bkjt");
            Assert.AreEqual("bkjt", output);
        }
        
        [TestMethod]
        public void TestAbstractAndApplyRuleWithCommonWordOnOutput()
        {
            var rules = new CharGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT master", "m a s t e r", "CONSTANT insect", "i n s e c t", string.Empty);
            Assert.IsTrue(newRule);
            string output = rules.ApplyMatchingRule("CONSTANT rabbit");
            Assert.AreEqual("r a b b i t", output);
        }

        [TestMethod]
        public void TestAbstractAndApplyRuleCompound()
        {
            var rules = new CharGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT ab cde", "edcba", "CONSTANT xy zab", "bazyx", string.Empty);
            Assert.IsTrue(newRule);
            string output = rules.ApplyMatchingRule("CONSTANT xa xyb");
            Assert.AreEqual("byxax", output);
        }

        [TestMethod]
        public void TestAbstractNotPossible1()
        {
            var rules = new CharGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT xyz", "zyx", "CONSTANT abc", "cab", string.Empty);
            Assert.IsFalse(newRule);
        }

        [TestMethod]
        public void TestAbstractNotPossible2()
        {
            var rules = new CharGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT1 xyz", "zyx", "CONSTANT2 abc", "cba", string.Empty);
            Assert.IsFalse(newRule);
        }

        [TestMethod]
        public void TestAbstractNotPossible3()
        {
            var rules = new CharGenericRules();
            bool newRule = rules.AbstractGenericRule("CONSTANT1 xyz", "zyx", "CONSTANT2 abc", "ca", string.Empty);
            Assert.IsFalse(newRule);
        }
    }
}
