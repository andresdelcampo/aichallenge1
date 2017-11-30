//**********************************************************************************************
// File:   Rules.cs
// Author: Andrés del Campo Novales
//
// This class handles encapsulates the mapping rules functionality and adds an output frequency
// to retrieve most popular mappings.
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
    public class Rules
    {
        public Dictionary<string, Rule> rules { get; } = new Dictionary<string, Rule>();
        private readonly Dictionary<string, int> outputFrequency = new Dictionary<string, int>();

        public void Successful(string input, string output)
        {
            if (rules.ContainsKey(input))
            {
                Rule rule = rules[input];
                ReduceOutputFrequency(rule.Output);
                rule.Output = output;
                rule.FailedOutputs.Remove(output);
                rules[input] = rule;
            }
            else
            {
                rules.Add(input, new Rule(input, output));
            }

            AddOutputFrequency(output);
        }

        public void Failed(string input, string output)
        {
            if (rules.ContainsKey(input))
            {
                Rule rule = rules[input];
                ReduceOutputFrequency(rule.Output);
                rule.Output = string.Empty;
                if(rule.FailedOutputs.Contains(output) == false)
                    rule.FailedOutputs.Add(output);
                rules[input] = rule;
            }
            else
            {
                rules.Add(input, new Rule(input, string.Empty, new List<string> { output }));
            }
        }

        private void AddOutputFrequency(string output)
        {
            Debug.Assert(output != string.Empty);

            int frequency = 0;
            if (outputFrequency.ContainsKey(output))
            {
                frequency = outputFrequency[output];
            }

            outputFrequency[output] = frequency + 1;
        }

        private void ReduceOutputFrequency(string output)
        {
            if (output != string.Empty)
            {
                int frequency = 0;
                if (outputFrequency.ContainsKey(output))
                {
                    frequency = outputFrequency[output];
                }

                outputFrequency[output] = frequency - 1;
            }
        }

        public Rule Retrieve(string input)
        {
            return rules.ContainsKey(input) ? rules[input] : null;
        }

        public List<string> RetrieveOutputsSortedByFreq()
        {
            List<string> outputs;

            if (outputFrequency.Count > 0)
            {
                var outputFrequencySorted = outputFrequency.ToList();
                outputFrequencySorted.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
                outputs = (from ofs in outputFrequencySorted select ofs.Key).ToList();
            }
            else
                outputs = new List<string>();

            return outputs;
        }
    }
}
