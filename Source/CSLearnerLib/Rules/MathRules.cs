//**********************************************************************************************
// File:   Rules.cs
// Author: Andrés del Campo Novales
//
// This class handles encapsulates the math related rules functionality.
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
using System.Linq;
using System.Diagnostics;

namespace CSLearnerLib
{
    public class MathRules
    {
        private const string IdOperand1 = "Ð1";
        private const string IdOperand2 = "Ð2";
        private const string IdSum = "ÐA";
        private const string IdSub = "ÐS";
        private const string IdMult = "ÐM";
        private const string IdDiv = "ÐD";
        private const string Id = "Ð";

        private readonly List<Rule> rules = new List<Rule>();

        //**********************************************************************************************
        // ApplyCompoundRollingRule
        //
        // Try to match the input to several rules, each of a subset of the input from left to right.
        // It takes the output of each rule found and keeps feeding it to the next rules.
        // Example:  3 + 5 + 7
        //             8   + 7
        //                 15
        //**********************************************************************************************
        public string ApplyCompoundRollingRule(string input, string endChar)
        {
            string[] inputWords = Syntax.GetWordsAndOperands(input);

            // Cycle through rules looking to match a subset of the input
            foreach (Rule rule in rules)
            {
                // When found, the output of the rule is accumulated to the final output
                // and the process repeats for the rest of the string.
                if (SentenceMatchesPattern(rule.Input, input, false))
                {
                    Syntax.SplitSentence(inputWords,
                        NumPatternWordsAfterRemovingEndingInSubRules(rule.Input, input),
                        out string inputSubsetForRule,
                        out string restOfInput);

                    bool lastRule = (restOfInput == string.Empty);

                    string output = ApplyRule(rule, inputSubsetForRule, lastRule);
                    
                    // End of the chain?
                    if (lastRule)
                        return output;

                    Syntax.RemoveEndCharIfPresent(ref output, endChar);
                    string resolvedInput = output + " " + restOfInput;

                    output = ApplyCompoundRollingRule(resolvedInput, endChar);

                    // Have we found a valid set of rules to apply to the rest of the input fragments? 
                    // Exit if yes, or continue searching if no.
                    if (output != string.Empty)
                    {
                        return output;
                    }
                }
            }

            return string.Empty;
        }

        //**********************************************************************************************
        // NumPatternWordsAfterRemovingEndingInSubRules
        //
        // Used for compound rules as we don't want to apply the simple rule ending to each of the rules
        // applied with the compound rule. 
        //**********************************************************************************************
        private int NumPatternWordsAfterRemovingEndingInSubRules(string pattern, string input)
        {
            string[] inputWords = Syntax.GetWordsAndOperands(input);
            string[] patternWords = Syntax.GetWordsAndOperands(pattern);

            int numPatternWords = patternWords.Length;

            if (inputWords.Length > patternWords.Length)
                if (Syntax.RemoveEqualEndOfSentenceCharsIfPresent(ref inputWords, ref patternWords))
                    numPatternWords--;

            return numPatternWords;
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
            // Separate all the strings into words
            string[] inputWords = Syntax.GetWordsAndOperands(inputText);
            string[] patternWords = Syntax.GetWordsAndOperands(pattern);

            // Check the input sentences against the pattern
            // Not the same number of words in the line, not match -if we are supposed to use all input
            if (useAllInput && inputWords.Length != patternWords.Length)
                return false;

            Syntax.RemoveEqualEndOfSentenceCharsIfPresent(ref inputWords, ref patternWords);

            // Either way, the input has to be at least the size of the pattern
            if (inputWords.Length < patternWords.Length)
                return false;

            string operand1 = null;
            string operand2 = null;
            int numBase = 0;

            for (int i = 0; i < patternWords.Length; i++)
            {
                if (patternWords[i].StartsWith(IdOperand1))
                {
                    operand1 = inputWords[i];
                    numBase = int.Parse(patternWords[i].Remove(0, IdOperand1.Length));
                }
                else if (patternWords[i].StartsWith(IdOperand2))
                    operand2 = inputWords[i];
                else if (patternWords[i] != inputWords[i])
                    return false;
            }
            Debug.Assert(operand1 != null && operand2 != null && numBase != 0);

            int minBaseOperand1 = DetermineMinBase(operand1);
            int minBaseOperand2 = DetermineMinBase(operand2);
            int maxBaseOperands = Math.Max(minBaseOperand1, minBaseOperand2);

            return maxBaseOperands <= numBase;
        }

        //**********************************************************************************************
        // ApplyRule
        //
        // Extract the different facts from the recent input and provides the output of the rule
        //**********************************************************************************************
        private string ApplyRule(Rule rule, string input, bool lastRule = true)
        {
            string[] patternInputWords = Syntax.GetWordsAndOperands(rule.Input);
            string[] inputWords = Syntax.GetWordsAndOperands(input);
            string output = rule.Output;

            string operand1 = null;
            string operand2 = null;
            int numBaseOperands = -1;

            for (int i=0; i<patternInputWords.Length; i++)
            {
                if (patternInputWords[i].StartsWith(IdOperand1))
                {
                    operand1 = inputWords[i];
                    numBaseOperands = int.Parse(patternInputWords[i].Substring(IdOperand1.Length, 2));
                }
                if (patternInputWords[i].StartsWith(IdOperand2))
                    operand2 = inputWords[i];
            }
            Debug.Assert(numBaseOperands == 2 || numBaseOperands == 8 || numBaseOperands == 10 || numBaseOperands == 16);

            int index = output.IndexOf(Id, StringComparison.InvariantCulture);
            Debug.Assert(index != -1);
            string operation = output.Substring(index, IdSum.Length + 2);

            string result = ApplyOperation(operand1, operand2, numBaseOperands, operation, lastRule);
            Debug.Assert(result != string.Empty);
            output = output.Replace(operation, result);

            return output;
        }

        //*********************************************************************************************
        // GetRule
        //*********************************************************************************************
        private Rule GetRule(string patternInput)
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

            return ruleFound;
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
            if (input1.Length < 3 || input2.Length < 3)
                return false;

            // We don't want to abstract a rule from two rather equal samples
            if (input1 == input2)
                return false;

            // If it has end of sentence character, take it out -we don't want it tied to the last word.
            bool inputHasEndChar = Syntax.RemoveEndCharIfPresent(ref input1, ref input2, end);
            bool inputHasEndSpace = Syntax.RemoveEndCharIfPresent(ref input1, ref input2, " ");
            bool outputHasEndChar = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, end);
            bool outputHasEndSpace = Syntax.RemoveEndCharIfPresent(ref output1, ref output2, " ");

            string[] inputWords1 = Syntax.GetWordsAndOperands(input1);
            string[] inputWords2 = Syntax.GetWordsAndOperands(input2);
            string[] outputWords1 = Syntax.GetWordsAndOperands(output1);
            string[] outputWords2 = Syntax.GetWordsAndOperands(output2);

            // The number of words must match or the outputs have more than one word
            if (inputWords1.Length != inputWords2.Length || outputWords1.Length != outputWords2.Length)
                return false;

            // Find the words that are common between both input and output and mark them to skip them when exploding IDs
            if (!WordGenericRules.FindCommonWords(inputWords1, inputWords2, out string inputVariability))
                return false;
            WordGenericRules.FindCommonWords(outputWords1, outputWords2, out string outputVariability);

            // We support two operand with one result rules only (i.e. A + B -> C)
            if (inputVariability.Replace("C", "").Length != 2 || outputVariability.Replace("C", "").Length != 1)
                return false;

            // Locate common chars between inputs and outputs and generalize the strings
            if (!AbstractOperation(inputWords1, outputWords1, inputVariability, outputVariability, out string inputPattern1, out string outputPattern1))
                return false;
            if (!AbstractOperation(inputWords2, outputWords2, inputVariability, outputVariability, out string inputPattern2, out string outputPattern2))
                return false;

            // They must match completely
            if (inputPattern1 != inputPattern2 || outputPattern1 != outputPattern2)
                return false;

            if (inputHasEndSpace) inputPattern1 += ' ';
            if (inputHasEndChar) inputPattern1 += end;
            if (outputHasEndSpace) outputPattern1 += ' ';
            if (outputHasEndChar) outputPattern1 += end;

            // We have a new rule!
            Rule existingRule = GetRule(inputPattern1);
            if (existingRule == null)
            {
                Rule newRule = new Rule(inputPattern1, outputPattern1);
                rules.Add(newRule);
                return true;
            }

            return false;
        }

        //**********************************************************************************************
        // AbstractOperation
        //
        // From input and output strings presplit in word lists, abstract the common words and operations.
        //
        // Example:     
        //      input: "SUM: A B"
        //      output: "C"
        //      inputVariability: "CVV"
        //      outputVariability: "V"
        // will provide the output: 
        //      input: "SUM: #01# #02#"
        //      output: "#SUM#"
        //**********************************************************************************************
        private static bool AbstractOperation(string[] inputWords, string[] outputWords,
            string inputVariability, string outputVariability,
            out string inputPattern, out string outputPattern)
        {
            Debug.Assert(inputVariability.Replace("C", "").Length == 2 && 
                         outputVariability.Replace("C", "").Length == 1);

            bool firstOperand = true;
            string operand1 = null;
            string operand2 = null;

            for (int i = 0; i < inputWords.Length; i++)
            {
                // Skip constant words
                if (inputVariability[i] == 'C')
                    continue;

                if (firstOperand)
                {
                    operand1 = inputWords[i];
                    inputWords[i] = IdOperand1;
                    firstOperand = false;
                }
                else
                {
                    operand2 = inputWords[i];
                    inputWords[i] = IdOperand2;
                }
            }

            Debug.Assert(operand1 != null && operand2 != null);

            int resultIndex = outputVariability.IndexOf('V');
            string result = outputWords[resultIndex];

            string resultId = DetermineOperation(operand1, operand2, result, out int numBaseOperands);
            if (string.IsNullOrEmpty(resultId))
            {
                inputPattern = outputPattern = string.Empty;
                return false;
            }

            outputWords[resultIndex] = resultId;

            inputPattern = string.Join(" ", inputWords);
            outputPattern = string.Join(" ", outputWords);

            // Add the base to the operands
            inputPattern = inputPattern.Replace(IdOperand1, IdOperand1 + numBaseOperands.ToString("00"));
            inputPattern = inputPattern.Replace(IdOperand2, IdOperand2 + numBaseOperands.ToString("00"));

            return true;
        }

        //*********************************************************************************************
        // DetermineOperation
        //
        // Returns an identifier of the arithmetic operation that matches 
        //      operand1 OPERATION operand2 = result
        // and returns the base in which the operands are in.
        //*********************************************************************************************
        private static string DetermineOperation(string operand1, string operand2, string result, out int numBaseOperands)
        {
            string id = string.Empty;
            numBaseOperands = 0;

            if (string.IsNullOrEmpty(operand1) || string.IsNullOrEmpty(operand2) || string.IsNullOrEmpty(result))
                return id;

            int minBaseOperands = DetermineMinBase(operand1, operand2);
            int minBaseResult = DetermineMinBase(result);

            if (minBaseOperands == int.MaxValue)
                return id;

            // Try the different possible numeric bases. If more than one succeeds, decline due to ambiuous result.
            if (minBaseOperands == 2)
            {
                numBaseOperands = 2;
                id = DetermineOperationMultiBaseResult(operand1, operand2, result, 2, minBaseResult);
            }
            if (minBaseOperands == 8 || (minBaseOperands == 2 && id == string.Empty))
            {
                numBaseOperands = 8;
                string idTemp = DetermineOperationMultiBaseResult(operand1, operand2, result, 8, minBaseResult);
                if (id != string.Empty)
                    return string.Empty;
                id = idTemp;
            }
            if (minBaseOperands == 10 || (minBaseOperands <= 8 && id == string.Empty))
            {
                numBaseOperands = 10;
                string idTemp = DetermineOperationMultiBaseResult(operand1, operand2, result, 10, minBaseResult);
                if (id != string.Empty)
                    return string.Empty;
                id = idTemp;
            }
            if (minBaseOperands == 16 || (minBaseOperands <= 10 && id == string.Empty))
            {
                numBaseOperands = 16;
                string idTemp = DetermineOperationMultiBaseResult(operand1, operand2, result, 16, minBaseResult);
                if (id != string.Empty)
                    return string.Empty;
                id = idTemp;
            }

            return id;
        }

        //*********************************************************************************************
        // DetermineOperationMultiBaseResult
        //
        // Determine an operation knowing the base of the operands but not the base of the result.
        //*********************************************************************************************
        private static string DetermineOperationMultiBaseResult(string operand1, string operand2, string result,
            int numBaseOperands, int minBaseResult)
        {
            string id = string.Empty;

            if (minBaseResult == 2)
            {
                id = DetermineOperationGivenBases(operand1, operand2, result, numBaseOperands, 2);
            }
            if (minBaseResult == 8 || (minBaseResult == 2 && id == string.Empty))
            {
                string idTemp = DetermineOperationGivenBases(operand1, operand2, result, numBaseOperands, 8);
                if (id != string.Empty)
                    return string.Empty;
                id = idTemp;
            }
            if (minBaseResult == 10 || (minBaseResult <= 8 && id == string.Empty))
            {
                string idTemp = DetermineOperationGivenBases(operand1, operand2, result, numBaseOperands, 10);
                if (id != string.Empty)
                    return string.Empty;
                id = idTemp;
            }
            if (minBaseResult == 16 || (minBaseResult <= 10 && id == string.Empty))
            {
                string idTemp = DetermineOperationGivenBases(operand1, operand2, result, numBaseOperands, 16);
                if (id != string.Empty)
                    return string.Empty;
                id = idTemp;
            }

            return id;
        }

        //*********************************************************************************************
        // DetermineOperationGivenBases
        //
        // Determine an operation knowing the base of the operands AND the base of the result.
        //*********************************************************************************************
        private static string DetermineOperationGivenBases(string operand1, string operand2, string result, 
                                                int numBaseOperands, int numBaseResult)
        {
            int op1, op2, res;

            try
            {
                op1 = Convert.ToInt32(operand1, numBaseOperands);
                op2 = Convert.ToInt32(operand2, numBaseOperands);
                res = Convert.ToInt32(result, numBaseResult);
            }
            catch
            {
                return string.Empty;
            }

            string id = string.Empty;
            string strBase = numBaseResult.ToString("00");

            if (op1 + op2 == res)
                id = IdSum + strBase;
            else if (op1 - op2 == res)
                id = IdSub + strBase;
            else if (op1 * op2 == res)
                id = IdMult + strBase;
            else if (op1 / (op2 == 0 ? 1 : op2) == res)
                id = IdDiv + strBase;

            return id;
        }

        //*********************************************************************************************
        // DetermineMinBase
        //
        // Returns the minimum common numeric base in which the operands and result are.
        //*********************************************************************************************
        private static int DetermineMinBase(string operand1, string operand2)
        {
            int op1MinBase = DetermineMinBase(operand1);
            int op2MinBase = DetermineMinBase(operand2);

            int minBase = Math.Max(op1MinBase, op2MinBase);
            return minBase;
        }

        private static int DetermineMinBase(string operand)
        {
            bool isMaxBase16 = operand.All(c => "0123456789ABCDEFabcdef".Contains(c));
            if (!isMaxBase16)
                return int.MaxValue;    // Base not recognized

            bool isMinBase16 = operand.Any(c => "ABCDEFabcdef".Contains(c));
            if (isMinBase16)
                return 16;              // Hex for certain

            bool isMinBase10 = operand.Any(c => "89".Contains(c));
            if (isMinBase10)
                return 10;              // Base 10 or 16...

            bool isMinBase8 = operand.Any(c => "234567".Contains(c));
            if (isMinBase8)
                return 8;               // Base 8, 10 or 16...

            return 2;                   // Base 2, 8, 10 or 16...
        }

        //*********************************************************************************************
        // ApplyOperation
        //
        // Applies the operation to the operands to obtain a result.
        //*********************************************************************************************
        private string ApplyOperation(string operand1, string operand2, int numBaseOperands, string operationAndBase, bool lastRule)
        {
            Debug.Assert(operationAndBase.Length == 4);
            if (string.IsNullOrEmpty(operand1) || string.IsNullOrEmpty(operand2) || string.IsNullOrEmpty(operationAndBase))
                return string.Empty;

            string operation = operationAndBase.Substring(0, IdSum.Length);

            int numBaseResult;
            int result;
            try
            {
                numBaseResult = int.Parse(operationAndBase.Remove(0, IdSum.Length));
                Debug.Assert(numBaseResult == 2 || numBaseResult == 8 || numBaseResult == 10 || numBaseResult == 16);

                int op1 = Convert.ToInt32(operand1, numBaseOperands);
                int op2 = Convert.ToInt32(operand2, numBaseOperands);
                result = Int32.MinValue;

                switch (operation)
                {
                    case IdSum:
                        result = op1 + op2;
                        break;

                    case IdSub:
                        result = op1 - op2;
                        break;

                    case IdMult:
                        result = op1 * op2;
                        break;

                    case IdDiv:
                        result = op1 / op2;
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            catch
            {
                return string.Empty;
            }

            if (result == Int32.MinValue)
                return string.Empty;

            if (lastRule)
                return Convert.ToString(result, numBaseResult);

            // Intermediate rules need to output the result in the same base as the operands in order to keep
            // concatenating rules.
            return Convert.ToString(result, numBaseOperands);
        }

    }
}
