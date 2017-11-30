//**********************************************************************************************
// File:   GenericRule.cs
// Author: Andrés del Campo Novales
//
// This class is a data structure that holds information on how to expand a variable size 
// generic rule. See CharGenericRules.cs for more info on them.
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

using System.Diagnostics;

namespace CSLearnerLib
{
    [DebuggerDisplay("Input: {Input}; Output: {Output}; GrowToLeft {GrowToLeft}; GrowToRight: {GrowToRight}")]
    public class GenericRule
    {
        public readonly string Input;
        public readonly string Output;
        public readonly string Id1;
        public readonly string Id2;
        public readonly string GrowToLeft;
        public readonly string GrowToRight;
        public readonly string Ending;

        //**********************************************************************************************
        // Constructor
        //**********************************************************************************************
        public GenericRule(string input, string output, string id1, string id2, string growToLeft, string growToRight, string ending)
        {
            Input = input;
            Output = output;
            Id1 = id1;
            Id2 = id2;
            GrowToLeft = growToLeft;
            GrowToRight = growToRight;
            Ending = ending;
        }
    }
}
