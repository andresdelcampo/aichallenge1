//**********************************************************************************************
// File:   CharGenericSizeRules.cs
// Author: Andrés del Campo Novales
//
// This class handles rules created from repeated patterns in which the element are characters, 
// and with the ability to abstract the size of one of its words following a growth pattern.
// A basic example of this is:
//      From:
//          Reverse XY -> YX
//          Reverse XYZ -> ZYX
//      extracts a GenericRule that can extend this pattern to any size:
//          Reverse #001#... -> ...#001#
// It can also detect whether a specific input matches a rule, and apply it to obtain an output.
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
using System.Collections.Generic;
using System.Diagnostics;

namespace CSLearnerLib
{
    [Serializable]
    public class CharGenericSizeRules
    {
        private readonly List<GenericRule> rules = new List<GenericRule>();

        //**********************************************************************************************
        // FailedRule
        //**********************************************************************************************
        public void FailedRule(string input)
        {
            bool ruleRemoved = false;

            foreach (GenericRule rule in rules)
            {
                if (SentenceMatchesPattern(rule, input, true, out string _))
                {
                    rules.Remove(rule);
                    ruleRemoved = true;
                    break;
                }
            }

            Debug.Assert(ruleRemoved);
        }

        //*********************************************************************************************
        // GetRule
        //*********************************************************************************************
        private GenericRule GetRule(string patternInput)
        {
            GenericRule ruleFound = null;

            // Find a matching rule in the list of rules
            foreach (GenericRule rule in rules)
            {
                if (patternInput == rule.Input)
                {
                    ruleFound = rule;
                    break;
                }
            }
            
            return ruleFound;
        }

        //**********************************************************************************************
        // ApplyMatchingRule
        //
        // Find and apply the rule that matches the rule input to provide the output
        //**********************************************************************************************
        public string ApplyMatchingRule(string input)
        {
            foreach (GenericRule rule in rules)
            {
                if (SentenceMatchesPattern(rule, input, true, out string varWord))
                {
                    BuildSpecificRule(rule, varWord, out string inputPattern, out string outputPattern);

                    return CharGenericRules.ApplyRule(inputPattern, outputPattern, input);
                }
            }

            return string.Empty;
        }

        //**********************************************************************************************
        // AbstractGenericRuleFromLastAdded
        //**********************************************************************************************
        public void AbstractGenericRuleFromLastAdded(CharGenericRules charRules, string end)
        {
            string input = charRules[charRules.Count - 1].Input;
            string output = charRules[charRules.Count - 1].Output;

            for (int i = charRules.Count - 2; i >= 0; i--)
            {
                AbstractGenericRule1To1(input, output, charRules[i].Input, charRules[i].Output, end);
                AbstractGenericRule1ToN(input, output, charRules[i].Input, charRules[i].Output, end);
            }
        }

        //**********************************************************************************************
        // AbstractGenericRule1To1
        //
        // Verify if the inputs and outputs suggest a generic size rule (because most words match and 
        // otherwise words of one are included as subwords in the other, following a growth pattern. 
        // If so, extract the rule. This variant recognizes only one growing word in input, matching only
        // one word in the output.
        // Example:
        //    Interleave #001##002# with #003# -> #001##003##002#
        //    Interleave #001##002##003# with #004# -> #001##004##002##004##003#
        // Learns the pattern and how to grow it further
        //**********************************************************************************************
        public bool AbstractGenericRule1To1(string input1, string output1, string input2, string output2, string end)
        {
            if (input1.Length == input2.Length || output1.Length == output2.Length)
                return false;

            // Swap the rules if the bigger is 1.
            if (input1.Length > input2.Length)
            {
                string temp = input1;
                input1 = input2;
                input2 = temp;
                temp = output1;
                output1 = output2;
                output2 = temp;
            }

            // The smaller input rule has the longest output? Not for generalization
            if (output1.Length > output2.Length)
                return false;

            // If it has end of sentence character, take it out -we don't want it tied to the last word.
            Syntax.RemoveEndCharIfPresent(ref input1, ref input2, end);
            Syntax.RemoveEndCharIfPresent(ref input1, ref input2, " ");
            bool outputHasEndChar = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, end);
            bool outputHasEndSpace = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, " ");

            // Check single word on input has constants -and which one.
            string[] inputWords1 = Syntax.GetWords(input1);
            string[] inputWords2 = Syntax.GetWords(input2);
            string[] outputWords1 = Syntax.GetWords(output1);
            string[] outputWords2 = Syntax.GetWords(output2);

            // Start simple: Same amount of words
            if (inputWords1.Length != inputWords2.Length || outputWords1.Length != outputWords2.Length)
                return false;

            // Words are either equal or included on each other and only one word variable
            // Also the var word has to be all ids, and the second rule only one more ID than the first
            int inputVarWordIndex = -1;
            for (int i=0; i<inputWords1.Length; i++)
            {
                if (inputWords1[i] == inputWords2[i])
                    continue;

                if (inputVarWordIndex != -1)
                    return false;

                if (NumIdsInIdOnlyWord(inputWords1[i]) + 1 != NumIdsInIdOnlyWord(inputWords2[i]))
                    return false;

                if (!inputWords2[i].Contains(inputWords1[i]))
                    return false;

                inputVarWordIndex = i;
            }

            if (inputVarWordIndex == -1)
                return false;

            // Same for output except that the word does not need to be all identifiers
            int outputVarWordIndex = -1;
            for (int i = 0; i < outputWords1.Length; i++)
            {
                if (outputWords1[i] == outputWords2[i])
                    continue;

                if (outputVarWordIndex != -1)
                    return false;

                if (!outputWords2[i].Contains(outputWords1[i]))
                    return false;

                outputVarWordIndex = i;
            }

            if (outputVarWordIndex == -1)
                return false;

            string inputVarWord1 = inputWords1[inputVarWordIndex];
            string inputVarWord2 = inputWords2[inputVarWordIndex];
            string outputVarWord1 = outputWords1[outputVarWordIndex];
            string outputVarWord2 = outputWords2[outputVarWordIndex];

            // Last check, all IDs from the input should appear in the output
            int ruleSize2 = NumIdsInIdOnlyWord(inputVarWord2);
            for (int i=0; i<ruleSize2; i++)
            {
                string id = CharGenericRules.ExtractIdOrChar(inputVarWord2, i * CharGenericRules.IdLength);
                Debug.Assert(id != string.Empty);

                if (!outputWords2[outputVarWordIndex].Contains(id))
                    return false;
            }

            // So far we have ONE variable word in input all IDs and ONE in output with anything, and they are contained in the second rule as well
            // Let's find out what differs.
            string id2 = inputVarWord2.Replace(inputVarWord1, string.Empty);
            if (id2.Length != CharGenericRules.IdLength)
                return false;

            // Determine how the output of N+1 grows by removing the output of N and keeping what is to the left and right
            string[] aroundId = outputVarWord2.Split(new[] { outputVarWord1 }, StringSplitOptions.None);
            if (aroundId.Length != 2)
                return false;
            string growToLeft = aroundId[0];
            string growToRight = aroundId[1];

            // The only id that changed in the input when growing the rule did not appear in the output... 
            if (!growToLeft.Contains(id2) && !growToRight.Contains(id2))
                return false;

            // Convert the rule into its basic size
            string id1 = inputWords1[inputVarWordIndex].Substring(0, CharGenericRules.IdLength);
            inputWords1[inputVarWordIndex] = id1;
            string input = string.Join(" ", inputWords1);

            outputWords1[outputVarWordIndex] = id1;
            string output = string.Join(" ", outputWords1);

            string endingSpace = outputHasEndSpace ? " " : string.Empty;
            string endingChar = outputHasEndChar ? end : string.Empty;
            string ending = endingSpace + endingChar;

            if (GetRule(input) == null)
                rules.Add(new GenericRule(input, output, id1, id2, growToLeft, growToRight, ending));

            return true;
        }

        //**********************************************************************************************
        // AbstractGenericRule1ToN
        //
        // Verify if the inputs and outputs suggest a generic size rule (because most words match and 
        // otherwise words of one are included as subwords in the other, following a growth pattern. 
        // If so, extract the rule. This variant recognizes only one growing word in input, matching 
        // many words in the output.
        // Example:
        //    Spell #001##002# -> #001# #002#
        //    Spell #001##002##003# -> #001# #002# #003#
        // Learns the pattern and how to grow it further
        //**********************************************************************************************
        public bool AbstractGenericRule1ToN(string input1, string output1, string input2, string output2, string end)
        {
            if (input1.Length == input2.Length)     // ## Differs from 1:1 in not checking output
                return false;

            // Swap the rules if the bigger is 1.
            if (input1.Length > input2.Length)
            {
                string temp = input1;
                input1 = input2;
                input2 = temp;
                temp = output1;
                output1 = output2;
                output2 = temp;
            }

            // The smaller input rule has the longest output? Not for generalization
            if (output1.Length > output2.Length)
                return false;

            // If it has end of sentence character, take it out -we don't want it tied to the last word.
            Syntax.RemoveEndCharIfPresent(ref input1, ref input2, end);
            Syntax.RemoveEndCharIfPresent(ref input1, ref input2, " ");
            bool outputHasEndChar = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, end);
            bool outputHasEndSpace = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, " ");

            // Check single word on input has constants -and which one.
            string[] inputWords1 = Syntax.GetWords(input1);
            string[] inputWords2 = Syntax.GetWords(input2);
            string[] outputWords1 = Syntax.GetWords(output1);
            string[] outputWords2 = Syntax.GetWords(output2);

            // Start simple: Same amount of words   
            // ## Differs from 1:1 as output length in bigger rule is higher
            if (inputWords1.Length != inputWords2.Length || outputWords1.Length + 1 != outputWords2.Length)
                return false;

            // Words are either equal or included on each other and only one word variable
            // Also the var word has to be all ids, and the second rule only one more ID than the first
            int inputVarWordIndex = -1;
            for (int i = 0; i < inputWords1.Length; i++)
            {
                if (inputWords1[i] == inputWords2[i])
                    continue;

                if (inputVarWordIndex != -1)
                    return false;

                if (NumIdsInIdOnlyWord(inputWords1[i]) + 1 != NumIdsInIdOnlyWord(inputWords2[i]))
                    return false;

                if (!inputWords2[i].Contains(inputWords1[i]))
                    return false;

                inputVarWordIndex = i;
            }

            if (inputVarWordIndex == -1)
                return false;

            // Same for output except that the word does not need to be all identifiers
            // ## Differs significantly from 1:1 starting here as there is no single output word, 
            // and each word is an identifier. Comparisons can be done with the whole string
            if (!output2.Contains(output1))
                return false;

            string inputVarWord1 = inputWords1[inputVarWordIndex];
            string inputVarWord2 = inputWords2[inputVarWordIndex];

            // Last check, all IDs from the input should appear in the output
            int ruleSize2 = NumIdsInIdOnlyWord(inputVarWord2);
            for (int i = 0; i < ruleSize2; i++)
            {
                string id = CharGenericRules.ExtractIdOrChar(inputVarWord2, i * CharGenericRules.IdLength);
                Debug.Assert(id != string.Empty);

                // ## Differs from 1:1 as output is now many words
                if (!output2.Contains(id))
                    return false;
            }

            // So far we have ONE variable word in input all IDs and MANY in output with anything, 
            // and they are contained in the second rule as well. Let's find out what differs.
            string id2 = inputVarWord2.Replace(inputVarWord1, string.Empty);
            if (id2.Length != CharGenericRules.IdLength)
                return false;

            // Determine how the output of N+1 grows by removing the output of N and keeping what is to the left and right
            // ## Differs from 1:1 as output is now many words
            string[] aroundId = output2.Split(new[] { output1 }, StringSplitOptions.None);
            if (aroundId.Length != 2)
                return false;
            string growToLeft = aroundId[0];
            string growToRight = aroundId[1];

            // The only id that changed in the input when growing the rule did not appear in the output... 
            if (!growToLeft.Contains(id2) && !growToRight.Contains(id2))
                return false;

            // Convert the rule into its basic size
            string id1 = inputWords1[inputVarWordIndex].Substring(0, CharGenericRules.IdLength);
            inputWords1[inputVarWordIndex] = id1;
            string input = string.Join(" ", inputWords1);

            // ## Differs from 1:1 as output is now many words
            string output = id1;

            string endingSpace = outputHasEndSpace ? " " : string.Empty;
            string endingChar = outputHasEndChar ? end : string.Empty;
            string ending = endingSpace + endingChar;

            if (GetRule(input) == null)
                rules.Add(new GenericRule(input, output, id1, id2, growToLeft, growToRight, ending));

            return true;
        }

        //**********************************************************************************************
        // NumIdsInIdOnlyWord
        //
        // Returns how many IDs are in the word or -1 if something is not an ID within the word.
        //**********************************************************************************************
        private static int NumIdsInIdOnlyWord(string word)
        {
            int numIds = 0;

            for (int i = 0; i < word.Length; i += CharGenericRules.IdLength)
            {
                string id = CharGenericRules.ExtractIdOrChar(word, i);

                if (id.Length > 1)
                    numIds++;
                else
                    return -1;  // All must be IDs!
            }

            return numIds;
        }

        //**********************************************************************************************
        // SentenceMatchesPattern
        //
        // Verify whether a string matches the rule input string with factIDs inside. 
        // With the twist of allowing rules of multiple word sizes.
        //**********************************************************************************************
        private static bool SentenceMatchesPattern(GenericRule rule, string inputText, bool useAllInput, out string varWord)
        {
            varWord = string.Empty;

            // If it has end of sentence character, take it out -we don't want it tied to the last word.
            string empty = rule.Ending;
            Syntax.RemoveEndCharIfPresent(ref inputText, ref empty, rule.Ending.Trim());

            Dictionary<string, char> seenIDs = new Dictionary<string, char>();

            // Separate all the strings into words
            string[] inputWords = Syntax.GetWords(inputText);
            string[] patternWords = Syntax.GetWords(rule.Input);

            // Check the input sentences against the pattern
            // Not the same number of words in the line, not match -if we are supposed to use all input
            if (useAllInput && inputWords.Length != patternWords.Length)
                return false;

            // Either way, the input has to be at least the size of the pattern
            if (inputWords.Length < patternWords.Length)
                return false;

            bool match = true;
            for (int word = 0; word < patternWords.Length && match; word++)
            {
                // It is not a variable word, and it is not the same, not match
                if (!CharGenericRules.IsIdWord(patternWords[word]))
                {
                    if (inputWords[word] != patternWords[word])
                        match = false;
                }
                else
                {
                    // #####
                    // Is it a VARIABLE SIZE variable word?
                    if (patternWords[word] == rule.Id1)
                    {
                        varWord = inputWords[word];
                    }
                    else
                    {
                        // #####
                        // It is a variable word, check each of its characters
                        int p = 0;
                        for (int c = 0; c < inputWords[word].Length && match; c++)
                        {
                            string id = CharGenericRules.ExtractIdOrChar(patternWords[word], p);

                            if (id.Length == 0)
                            {
                                // We ran out of pattern!!
                                match = false;
                            }
                            else if (id.Length == 1)
                            {
                                // Not really an ID! Must match the input
                                if (inputWords[word][c] != id[0])
                                    match = false;
                            }
                            else
                            {
                                // It IS an ID - Check if the char has been seen before or is new
                                if (seenIDs.ContainsKey(id))
                                {
                                    if (inputWords[word][c] != seenIDs[id])
                                        match = false;
                                }
                                else
                                {
                                    seenIDs.Add(id, inputWords[word][c]);
                                }
                            }

                            p += id.Length;
                        }

                        // Input is shorter than pattern!
                        if (p != patternWords[word].Length)
                            match = false;
                    }
                }
            }

            return match;
        }

        //**********************************************************************************************
        // BuildSpecificRule
        //
        // Starting with a generic size char rule, and a word with identifiers (which also determines 
        // the rule size to build), build the specific rule. 
        // Example:
        // Rule:                Hello #001# -> #001#   , growleft empty, growright #002#
        // Varword:                   #004##005##006# 
        // Output patterns:     Hello #004##005##006# -> #004##005##006# 
        //**********************************************************************************************
        private static void BuildSpecificRule(GenericRule rule, string varWord, out string inputPattern, out string outputPattern)
        {
            int ruleSize = varWord.Length;

            int nextId = 900;
            string id = CharGenericRules.GenerateId(nextId++);
            string inputIdWord = id;

            outputPattern = rule.Output.Replace(rule.Id1, id);

            // Grow the rule as needed
            for (int i = 2; i <= ruleSize; i++)
            {
                id = CharGenericRules.GenerateId(nextId++);
                inputIdWord = inputIdWord + id;

                string growLeft = rule.GrowToLeft.Replace(rule.Id2, id);
                string growRight = rule.GrowToRight.Replace(rule.Id2, id);

                outputPattern = growLeft + outputPattern + growRight;
            }

            outputPattern += rule.Ending;
            inputPattern = rule.Input.Replace(rule.Id1, inputIdWord);
        }
    }
}
