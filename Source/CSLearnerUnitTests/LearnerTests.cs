//**********************************************************************************************
// File:   LearnerTests.cs
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
    public class LearnerTests
    {
        [TestMethod]
        public void TestIdentity()
        {
            string input___ = "abcdefgabc";
            string output__ = "  ccefgabc";
            string rewards = " --+-++++++";

            LearnerTester(input___, output__, rewards);
        }

        [TestMethod]
        public void TestUniform()
        {
            string input___ = "abcdabcd";
            string output__ = "  cdaaaa";
            string rewards = " ----++++";

            LearnerTester(input___, output__, rewards);
        }

        [TestMethod]
        public void TestInverted()
        {
            string input___ = "ababbbab";
            string output__ = "  abaaba";
            string rewards = " ----++++";

            LearnerTester(input___, output__, rewards);
        }

        [TestMethod]
        public void TestMoved()
        {
            string input___ = "abcabcabcabc";
            string output__ = "  cababaabca";
            string rewards = " -----++-++++";

            LearnerTester(input___, output__, rewards);
        }

        [TestMethod]
        public void TestSwitch()
        {
            string input___ = "abcabcabcabcaaaaaaaababbbab";
            string output__ = "  cababaabcabbbbbbb  aabbab";
            string rewards = " -----++-++++++++++---+-++++";

            LearnerTester(input___, output__, rewards);
        }

        [TestMethod]
        public void TestMultiChar()
        {
            string input___ = "64081922579070707367914402514255379366836928114363614362";
            string output__ = "         7 9 7 7 7 9 6 4 0 4 5 5 9 6 3 6 8 2 4 6 6 4 6 5";
            string rewards = "  - - - - - - + + - + - - - + + - + + - + - - + + + + + +";

            LearnerTester(input___, output__, rewards);
        }


        private void LearnerTester(string input, string output, string rewards)
        {
            Learner learner = new Learner();
            bool firstRun = true;

            // talk
            for (int i = 0; i < input.Length; i++)
            {
                if (firstRun)
                    firstRun = false;
                else
                    learner.RegisterReward(rewards.Substring(i, 1));

                // reply
                string reply = learner.Answer(input.Substring(i, 1));

                Assert.AreEqual(output.Substring(i, 1), reply, "Unexpected answer in step "+(i+1));
            }
        }
    }
}
