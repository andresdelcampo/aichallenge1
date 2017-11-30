//**********************************************************************************************
// File:   Display.cs
// Author: Andrés del Campo Novales
//
// This class handles the learner input, output and rewards visualization.
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

namespace CSLearner
{
    class Display
    {
        private string inputs = string.Empty;
        private string outputs = string.Empty;
        private string rewards = string.Empty;
        private int delay = 0;

        public void ShowStep(string reward, string input)
        {
            Debug.Assert(reward.Length <= 1);
            Debug.Assert(input.Length == 1);

            rewards += reward;
            inputs += input;

            LimitVisibleConversation();
            ShowConversation();
        }

        private void LimitVisibleConversation()
        {
            if (inputs.Length > 100)
            {
                rewards = rewards.Substring(1);
                inputs = inputs.Substring(1);
                outputs = outputs.Substring(1);
            }
        }

        private void ShowConversation()
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Input:  " + inputs + " ");
            Console.WriteLine("Output: " + outputs + " ");
            Console.WriteLine("Reward: " + rewards + " ");
            Console.SetCursorPosition(outputs.Length + 8, 1);
        }

        public void ShowReply(string output)
        {
            Debug.Assert(output.Length == 1);

            outputs += output;

            ShowConversation();

            ProcessKeyboard();

            System.Threading.Thread.Sleep(delay);
        }

        private void ProcessKeyboard()
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.KeyChar == '+' && delay > 0)
                    delay -= 5;
                else if (keyInfo.KeyChar == '-')
                    delay += 5;
                else
                {
                    do
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                    while (!Console.KeyAvailable);
                    Console.ReadKey(true);
                }
            }
        }
    }
}
