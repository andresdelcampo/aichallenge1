//**********************************************************************************************
// File:   CharGenericRules.cs
// Author: Andrés del Campo Novales
//
// This class handles rules created from repeated patterns in which the element is characters.
// It can abstract rules similar to this one, based on examples:
//      Reverse XY -> YX
//      Reverse #001##002# -> #002##001#
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
    public class CharGenericRules
    {
        private readonly List<Rule> rules = new List<Rule>();

        private const int MaxIdNumLength = 3;
        public const int IdLength = MaxIdNumLength + 2;

        public Rule this[int index] => rules[index];

        public int Count => rules.Count;

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

                    if (restOfInput == string.Empty)
                    {
                        // End of the chain
                        string output = ApplyRule(rule, inputSubsetForRule);
                        return output;
                    }

                    string innerOutput = ApplyCompoundMatchingRule(restOfInput);

                    // Have we found a valid set of rules to apply to the rest of the input fragments? 
                    // Exit if yes, or continue searching if no.
                    if (innerOutput != string.Empty)
                    {
                        string output = ApplyRule(rule, inputSubsetForRule);
                        return output + " " + innerOutput;
                    }
                }
            }

            return string.Empty;
        }

        //**********************************************************************************************
        // FailedRule
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
        private Rule FindMatchingRule(string input)
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
        // ApplyClosestRule
        //
        // Find the best rule that has its pattern closest to the input
        //**********************************************************************************************
        public string ApplyClosestRule(string input)
        {
            float bestApplicability = 0;
            Rule bestRule = null;

            foreach (Rule rule in rules)
            {
                // Look for the heaviest rule (the one that meets more preconditions steps)
                float applicability = GetApplicability(rule, input);
                if (applicability >= bestApplicability)
                {
                    bestApplicability = applicability;
                    bestRule = rule;
                }
            }

            return bestApplicability > 0 ? 
                        ApplyRule(bestRule, input) : string.Empty;
        }


        //**********************************************************************************************
        // GetApplicability
        //
        // Score the rule against the input provided. The maximum score is 1. 
        // Each equal word increases proportionally the score
        //**********************************************************************************************
        private float GetApplicability(Rule rule, string input)
        {
            Dictionary<string, char> seenIDs = new Dictionary<string, char>();
            float ruleScore = 0.0f;

            string[] inputWords = Syntax.GetWords(input);
            string[] patternWords = Syntax.GetWords(rule.Input);
            
            if (inputWords.Length != patternWords.Length)
                return 0;

            float maxScorePerWord = 1.0f / patternWords.Length;

            for (int word = 0; word < patternWords.Length; word++)
            {
                // It is not a variable word, and it is not the same, not match
                if (!IsIdWord(patternWords[word]))
                {
                    if (inputWords[word] == patternWords[word])
                        ruleScore += maxScorePerWord;
                }
                else
                {
                    // It is a variable word, check each of its characters
                    float maxScorePerChar = maxScorePerWord / patternWords[word].Length;
                    string id;
                    int ic, pc;
                    for (ic = 0, pc = 0;
                         ic < inputWords[word].Length && pc < patternWords[word].Length; 
                         ic++, pc+=id.Length)
                    {
                        id = ExtractIdOrChar(patternWords[word], pc);

                        if (id.Length == 0)
                        {
                            // We ran out of pattern!! No more score!
                            break;
                        }

                        if (id.Length == 1)
                        {
                            // Not really an ID! Matching the input scores
                            if (inputWords[word][ic] == id[0])
                                ruleScore += maxScorePerChar;
                        }
                        else 
                        {
                            // It IS an ID - Check if the char has been seen before or is new
                            if (seenIDs.ContainsKey(id))
                            {
                                if (inputWords[word][ic] == seenIDs[id])
                                    ruleScore += maxScorePerChar * IdLength;
                            }
                            else
                            {
                                seenIDs.Add(id, inputWords[word][ic]);
                                ruleScore += maxScorePerChar * IdLength;
                            }
                        }
                    }

                    // Do not leave any pattern unmatched or we may miss IDs!
                    if (pc < patternWords[word].Length)
                    {
                        ruleScore = 0;
                        break;
                    }
                }
            }

            return ruleScore;
        }

        //**********************************************************************************************
        // ApplyRule
        //
        // Extract the different facts from the input and provides the output of the rule
        //**********************************************************************************************
        private static string ApplyRule(Rule rule, string input)
        {
            var curFacts = new Dictionary<string, char>();
            ExtractFactsFromStr(rule.Input, input, ref curFacts);
            return ApplyFactsToStr(rule.Output, curFacts);
        }

        //**********************************************************************************************
        // ApplyRule
        //
        // Extract the different facts from the input and provides the output of the rule
        //**********************************************************************************************
        public static string ApplyRule(string inputPattern, string outputPattern, string input)
        {
            var curFacts = new Dictionary<string, char>();
            ExtractFactsFromStr(inputPattern, input, ref curFacts);
            return ApplyFactsToStr(outputPattern, curFacts);
        }

        //**********************************************************************************************
        // SentenceMatchesPattern
        //
        // Verify whether a string matches the rule input string with factIDs inside. 
        //
        // Example that will match: 
        // pattern: "Hello Ð001ÐÐ002ÐÐ003ÐÐ002ÐÐ004Ð"     inputText: "Hello Peter"
        //**********************************************************************************************
        private static bool SentenceMatchesPattern(string pattern, string inputText, bool useAllInput = true)
        {
            Dictionary<string, char> seenIDs = new Dictionary<string, char>();

            // Separate all the strings into words
            string[] inputWords = Syntax.GetWords(inputText);
            string[] patternWords = Syntax.GetWords(pattern);

            // Check the input sentences against the pattern
            // Not the same number of words in the line, not match -if we are supposed to use all input
            if (useAllInput && inputWords.Length != patternWords.Length)
                return false;

            Syntax.RemoveEqualEndOfSentenceCharsIfPresent(ref inputWords, ref patternWords);

            // Either way, the input has to be at least the size of the pattern
            if (inputWords.Length < patternWords.Length)
                return false;

            bool match = true;
            for (int word = 0; word < patternWords.Length && match; word++)
            {
                // It is not a variable word, and it is not the same, not match
                if (!IsIdWord(patternWords[word]))
                {
                    if (inputWords[word] != patternWords[word])
                        match = false;
                }
                else
                {
                    // It is a variable word, check each of its characters
                    int p = 0;
                    for (int c = 0; c < inputWords[word].Length && match; c++)
                    {
                        string id = ExtractIdOrChar(patternWords[word], p);

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

            return match;
        }

        //**********************************************************************************************
        // ExtractFactsFromStr
        //
        // Extract values from a inputText string where they match the preconditions string with IDs 
        // inside
        //
        // Example: 
        // preconditions: "Hello Ð001ÐÐ002ÐÐ003ÐÐ002ÐÐ004Ð"
        // inputText: "Hello Peter"
        // will add to our facts database: Ð001Ð = P, Ð002Ð = e, Ð003Ð = t, Ð004Ð = r
        //**********************************************************************************************
        private static void ExtractFactsFromStr(string pattern, string inputText, ref Dictionary<string, char> curFacts)
        {
            string[] inputWords = Syntax.GetWords(inputText);
            string[] patternWords = Syntax.GetWords(pattern);

            Debug.Assert(inputWords.Length == patternWords.Length || 
                        (inputWords.Length == patternWords.Length + 1 && inputWords[inputWords.Length-1].Length <= 2));

            for (int i = 0; i < patternWords.Length; i++)
            {
                if (IsIdWord(patternWords[i]))
                {
                    for (int j = 0, p = 0; j < inputWords[i].Length; j++)
                    {
                        string id = ExtractIdOrChar(patternWords[i], p);
                        if (id.Length == IdLength)  
                            curFacts[id] = inputWords[i][j];
                        p += id.Length;
                    }
                }
            }
        }

        //**********************************************************************************************
        // ApplyFactsToStr
        //
        // Resolve IDs (Ð001Ð) in a string to their current char value stored in this class.
        // This is the opposite of ExtractFactsFromStr
        //**********************************************************************************************
        private static string ApplyFactsToStr(string outputText, Dictionary<string, char> curFacts)
        {
            string[] outputWords = Syntax.GetWords(outputText);

            foreach (string word in outputWords)
            {
                if (IsIdWord(word))
                {
                    string id;
                    for (int j = 0; j < word.Length; j+=id.Length)
                    {
                        id = ExtractIdOrChar(word, j);

                        if (id.Length > 1)
                        {
                            outputText = outputText.Replace(id, curFacts[id].ToString());
                        }
                    }
                }
            }

            return outputText;
        }

        //**********************************************************************************************
        // AbstractGenericRule
        //
        // Verify if the facts provided are a rule or not. Compares two inputs and two outputs
        // to abstract a rule from them (if there is a applicability pattern).
        //**********************************************************************************************
        public bool AbstractGenericRule(string input1, string output1, string input2, string output2, string end)
        {
            // Do not learn trivial mappings... (they have to be at least two words of one character in input)
            if (input1.Length < 3 || input2.Length < 3 ||
                !input1.Contains(' ') || !input2.Contains(' '))
                return false;

            // We don't want to abstract a rule from two rather equal samples
            if (input1 == input2)
                return false;

            // If it has end of sentence character, take it out -we don't want it tied to the last word.
            bool inputHasEndChar = Syntax.RemoveEndCharIfPresent(ref input1, ref input2, end);
            bool inputHasEndSpace = Syntax.RemoveEndCharIfPresent(ref input1, ref input2, " ");
            bool outputHasEndChar = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, end);
            bool outputHasEndSpace = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, " ");

            string[] inputWords1 = Syntax.GetWords(input1);
            string[] inputWords2 = Syntax.GetWords(input2);
            string[] outputWords1 = Syntax.GetWords(output1);
            string[] outputWords2 = Syntax.GetWords(output2);

            // The number of words must match
            if (inputWords1.Length != inputWords2.Length || outputWords1.Length != outputWords2.Length)
                return false;

            // Find the words that are common between both input and output and mark them to skip them when exploding IDs
            if (!WordGenericRules.FindCommonWords(inputWords1, inputWords2, out string inputVariability))
                return false;

            // Locate common chars between inputs and outputs and generalize the strings
            AbstractRepeatedChars(inputWords1, outputWords1, inputVariability, out string inputPattern1, out string outputPattern1);
            AbstractRepeatedChars(inputWords2, outputWords2, inputVariability, out string inputPattern2, out string outputPattern2);

            if (!ValidateEquivalentPatterns(inputPattern1, outputPattern1, input1, output1, 
                                            inputPattern2, outputPattern2, input2, output2,
                                            out string inputPattern, out string outputPattern))
                return false;

            if (inputHasEndSpace) inputPattern += ' ';
            if (inputHasEndChar) inputPattern += end;
            if (outputHasEndSpace) outputPattern += ' ';
            if (outputHasEndChar) outputPattern += end;

            // We have a new rule!
            Rule existingRule = GetRule(inputPattern, input1, input2);
            if (existingRule == null)
            {
                Rule newRule = new Rule(inputPattern, outputPattern);
                rules.Add(newRule);
                return true;
            }

            return false;
        }

        //**********************************************************************************************
        // ValidateEquivalentPatterns
        //
        // The patterns are equivalent if by crossing the inputs and output with the pattern of the other 
        // rule, they still produce the same result. Then the most specific rule should be returned.
        //
        // Example: 
        //      pattern1: #001# #002# -> #002# #001#        a b -> b a
        //      pattern2: #001# #001# -> #001# #001#        a a -> a a
        // Passing input a a to pattern1 should still return a a and therefore applicable, but the final
        // rule must be pattern1 as it is more specific.
        //**********************************************************************************************
        private bool ValidateEquivalentPatterns(string inputPattern1, string outputPattern1, string input1, string output1, 
                                                string inputPattern2, string outputPattern2, string input2, string output2,
                                                out string inputPattern, out string outputPattern)
        {
            // Let's assume rule 1 is more specific
            inputPattern = inputPattern1;
            outputPattern = outputPattern1;

            // Equal rules are definitely equivalent...
            if (inputPattern1 == inputPattern2 && outputPattern1 == outputPattern2)
                return true;

            // Cross validate the rules (if actually possible! -check first)
            var rule1 = new Rule(inputPattern1, outputPattern1);
            var rule2 = new Rule(inputPattern2, outputPattern2);

            bool input2MatchesPattern1 = SentenceMatchesPattern(inputPattern1, input2);
            bool input1MatchesPattern2 = SentenceMatchesPattern(inputPattern2, input1);

            if (!input2MatchesPattern1 && !input1MatchesPattern2)
                return false;

            // Both match each other, choose the most specific
            if (input1MatchesPattern2 && input2MatchesPattern1)
            {
                string output12 = ApplyRule(rule2, input1);
                string output21 = ApplyRule(rule1, input2);

                if (output12 != output1 || output21 != output2)
                    return false;

                // Find the most specific one -all we need is the number of IDs actually. Can be optimized...
                var ids1 = new Dictionary<string, char>();
                ExtractFactsFromStr(rule1.Input, input1, ref ids1);
                var ids2 = new Dictionary<string, char>();
                ExtractFactsFromStr(rule2.Input, input2, ref ids2);

                // Actually 2 is more specific, switch.
                if (ids2.Count > ids1.Count)
                {
                    inputPattern = inputPattern2;
                    outputPattern = outputPattern2;
                }
            }
            else if(input1MatchesPattern2)
            {
                // The good rule is the second -if it passes cross validation
                inputPattern = inputPattern2;
                outputPattern = outputPattern2;

                string output12 = ApplyRule(rule2, input1);

                if (output12 != output1)
                    return false;
            }
            else
            {
                // The good rule is the first (already assigned) -if it passes cross validation
                string output21 = ApplyRule(rule1, input2);

                if (output21 != output2)
                    return false;
            }

            return true;
        }
        
        //**********************************************************************************************
        // AbstractRepeatedChars
        //
        // From input and output strings presplit in word lists, abstract the common words.
        //
        // Example:     
        //      input: "Reverse: Biker"
        //      output: "rekiB"
        //      inputVariability: "CV"
        // will provide the output: 
        //      input: "Reverse: Ð001ÐÐ002ÐÐ003ÐÐ004ÐÐ005Ð"
        //      output: "Ð005ÐÐ004ÐÐ003ÐÐ002ÐÐ001Ð"
        //**********************************************************************************************
        private static void AbstractRepeatedChars(string[] inputWords, string[] outputWords,
                                                  string inputVariability,
                                                  out string inputPattern, out string outputPattern)
        {
            var generatedIds = new Dictionary<char, int>();
            var analyzedIds = new List<char>();

            int nextId = 1;

            // For each output word
            for (int ow = 0; ow < outputWords.Length; ow++)
            {
                // For each char in the output word
                int owStep;
                for (int oc=0; oc < outputWords[ow].Length; oc+=owStep)
                {
                    string id = string.Empty;
                    char currentChar = outputWords[ow][oc];
                    owStep = 1;

                    // We already traversed the input for this char
                    if (generatedIds.ContainsKey(currentChar))
                    {
                        // Replace the char with the ID string in the input and output place 
                        id = GenerateId(generatedIds[currentChar]);
                        outputWords[ow] = outputWords[ow].Substring(0, oc) + id + outputWords[ow].Substring(oc + 1, outputWords[ow].Length - 1 - oc);
                        owStep = id.Length;
                        continue;
                    }
                    else if (analyzedIds.Contains(currentChar))
                    {
                        // The char is analyzed and not present in the input -just skip it 
                        // and keep it as constant in the output
                        continue;
                    }

                    analyzedIds.Add(currentChar);

                    // For each input word
                    for (int iw=0; iw<inputWords.Length; iw++)
                    {
                        // Skip constant words
                        if (inputVariability[iw] == 'C')
                            continue;

                        // For each char in the input word
                        int iwStep;
                        for (int ic=0; ic < inputWords[iw].Length; ic += iwStep)
                        {
                            iwStep = 1;
                            if (inputWords[iw][ic] == currentChar)
                            {
                                if (id == string.Empty)
                                {
                                    generatedIds.Add(currentChar, nextId);
                                    id = GenerateId(nextId++);
                                    outputWords[ow] = outputWords[ow].Substring(0, oc) + id + outputWords[ow].Substring(oc + 1, outputWords[ow].Length - 1 - oc);
                                    owStep = id.Length;
                                }

                                // Replace the char with the ID string in the input and output place 
                                inputWords[iw] = inputWords[iw].Substring(0, ic) + id + inputWords[iw].Substring(ic+1, inputWords[iw].Length -1-ic);
                                iwStep = id.Length;
                            }
                        }
                    }
                }
            }

            // Replace variable words in the input with identifiers
            for (int iw1 = 0; iw1 < inputWords.Length; iw1++)
            {
                if (inputVariability[iw1] == 'C')
                    continue;

                string currentWord = inputWords[iw1];

                string id;
                for (int ic = 0; ic < currentWord.Length; ic += id.Length)
                {
                    id = ExtractIdOrChar(currentWord, ic);
                    char currentChar = currentWord[ic];

                    if (id.Length == 1)
                    {
                        if (generatedIds.ContainsKey(currentChar))
                        {
                            id = GenerateId(generatedIds[currentChar]);
                        }
                        else
                        {
                            generatedIds.Add(currentChar, nextId);
                            id = GenerateId(nextId++);
                        }
                        currentWord = currentWord.Substring(0, ic) + id + currentWord.Substring(ic + 1, currentWord.Length - 1 - ic);
                    }
                }
                inputWords[iw1] = currentWord;
            }

            inputPattern = string.Join(" ", inputWords);
            outputPattern = string.Join(" ", outputWords);
        }

        //**********************************************************************************************
        // GenerateId
        //
        // Generate an ID string (format Ð00Ð where 0s are numbers) given the ID number 
        //**********************************************************************************************
        public static string GenerateId(int idNum)
        {
            string id = idNum.ToString().PadLeft(MaxIdNumLength, '0');
            return $"Ð{id}Ð";
        }

        //**********************************************************************************************
        // IsIdWord
        //
        // Check the ID format (Ð0000Ð where 0s are numbers) and return whether the given word contains
        // any char IDs
        //**********************************************************************************************
        public static bool IsIdWord(string word)
        {
            if (word.Length < IdLength)
                return false;

            if (word == "")
                return false;

            for (int i=0; i<word.Length - IdLength + 1; i++)
            {
                if (word[i] == 'Ð')
                {
                    if (word[i] != 'Ð' || word[i + IdLength - 1] != 'Ð')
                        return false;

                    return true;
                }
            }

            return false;
        }

        //**********************************************************************************************
        // ExtractIdOrChar
        //
        // Extract an ID (Ð001Ð) or single char from a word. It does NOT detect if within an ID already.
        // For example:
        //      word: TeÐ001Ðst
        //      pos: 1              ->  e
        //      pos: 2              ->  Ð001Ð
        //      pos: 3              ->  0 (Beware!!)
        //**********************************************************************************************
        public static string ExtractIdOrChar(string word, int pos)
        {
            if (pos >= word.Length)
                return string.Empty;

            if (pos < word.Length - IdLength + 1)
            {
                string id = word.Substring(pos, IdLength);
                if (IsIdWord(id))
                    return id;
            }

            return word[pos].ToString();
        }
        
        //**********************************************************************************************
        // ToString
        //**********************************************************************************************
        public override string ToString()
        {
            string output = "## CHAR GENERIC RULES ##" + Environment.NewLine;
            foreach (Rule rule in rules)
            {
                output += rule.ToString();
            }

            return output + Environment.NewLine;
        }
    }
}
