//**********************************************************************************************
// File:   Brain.cs
// Author: Andrés del Campo Novales
//
// This "brain" class contains the learned syntax, rule patterns and rule inference mechanisms.
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
    public class Brain
    {
        public Syntax Syntax = new Syntax();
        public readonly List<string> Alphabet = new List<string>();

        public MappingRules MappingRules = new MappingRules();
        public readonly WordGenericRules WordGenericRules = new WordGenericRules();
        public readonly CharGenericRules CharGenericRules = new CharGenericRules();
        public readonly MathRules MathRules = new MathRules();
        public readonly CharGenericSizeRules CharGenericSizeRules = new CharGenericSizeRules();

        public Brain()
        {
            NewTask(false);
        }

        public void NewTask(bool copyDelimiters)
        {
            string answerNowChar = Syntax.AnswerNowChar;
            string nextRequestChar = Syntax.NextRequestChar;

            MappingRules = new MappingRules();

            Syntax = copyDelimiters ? new Syntax(answerNowChar, nextRequestChar) : new Syntax();
        }
    }
}
