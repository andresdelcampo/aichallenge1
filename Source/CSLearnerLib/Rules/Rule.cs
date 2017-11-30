//**********************************************************************************************
// File:   Rule.cs
// Author: Andrés del Campo Novales
//
// This class holds a simple mapping rule (input -> output), or in other words, given an input,
// there is an output. It also keeps failed outputs when a mapping is not yet known, in which 
// the output itself would be empty.
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

using System.Collections.Generic;
using System.Diagnostics;

namespace CSLearnerLib
{
    [DebuggerDisplay("Input: {Input}; Output: {Output}")]
    public class Rule
    {
        public readonly string Input;
        public string Output;
        public readonly List<string> FailedOutputs;
        
        //**********************************************************************************************
        // Constructor
        //**********************************************************************************************
        public Rule(string input, string output)
        {
            Input = input;
            Output = output;
            FailedOutputs = new List<string>();
        }

        public Rule(string input, string output, List<string> failedOutputs)
        {
            Input = input;
            Output = output;
            FailedOutputs = failedOutputs;
        }

        public bool HasFailedOutput(string failedOutput)
        {
            return FailedOutputs.Contains(failedOutput);
        }
    }
}
