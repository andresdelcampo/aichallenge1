//**********************************************************************************************
// File:   MappingRules.cs
// Author: Andrés del Campo Novales
//
// This class handles mapping rules (input -> output), or in other words, given an input,
// there is an output. It mostly encapsulates basic functionality for the data structure.
// It also handles the existence of uniform output (in which all rules output the same value).
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

namespace CSLearnerLib
{
    [Serializable]
    public class MappingRules
    {
        private Rules rules = new Rules();

        // Special case of X -> c 
        public string UniformValue { get; private set; } = string.Empty;
        private bool uniformOutput = true;

        public bool IsUniform()
        {
            return uniformOutput && !string.IsNullOrEmpty(UniformValue);
        }

        public void ResetRules()
        {
            rules = new Rules();
        }

        public Rule Retrieve(string input)
        {
            return rules.Retrieve(input);
        }

        public List<string> RetrieveOutputsSortedByFreq()
        {
            return rules.RetrieveOutputsSortedByFreq();
        }
        
        public void Successful(string input, string output)
        {
            if (uniformOutput)
            {
                if (UniformValue == string.Empty)
                {
                    UniformValue = output;
                }
                else if (UniformValue != output)
                {
                    uniformOutput = false;
                    UniformValue = string.Empty;
                }
            }

            rules.Successful(input, output);
        }

        public void Failed(string input, string output)
        {
            if (output == UniformValue)
            {
                uniformOutput = false;
                UniformValue = string.Empty;
            }

            rules.Failed(input, output);
        }

        public bool IsFailed(Rule rule, string input, string output)
        {
            return IsFailedRule(rule, input, output)
                || IsFailedUniform(output);
        }

        public bool IsFailedRule(Rule rule, string input, string output)
        {
            return rule != null && 
                   rule.Input != string.Empty &&
                   rule.Input == input && 
                   (rule.Output == output || rule.HasFailedOutput(output));
        }

        private bool IsFailedUniform(string output)
        {
            return IsUniform() && UniformValue == output;
        }
    }
}
