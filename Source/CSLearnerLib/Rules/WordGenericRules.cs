//**********************************************************************************************
// File:   WordGenericRules.cs
// Author: Andrés del Campo Novales
//
// This class handles rules created from repeated patterns in which the element is words.
// It can abstract rules similar to this, based on examples:
//      Reverse Word1 Word2 -> Word2 Word1
//      Reverse #001# #002# -> #002# #001#
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
using System.Linq;

namespace CSLearnerLib
{
    [Serializable]
    public class WordGenericRules
    {
        private readonly List<Rule> rules = new List<Rule>();

        // Keep aligned with CharGenericRules constants as long as some ID methods are reused...
        private const int MaxIdNumLength = 3;
        private const int IdLength = MaxIdNumLength + 2;
        

        //**********************************************************************************************
        // ApplyCompoundMatchingRule
        //
        // Try to match the input to several rules, each of a subset of the input from left to right.
        // This implementation directly takes the output and concatenates with no second level rule applied.
        //**********************************************************************************************
        public string ApplyCompoundMatchingRule(string input)
        {
            string[] inputWords = Syntax.GetWords(input);

            // Cycle through rules looking to match a subset of the input
            foreach (Rule rule in rules)
            {
                // When found, the output of the rule is accumulated to the final output
                // and the process repeats for the rest of the string.
                if (SentenceMatchesPattern(rule.Input, input, false))
                {
                    Syntax.SplitSentence(inputWords,
                                             Syntax.GetWords(rule.Input).Length,
                                             out string inputSubsetForRule,
                                             out string restOfInput);

                    string output;
                    if (restOfInput == string.Empty)
                    {
                        // End of the chain
                        output = ApplyRule(rule, inputSubsetForRule);
                        return output;
                    }

                    string innerOutput = ApplyCompoundMatchingRule(restOfInput);

                    // Have we found a valid set of rules to apply to the rest of the input fragments? 
                    // Exit if yes, or continue searching if no.
                    if (innerOutput != string.Empty)
                    {
                        output = ApplyRule(rule, inputSubsetForRule);
                        return output + " " + innerOutput;
                    }
                }
            }

            return string.Empty;
        }

        //**********************************************************************************************
        // FailedRule
        //
        // Adds the failed input to the rule to skip it as exception. After a number of failed attempts,
        // it deletes the rule.
        //**********************************************************************************************
        public void FailedRule(string input)
        {
            bool ruleRemoved = false;

            foreach (Rule rule in rules)
            {
                if (SentenceMatchesPattern(rule.Input, input))
                {
                    rules.Remove(rule);
                    ruleRemoved = true;
                    break;
                }
            }

            Debug.Assert(ruleRemoved);
        }

        //**********************************************************************************************
        // ApplyMatchingRule
        //
        // Find and apply the rule that matches the rule input to provide the output
        //**********************************************************************************************
        public string ApplyMatchingRule(string input)
        {
            foreach (Rule rule in rules)
            {
                if (SentenceMatchesPattern(rule.Input, input))
                {
                    return ApplyRule(rule, input); 
                }
            }

            return string.Empty;
        }

        //**********************************************************************************************
        // ApplyRule
        //
        // Extract the different facts from the input and provides the output of the rule
        //**********************************************************************************************
        private string ApplyRule(Rule rule, string input)
        {
            Dictionary<string, string> curFacts = new Dictionary<string, string>();
            ExtractFactsFromStr(rule.Input, input, ref curFacts);
            return ApplyFactsToStr(rule.Output, curFacts);
        }

        //**********************************************************************************************
        // SentenceMatchesPattern
        //
        // Verify whether a string matches the rule input string with factIDs inside. 
        //
        // Example that will match: 
        // pattern: "Hello #0001#"     inputText: "Hello Peter"
        //**********************************************************************************************
        private static bool SentenceMatchesPattern(string pattern, string inputText, bool useAllInput = true)
        {
            Dictionary<string, string> seenIDs = new Dictionary<string, string>();

            // Separate all the strings into words
            string[] inputWords = Syntax.GetWords(inputText);
            string[] patternWords = Syntax.GetWords(pattern);

            // Check the input sentences against the pattern
            // Not the same number of words in the line, not match
            if (useAllInput && inputWords.Length != patternWords.Length)
                return false;

            Syntax.RemoveEqualEndOfSentenceCharsIfPresent(ref inputWords, ref patternWords);

            // Either way, the input has to be at least the size of the pattern
            if (inputWords.Length < patternWords.Length)
                return false;

            bool match = true;
            for (int word = 0; word < patternWords.Length && match; word++)
            {
                // It is not a variable, and it is not the same, not match
                if (!IsIdWord(patternWords[word]))
                {
                    if (inputWords[word] != patternWords[word])
                        match = false;
                }
                else
                {
                    // It is a variable, check if it has been seen before in previous sentences or in this one
                    if (seenIDs.ContainsKey(patternWords[word]))
                    {
                        if (inputWords[word] != seenIDs[patternWords[word]])
                            match = false;
                    }
                    else
                    {
                        seenIDs.Add(patternWords[word], inputWords[word]);
                    }
                }
            }

            return match;
        }

        //**********************************************************************************************
        // ExtractFactsFromStr
        //
        // Extract values from a inputText string where they match the preconditions string with factIDs 
        // inside
        //
        // Example: 
        // preconditions: "Hello #0001#"
        // inputText: "Hello Peter"
        // will add to our facts database: #0001# = Peter
        //**********************************************************************************************
        private void ExtractFactsFromStr(string pattern, string inputText, ref Dictionary<string, string> curFacts)
        {
            string[] inputWords = Syntax.GetWords(inputText);
            string[] patternWords = Syntax.GetWords(pattern);

            Debug.Assert(inputWords.Length == patternWords.Length);

            Syntax.RemoveEqualEndOfSentenceCharsIfPresent(ref inputWords, ref patternWords);

            for (int i = 0; i < patternWords.Length; i++)
            {
                if (IsIdWord(patternWords[i]))
                {
                    // Substring in case that the input did not match the pattern and the pattern actually had
                    // end of sentence on it...
                    string id = patternWords[i].Substring(0, IdLength);
                    curFacts[id] = inputWords[i];
                }
            }
        }

        //**********************************************************************************************
        // ApplyFactsToStr
        //
        // Resolve facts IDs (#0001#) in a string to their current fact value stored in this class.
        // This is the opposite of ExtractFactsFromStr
        //**********************************************************************************************
        private string ApplyFactsToStr(string outputText, Dictionary<string, string> curFacts)
        {
            string[] outputWords = Syntax.GetWords(outputText);

            foreach (string word in outputWords)
            {
                if (IsIdWord(word))
                {
                    string id;
                    for (int j = 0; j < word.Length; j += id.Length)
                    {
                        id = CharGenericRules.ExtractIdOrChar(word, j);

                        if (id.Length > 1)
                        {
                            outputText = outputText.Replace(id, curFacts[id]);
                        }
                    }
                }
            }

            Debug.Assert(outputText.Contains('Ð') == false);

            return outputText;
        }

        //**********************************************************************************************
        // AbstractRepeatedElements
        //
        // From input and output strings presplit in word lists, abstract the common words.
        //
        // Example:     
        //      input: "Reverse: Car Bike"
        //      output: "Bike Car"
        //      inputVariability: "CVV"
        // will provide the output: 
        //      input: "Reverse: #01# #02#"
        //      output: "#02# #01#"
        //**********************************************************************************************
        private static bool AbstractRepeatedElements(string[] inputWords, string[] outputWords,
                                                     string inputVariability, 
                                                     out string inputPattern, out string outputPattern)
        {
            int nextId = 1;
            bool foundWordFromInputInOutput = false;

            for (int i = 0; i < outputWords.Length; i++)
            {
                string currentWord = outputWords[i];

                // We already replaced it earlier
                if (IsIdWord(currentWord))
                    continue;

                // Replace the word with ID in input and rest of output because it is variable
                string id = string.Empty;
                for (int w = 0; w < inputWords.Length; w++)
                {
                    if (inputWords[w] == currentWord)
                    {
                        if (id == string.Empty)
                            id = GenerateId(nextId++);
                        inputWords[w] = id;
                        foundWordFromInputInOutput = true;
                    }
                }

                // Variable word not found on input, try compound?
                if (id == string.Empty)  
                {
                    // Check if the output word is compound of input words
                    List<int> componentIndexes = new List<int>();
                    if (FindSubWords(currentWord, inputWords, ref componentIndexes))
                    {
                        // Found word combination, reconstruct the word made of indexes given the list
                        string outputWord = string.Empty;
                        foreach (int index in componentIndexes)
                        {
                            // Repeated reference to input word in the list? 
                            id = IsIdWord(inputWords[index]) ? inputWords[index] : GenerateId(nextId++);
                            inputWords[index] = id;
                            outputWord += id;
                        }

                        id = outputWord;
                        foundWordFromInputInOutput = true;
                    }
                }

                // If found on input (have an ID), replace the output and rest of the output with the new ID
                if (id != string.Empty)
                {
                    for (int w = i; w < outputWords.Length; w++)
                        if (outputWords[w] == currentWord)
                            outputWords[w] = id;
                }
            }

            for (int i = 0; i < inputWords.Length; i++)
            {
                // Skip constant words
                if (inputVariability[i] == 'C')
                    continue;

                string currentWord = inputWords[i];

                // We already replaced it earlier
                if (IsIdWord(currentWord))
                    continue;

                // Replace the word with ID in input because it is variable
                string id = GenerateId(nextId++);
                for (int w = i; w < inputWords.Length; w++)
                    if (inputWords[w] == currentWord)
                        inputWords[w] = id;
            }

            inputPattern = string.Join(" ", inputWords);
            outputPattern = string.Join(" ", outputWords);

            return foundWordFromInputInOutput;
        }

        //*********************************************************************************************
        // FindCommonWords
        //
        // From input strings presplit in word lists, find the common words and return a 
        // pattern summarizing it, in which C=Constant (word equal in both), V=Variable (word different).
        //
        // Example:     
        //      input1: "Reverse: Biker"
        //      input2: "Reverse: Haste"
        // will provide the output: 
        //      inputVariability: "CV"
        //*********************************************************************************************
        public static bool FindCommonWords(string[] inputWords1, string[] inputWords2, 
                                           out string inputVariability)
        {
            inputVariability = string.Empty;

            for (int i = 0; i < inputWords1.Length; i++)
            {
                inputVariability += (inputWords1[i] == inputWords2[i]) ? "C" : "V";
            }

            return inputVariability.Contains('C');
        }

        //**********************************************************************************************
        // FindSubWords
        //
        // Split a word into subwords in the input.
        // Example: Say X Y -> XY 
        // will return component indexes 1 and 2 (2nd and 3rd word of the input)
        //**********************************************************************************************
        private static bool FindSubWords(string word, string[] inputWords, ref List<int> componentIndexes)
        {
            for (int i = 0; i < inputWords.Length; i++)
            {
                if (word.StartsWith(inputWords[i]))
                {
                    // End of recursion, no more string left
                    if (word.Length == inputWords[i].Length)
                    {
                        componentIndexes.Insert(0, i);
                        return true;
                    }

                    // Keep searching for compounds of the rest of the string
                    string restOfWord = word.Substring(inputWords[i].Length, word.Length - inputWords[i].Length);
                    if (FindSubWords(restOfWord, inputWords, ref componentIndexes))
                    {
                        componentIndexes.Insert(0, i);
                        return true;
                    }
                }
            }

            return false;
        }

        //**********************************************************************************************
        // AbstractGenericRule
        //
        // Verify if the facts provided are a rule or not. Compares two inputs and two outputs
        // to abstract a rule from them (if there is a applicability pattern).
        //**********************************************************************************************
        public bool AbstractGenericRule(string input1, string output1, string input2, string output2, string end)
        {
            // If it has end of sentence character, take it out -we don't want it tied to the last word.
            bool inputHasEndChar = Syntax.RemoveEndCharIfPresent(ref input1, ref input2, end);
            bool inputHasEndSpace = Syntax.RemoveEndCharIfPresent(ref input1, ref input2, " ");
            bool outputHasEndChar = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, end);
            bool outputHasEndSpace = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, " ");

            // Do not learn trivial mappings... (they have to be at least two words of one character in input)
            if (input1.Length < 3 || input2.Length < 3 ||
                !input1.Contains(' ') || !input2.Contains(' '))
                return false;

            // We don't want to abstract a rule from two rather equal samples
            if (input1 == input2 || output1 == output2)
                return false;

            string[] inputWords1 = Syntax.GetWords(input1);
            string[] inputWords2 = Syntax.GetWords(input2);
            string[] outputWords1 = Syntax.GetWords(output1);
            string[] outputWords2 = Syntax.GetWords(output2);

            // The number of words must match
            if (inputWords1.Length != inputWords2.Length || outputWords1.Length != outputWords2.Length)
                return false;

            // Find the words that are common between both input and output and mark them to skip them when exploding IDs
            if (!FindCommonWords(inputWords1, inputWords2, out string inputVariability))
                return false;

            // Locate common elements between initial and final state of the first state
            if (!AbstractRepeatedElements(inputWords1, outputWords1, inputVariability, out string inputPattern1, out string outputPattern1))
                return false;

            // Locate common elements between initial and final state of the second state
            if (!AbstractRepeatedElements(inputWords2, outputWords2, inputVariability, out string inputPattern2, out string outputPattern2))
                return false;

            // Patterns are not matching, it is not a rule
            if (inputPattern1 != inputPattern2 || outputPattern1 != outputPattern2)
                return false;

            // Do not learn uniform rules...
            if (inputPattern1 == outputPattern1)
                return false;

            if (inputHasEndSpace) inputPattern1 += ' ';
            if (inputHasEndChar) inputPattern1 += end;
            if (outputHasEndSpace) outputPattern1 += ' ';
            if (outputHasEndChar) outputPattern1 += end;

            // We have a new rule!
            Rule existingRule = GetRule(inputPattern1, input1, input2);
            if (existingRule == null)
            {
                rules.Add(new Rule(inputPattern1, outputPattern1));
                return true;
            }

            return false;
        }

        //*********************************************************************************************
        // GetRule
        //*********************************************************************************************
        private Rule GetRule(string patternInput, string input1, string input2)
        {
            Rule ruleFound = null;

            // Find a matching rule in the list of rules
            foreach (Rule rule in rules)
            {
                if (patternInput == rule.Input)
                {
                    ruleFound = rule;
                    break;
                }
            }

            // If not found by pattern, try to find it by equivalent pattern rule
            if (ruleFound == null)
            {
                Rule rule1 = FindMatchingRule(input1);
                if (rule1 != null)
                {
                    Rule rule2 = FindMatchingRule(input2);
                    if (rule1 == rule2)
                    {
                        ruleFound = rule1;
                    }
                }
            }

            return ruleFound;
        }

        //**********************************************************************************************
        // FindMatchingRule
        //
        // Find the rule that matches the rule input to provide the output
        //**********************************************************************************************
        public Rule FindMatchingRule(string input)
        {
            foreach (Rule rule in rules)
            {
                if (SentenceMatchesPattern(rule.Input, input))
                {
                    return rule;
                }
            }

            return null;
        }

        //**********************************************************************************************
        // GenerateID
        //
        // Generate an ID string (format #00# where 0s are numbers) given the ID number 
        //**********************************************************************************************
        private static string GenerateId(int idNum)
        {
            string id = idNum.ToString().PadLeft(MaxIdNumLength, '0');
            return $"Ð{id}Ð";
        }

        //**********************************************************************************************
        // IsIDWord
        //
        // Check the fact format (#0000# where 0s are numbers) and return whether the given string is
        // or is not an identifier
        //**********************************************************************************************
        private static bool IsIdWord(string word)
        {
            // Id and separators
            if (word.Length < MaxIdNumLength + 2)
                return false;

            if (word == "")
                return false;

            if (word[0] != 'Ð' || word[MaxIdNumLength + 1] != 'Ð')
                return false;

            for (int i = 1; i < MaxIdNumLength + 1; i++)
            {
                if (word[i] < '0' || word[i] > '9')
                    return false;
            }

            return true;
        }

        //**********************************************************************************************
        // ToString
        //**********************************************************************************************
        public override string ToString()
        {
            string output = "## WORD GENERIC RULES ##" + Environment.NewLine;
            foreach (Rule rule in rules)
            {
                output += rule.ToString();
            }

            return output + Environment.NewLine;
        }
    }
}
