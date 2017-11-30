//**********************************************************************************************
// File:   FeedbackWords.cs
// Author: Andrés del Campo Novales
//
// This class handles mostly learning and storing the words for negative feedback.
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

using System;
using System.Linq;

namespace CSLearnerLib
{
    [Serializable]
    public class FeedbackWords
    {
        private readonly CircList<string> lastFeedbacks = new CircList<string>(2);

        // Words or sentences included in the feedback but are redundant / bogus and merely matching the reward.
        public string WrongFeedbackWords { get; set; } = string.Empty;
        
        public void Add(string feedback)
        {
            lastFeedbacks.Add(feedback);
        }

        public string ParseFeedbackForRewards(string fullFeedback)
        {
            lastFeedbacks.Add(fullFeedback);

            string reward = "-";

            string wrongFeedbackWords = LearnWrongFeedbackWords(set: false);
            if (wrongFeedbackWords != string.Empty)
                WrongFeedbackWords = wrongFeedbackWords;

            // It's right when it's not wrong... I hope!
            if (WrongFeedbackWords != string.Empty &&
                !fullFeedback.Contains(WrongFeedbackWords))
                reward = "+";

            return reward;
        }

        public string LearnWrongFeedbackWords(bool set = true)
        {
            string lastFeedback1 = lastFeedbacks[0];
            string lastFeedback2 = lastFeedbacks[1];

            string wrongFeedbackWords = LearnWrongFeedbackWords(lastFeedback1, lastFeedback2);

            if (set)
                  WrongFeedbackWords = wrongFeedbackWords;

            return wrongFeedbackWords;
        }

        private string LearnWrongFeedbackWords(string lastFeedback1, string lastFeedback2)
        {
            if (string.IsNullOrEmpty(lastFeedback1) || string.IsNullOrEmpty(lastFeedback2))
                return string.Empty;

            if (lastFeedback1.Length < 3 || lastFeedback2.Length < 3)
                return string.Empty;

            if (lastFeedback1 == lastFeedback2)
                return string.Empty;

            if (!lastFeedback1.Contains(' ') || !lastFeedback2.Contains(' '))
                return string.Empty;

            string[] lastFeedbackWords1 = Syntax.GetWords(lastFeedback1);
            string[] lastFeedbackWords2 = Syntax.GetWords(lastFeedback2);

            // There has to be room for some actual feedback...
            if (lastFeedbackWords1.Length < 2 || lastFeedbackWords2.Length < 2)
                return string.Empty;

            // Try to find equal words from the left
            string wrongFeedbackWords = string.Empty;
            for (int i = 0; i < lastFeedbackWords1.Length && i < lastFeedbackWords2.Length; i++)
            {
                if (lastFeedbackWords1[i] != lastFeedbackWords2[i])
                    break;
                wrongFeedbackWords += lastFeedbackWords1[i] + " ";
            }

            // If nothing found, try to find equal words from the right
            if (wrongFeedbackWords == string.Empty)
            {
                for (int i = Math.Min(lastFeedbackWords1.Length, lastFeedbackWords2.Length) - 1; i >= 0; i--)
                {
                    if (lastFeedbackWords1[i] != lastFeedbackWords2[i])
                        break;
                    wrongFeedbackWords = " " + lastFeedbackWords1[i] + wrongFeedbackWords;
                }
            }

            return wrongFeedbackWords;
        }

        public void LearnAndSetWrongFeedbackWordsIfEmpty()
        {
            if (WrongFeedbackWords == string.Empty)
                LearnWrongFeedbackWords();
        }
    }
}
