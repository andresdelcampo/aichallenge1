//**********************************************************************************************
// File:   Syntax.cs
// Author: Andrés del Campo Novales
//
// This class handles anything related to the syntax of the input. It can parse it to analyze
// whether there are delimiting characters, where the input and feedback separate, etc.
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
using System.Diagnostics;
using System.Linq;

namespace CSLearnerLib
{
    [Serializable]
    public class Syntax
    {
        public readonly FeedbackWords FeedbackWords = new FeedbackWords();

        // Optional characters/strings to indicate feedback on input or separation of requests
        public string AnswerNowChar { get; private set; } = string.Empty;
        public string NextRequestChar { get; private set; }  = string.Empty;

        // Words or sentences included in the feedback but are redundant / bogus and merely matching the reward.
        public string WrongFeedbackWords { get => FeedbackWords.WrongFeedbackWords; set => FeedbackWords.WrongFeedbackWords = value; }


        // Last attempted or known input and feedback lengths -on absence of separators
        public int InputLength = 1;
        public int FeedbackLength;

        // Sometimes feedback can be provided with bogus numbers/chars in front, not detectable as wrong feedback
        // words, but just as a number of chars.
        public int FeedbackRealChars = 0;

        public Syntax()
        {
        }

        public Syntax(string answerNowChar, string nextRequestChar)
        {
            AnswerNowChar = answerNowChar;
            NextRequestChar = nextRequestChar;
        }

        public bool IsSingleCharMode()
        {
            return InputLength == 1
                && FeedbackLength == 0;
        }

        public void AnalyzeSeparators(string inputs, string rewards)
        {
            Debug.Assert(inputs.Length == rewards.Length || inputs.Length-1 == rewards.Length);
            Debug.Assert(inputs.Length >= 4);

            // Single char mode
            if (inputs.Length == 4)
                return;

            if (inputs.Length == rewards.Length + 1)
                inputs = inputs.Substring(0, rewards.Length);

            RemoveExtraSpaces(ref inputs, ref rewards);

            // We should always have 4 rewards in the string or someone messed up badly and we deserve the crash
            int[] rewardIndexes = RewardIndexes(rewards);

            bool hasAnswerNow = FindAnswerNowChar(inputs, rewardIndexes);

            FindNextRequestCharFromLeft(inputs, rewardIndexes);
            FindNextRequestChar(inputs, rewardIndexes, hasAnswerNow);

            Debug.Assert(FeedbackLength >= 0);
        }

        #region Find methods
        private void FindNextRequestChar(string inputs, int[] rewardIndexes, bool hasAnswerNow)
        {
            if (NextRequestChar == string.Empty)
            {
                char symbol1 = FindSymbolToLeft(inputs, rewardIndexes[2] - (hasAnswerNow ? 1 : 0), out int distance1);
                char symbol2 = FindSymbolToLeft(inputs, rewardIndexes[3] - (hasAnswerNow ? 1 : 0), out int distance2);

                if (symbol1 == symbol2 &&
                    symbol1 != ' ' &&
                    distance1 <= rewardIndexes[2] - rewardIndexes[1] && 
                    distance2 <= rewardIndexes[3] - rewardIndexes[2])
                {
                    NextRequestChar = symbol1.ToString();
                    InputLength = Math.Max(distance1, distance2) - (hasAnswerNow ? 1 : 0);

                    // Make room in input length for the output
                    int outputLength = FeedbackLength - 1;
                    InputLength -= outputLength;
                    if (InputLength < 1)
                        InputLength = 1;
                }
            }
        }

        private void FindNextRequestCharFromLeft(string inputs, int[] rewardIndexes)
        {
            if (NextRequestChar == string.Empty)
            {
                string feedback2 = inputs.Substring(rewardIndexes[1] + 1, rewardIndexes[2] - rewardIndexes[1]);
                string feedback3 = inputs.Substring(rewardIndexes[2] + 1, inputs.Length - rewardIndexes[2] - 1);

                if (feedback2[0] != feedback3[0])
                    return;

                if (feedback2 == feedback3 || feedback2.Contains(feedback3) || feedback3.Contains(feedback2))
                    return;

                int i = 0;
                while (feedback2[i] == feedback3[i]) { i++; }

                char symbol2 = FindSymbolToRight(feedback2, i, out int distance2);
                char symbol3 = FindSymbolToRight(feedback3, i, out int distance3);

                // The symbol might just be further to the right? Think of scenarios like "Answer.; Newinput"
                // We don't want the "." but the ";" there.
                if (symbol2 == symbol3 && symbol2.ToString() == AnswerNowChar)
                {
                    char symbol2B = FindSymbolToRight(feedback2, i + distance2, out int distance2B);
                    char symbol3B = FindSymbolToRight(feedback3, i + distance3, out int distance3B);
                    if (symbol2B == symbol3B && symbol2B != ' ')
                    {
                        symbol2 = symbol2B;
                        symbol3 = symbol3B;
                        distance2 = distance2B;
                        distance3 = distance3B;
                    }
                }

                if (symbol2 == symbol3 && symbol2 != ' ')
                {
                    NextRequestChar = symbol2.ToString();
                    WrongFeedbackWords = feedback2.Substring(0, i);
                    InputLength = Math.Max(feedback2.Length - i - distance2,
                                           feedback3.Length - i - distance3);
                }
            }
        }

        private bool FindAnswerNowChar(string inputs, int[] rewardIndexes)
        {
            bool hasAnswerNow = false;

            // Assuming this for now as we are no longer in single char mode
            FeedbackLength = 1;

            if (AnswerNowChar != string.Empty)
            {
                hasAnswerNow = true;
            }
            else
            {
                char answerNowCandidate = inputs[rewardIndexes[1]];

                if (!Char.IsLetterOrDigit(answerNowCandidate) &&
                     inputs[rewardIndexes[1]] == inputs[rewardIndexes[2]] &&
                     inputs[rewardIndexes[2]] == inputs[rewardIndexes[3]])
                {
                    if (answerNowCandidate == ' ')
                    {
                        char answerNowChar1 = FindNonSpaceToLeft(inputs, rewardIndexes[1], out int distance1);
                        char answerNowChar2 = FindNonSpaceToLeft(inputs, rewardIndexes[2], out int distance2);
                        char answerNowChar3 = FindNonSpaceToLeft(inputs, rewardIndexes[3], out int distance3);

                        if (!Char.IsLetterOrDigit(answerNowChar1) &&
                            answerNowChar1 == answerNowChar2 && 
                            answerNowChar2 == answerNowChar3 &&
                            answerNowChar1 != ' ' &&
                            distance1 < rewardIndexes[1] &&
                            distance2 < rewardIndexes[2] - rewardIndexes[1] &&
                            distance3 < rewardIndexes[3] - rewardIndexes[2])
                        {
                            AnswerNowChar = answerNowChar1.ToString();
                            hasAnswerNow = true;
                        }

                        FeedbackLength = Math.Max(distance1, Math.Max(distance2, distance3)) + 1;
                        Debug.Assert(FeedbackLength > 1);
                    }
                    else
                    {
                        AnswerNowChar = answerNowCandidate.ToString();
                        hasAnswerNow = true;
                    }
                }
            }

            return hasAnswerNow;
        }

        private static char FindNonSpaceToLeft(string inputs, int startIndex, out int distance)
        {
            char nonSpace = ' ';
            distance = 1;

            for (int i=startIndex-1; i>=0; i--, distance++)
            {
                if (inputs[i] != ' ')
                {
                    nonSpace = inputs[i];
                    break;
                }
            }

            return nonSpace;
        }

        private static char FindSymbolToLeft(string inputs, int startIndex, out int distance)
        {
            char symbol = ' ';
            distance = 1;

            for (int i = startIndex - 1; i >= 0; i--, distance++)
            {
                if (!Char.IsLetterOrDigit(inputs[i]) && inputs[i] != ' ')
                {
                    symbol = inputs[i];
                    break;
                }
            }

            return symbol;
        }

        private static char FindSymbolToRight(string inputs, int startIndex, out int distance)
        {
            char symbol = ' ';
            distance = 1;

            for (int i = startIndex + 1; i < inputs.Length; i++, distance++)
            {
                if (!Char.IsLetterOrDigit(inputs[i]) && inputs[i] != ' ')
                {
                    symbol = inputs[i];
                    break;
                }
            }

            return symbol;
        }
        #endregion

        #region Sentence Manipulation
        public static void RemoveExtraSpaces(ref string output)
        {
            for (int i = 0; i < output.Length - 1; i++)
            {
                while (output[i] == ' ' && output[i + 1] == ' ')
                {
                    output = output.Remove(i, 1);

                    if (i == output.Length - 1)
                        return;
                }
            }
        }

        private static void RemoveExtraSpaces(ref string inputs, ref string rewards)
        {
            for (int i = 0; i < inputs.Length - 1; i++)
            {
                while (inputs[i] == ' ' && inputs[i + 1] == ' ')
                {
                    Debug.Assert(rewards[i] == ' ');

                    inputs = inputs.Remove(i, 1);
                    rewards = rewards.Remove(i, 1);

                    if (i == inputs.Length - 1)
                        return;
                }
            }
        }

        private static int[] RewardIndexes(string rewards)
        {
            int[] rewardIndexes = new int[4];
            int index = 0;
            for (int i = 0; i < 4; i++)
            {
                while (rewards[index] == ' ')
                    index++;
                rewardIndexes[i] = index;
                index++;
            }

            return rewardIndexes;
        }

        public static string[] GetWords(string inputText)
        {
            char[] charSeparators = { ' ' };
            return inputText.Trim().Split(charSeparators);
        }
        
        public static string[] GetWordsAndOperands(string inputText)
        {
            // Separate into words including separating on symbols, with the exception of 
            // negative numbers! We intend to separate things like 5asf3+8234 into 5asf3 + 8234.
            string operandSeparatedInput = inputText;
            for (int i = inputText.Length - 2; i >= 0; i--)
            {
                if (Char.IsLetterOrDigit(inputText[i]) && 
                    !Char.IsLetterOrDigit(inputText[i+1]) && 
                    inputText[i + 1] != ' ')
                    operandSeparatedInput = operandSeparatedInput.Insert(i+1, " ");
                else if (!Char.IsLetterOrDigit(inputText[i]) && 
                         Char.IsLetterOrDigit(inputText[i + 1]) && 
                         inputText[i] != ' ' &&
                         (inputText[i] != '-' || (i>0 && Char.IsLetterOrDigit(inputText[i - 1]))))  
                    operandSeparatedInput = operandSeparatedInput.Insert(i + 1, " ");
            }

            return GetWords(operandSeparatedInput);
        }

        public static void SplitSentence(string[] inputWords, int wordsInFirstSentence, out string inputSubsetForRule, out string restOfInput)
        {
            inputSubsetForRule = string.Empty;
            restOfInput = string.Empty;

            for (int i = 0; i < inputWords.Length; i++)
            {
                if (i < wordsInFirstSentence)
                    inputSubsetForRule += inputWords[i] + " ";
                else
                    restOfInput += inputWords[i] + " ";
            }

            inputSubsetForRule = inputSubsetForRule.TrimEnd();
            restOfInput = restOfInput.TrimEnd();
        }

        public static bool RemoveEndCharIfPresent(ref string input, string end)
        {
            bool inputHasEndChar = (end != string.Empty && input.EndsWith(end));
            if (inputHasEndChar)
            {
                input = input.Substring(0, input.Length - end.Length);
            }

            return inputHasEndChar;
        }

        public static bool RemoveEndCharIfPresent(ref string input1, ref string input2, string end)
        {
            bool inputHasEndChar = (end != string.Empty && input1.EndsWith(end) && input2.EndsWith(end));
            if (inputHasEndChar)
            {
                input1 = input1.Substring(0, input1.Length - end.Length);
                input2 = input2.Substring(0, input2.Length - end.Length);
            }

            return inputHasEndChar;
        }

        public static bool RemoveEqualEndOfSentenceCharsIfPresent(ref string[] inputWords, ref string[] patternWords)
        {
            string lastInputWord = inputWords[inputWords.Length - 1];
            string lastPatternWord = patternWords[patternWords.Length - 1];
            bool patternWordRemoved = false;

            // Remove also a second ending character if present too.
            if (lastInputWord.Length - 2 >= 0 && lastPatternWord.Length - 2 >= 0 &&
                lastInputWord[lastInputWord.Length - 2] == lastPatternWord[lastPatternWord.Length - 2] &&
                lastInputWord[lastInputWord.Length - 1] == lastPatternWord[lastPatternWord.Length - 1])
            {
                inputWords[inputWords.Length - 1] = lastInputWord.Substring(0, lastInputWord.Length - 2);
                patternWords[patternWords.Length - 1] = lastPatternWord.Substring(0, lastPatternWord.Length - 2);
            }
            else if (lastInputWord.Length - 1 >= 0 && lastPatternWord.Length - 1 >= 0 &&
                     lastInputWord[lastInputWord.Length - 1] == lastPatternWord[lastPatternWord.Length - 1])
            {
                inputWords[inputWords.Length - 1] = lastInputWord.Substring(0, lastInputWord.Length - 1);
                patternWords[patternWords.Length - 1] = lastPatternWord.Substring(0, lastPatternWord.Length - 1);
            }

            if (patternWords[patternWords.Length - 1] == string.Empty)
            {
                patternWords = patternWords.Take(patternWords.Count() - 1).ToArray();
                patternWordRemoved = true;
            }
            if (inputWords[inputWords.Length - 1] == string.Empty)
                inputWords = inputWords.Take(inputWords.Count() - 1).ToArray();

            return patternWordRemoved;
        }

        public string RemoveFeedbackWords(string output)
        {
            if (output == string.Empty || WrongFeedbackWords == string.Empty)
                return output;

            if (output.StartsWith(WrongFeedbackWords))
            {
                return output.Remove(0, WrongFeedbackWords.Length);
            }

            if (output.EndsWith(WrongFeedbackWords))
            {
                return output.Remove(output.Length - WrongFeedbackWords.Length, WrongFeedbackWords.Length);
            }

            return output;
        }
        #endregion
    }
}
