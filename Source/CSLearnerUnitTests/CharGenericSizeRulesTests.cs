//**********************************************************************************************
// File:   CharGenericSizeRulesTests.cs
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
    public class CharGenericSizeRulesTests
    {
        [TestMethod]
        public void TestAbstractGenericRule1To1()
        {
            var rules = new CharGenericSizeRules();
            bool newRule = rules.AbstractGenericRule1To1("CONSTANT Ð001ÐÐ002Ð +", "Ð002Ð+Ð001Ð", "CONSTANT Ð001ÐÐ002ÐÐ003Ð +", "Ð003Ð+Ð002Ð+Ð001Ð", string.Empty);
            Assert.IsTrue(newRule);
            string output = rules.ApplyMatchingRule("CONSTANT abcde +");
            Assert.AreEqual("e+d+c+b+a", output);
        }

        [TestMethod]
        public void TestAbstractGenericRule1ToN()
        {
            var rules = new CharGenericSizeRules();
            bool newRule = rules.AbstractGenericRule1ToN("CONSTANT Ð001ÐÐ002Ð", "Ð002Ð Ð001Ð", "CONSTANT Ð001ÐÐ002ÐÐ003Ð", "Ð003Ð Ð002Ð Ð001Ð", string.Empty);
            Assert.IsTrue(newRule);
            string output = rules.ApplyMatchingRule("CONSTANT abcde");
            Assert.AreEqual("e d c b a", output);
        }
    }
}
