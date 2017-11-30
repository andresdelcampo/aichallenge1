//**********************************************************************************************
// File:   State.cs
// Author: Andrés del Campo Novales
//
// This class handles the state (reading the input, outputting the answer, or parsing the 
// feedback). It can also make syntax decisions to determine when to switch between the states.
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

namespace CSLearnerLib
{
    public class State
    {
        private Syntax syntax;

        private const int SilenceLength = 50;

        private string inputs = string.Empty;
        private string rewards = string.Empty;

        private int numRewards;
        private int answersWithoutReward;
        private bool rewardExpected;
        private bool lastOutputWasAnswerNow;

        private InputState state = InputState.ReceivingInput;

        public bool IsAllReady { get; private set; }        // All input and feedback has been read
        public bool IsFirstStep { get; private set; }       // First state execution when trying to locate delimiters, etc.
        public bool DelimitersKnown { get; private set; }   // Whether the syntax delimiters are known
        public bool RewardInInputOnly { get; private set; }   // Whether the rewards are no longer coming in the reward channel and should be guessed from the input
        public bool ShouldSendOutputNow { get; private set; }   // Whether the output should be sent now -vs. wait for input or feedback

        // Complete input, output and feedback on the current request.
        public string FullInput { get; private set; } = string.Empty;
        public string FullOutput { get; private set; } = string.Empty;
        public string FullFeedback { get; private set; } = string.Empty;

        private enum InputState
        {
            ReceivingInput,
            InLongOutput,
            ReceivingFeedback
        }

        public void NewTask(Syntax newSyntax)
        {
            state = InputState.ReceivingInput;

            IsAllReady = false;
            FullInput = string.Empty;
            FullOutput = string.Empty;
            FullFeedback = string.Empty;
            inputs = string.Empty;
            rewards = string.Empty;
            numRewards = 0;
            RewardInInputOnly = false;
            DelimitersKnown = false;

            syntax = newSyntax;
        }

        #region Long output handling
        public void ClearOutput()
        {
            FullOutput = string.Empty;
            SetNextStateAfterInput();
        }

        public string SetOutput(string output)
        {
            Debug.Assert(output.Length > 0);

            FullOutput = output;

            if (output.Length > 1)
                state = InputState.InLongOutput;
            if (output.Length == 1)
                rewardExpected = true;

            return GetOutput();
        }

        public string GetOutput()
        {
            string outputChar = FullOutput[0].ToString();
            FullOutput = FullOutput.Substring(1, FullOutput.Length - 1);
            lastOutputWasAnswerNow = (HasAnswerNowChar() && outputChar[0].ToString() == syntax.AnswerNowChar);
            return outputChar[0].ToString();
        }
        #endregion

        public bool SetReward(string reward, bool fromInput = false)
        {
            bool ok = true;

            // Track only real rewards -input rewards don't count
            if (!fromInput)
            {
                rewards += reward;
            }

            if (reward != " ")
            { 
                rewardExpected = false;

                // Track only real rewards -input rewards don't count
                if (!fromInput)
                {
                    answersWithoutReward = 0;
                    RewardInInputOnly = false;
                }

                if (DelimitersKnown)
                { 
                    // Are we still reading input and got feedback? 
                    if (state == InputState.ReceivingInput 
                        && HasFeedback()
                        && !NoRewardsExpectedMode())
                    {
                        if (reward == "-")
                        {
                            // We probably missed the feedback spot
                            // Mode needs to be changed!
                            ok = false;
                        }
                    }
                }
                else
                {
                    numRewards++;

                    // Single char detected
                    if (numRewards == 2 && inputs.Length == 2)
                    {
                        NewTask(syntax);
                        DelimitersKnown = true;
                    }

                    // Fourth reward found? We can work with that
                    if (numRewards == 4)
                    {
                        syntax.AnalyzeSeparators(inputs, rewards);
                        DelimitersKnown = true;
                        SetNextStateAfterInput();

                        // We are falling behind one character in the feedback, take it.
                        if (fromInput && state == InputState.ReceivingFeedback)
                            FullFeedback = inputs[inputs.Length - 1].ToString();
                    }
                }
            }

            return ok;
        }

        public void ProcessState(string input)
        {
            CleanStoredInputAndRewardsIfBig();
            inputs += input;
            ShouldSendOutputNow = false;

            if (DelimitersKnown)
            {
                // Question solved, back to new question
                if (IsAllReady)
                {
                    IsAllReady = false;
                    FullInput = string.Empty;
                    FullOutput = string.Empty;
                    FullFeedback = string.Empty;
                }

                switch (state)
                {
                    case InputState.ReceivingInput:

                        if (FullInput == string.Empty)  
                            answersWithoutReward++;

                        FullInput += input;

                        if (HasAnswerNowChar())
                        {
                            if (input == syntax.AnswerNowChar)
                            {
                                ShouldSendOutputNow = true;
                                if (HasLongOutput())
                                {
                                    state = InputState.InLongOutput;
                                }
                                else
                                {
                                    SetNextStateAfterInput();
                                }
                            }
                        }
                        else if (IsInputReady())
                        {
                            ShouldSendOutputNow = true;
                            SetNextStateAfterInput();
                        }
                        break;

                    case InputState.InLongOutput:
                        if (input != " ")
                        {
                            if (lastOutputWasAnswerNow)
                            {
                                FullOutput = string.Empty;
                                FullFeedback = input;
                                SetNextStateAfterInput();
                            }
                            else
                            {
                                syntax = new Syntax();
                                NewTask(syntax);
                                inputs = input;
                                syntax.WrongFeedbackWords = string.Empty;
                            }
                        }
                        else if (FullOutput.Length <= 1)
                        {
                            rewardExpected = true;
                            SetNextStateAfterInput();
                        }
                        break;

                    case InputState.ReceivingFeedback:
                        if (!IsOutputLeft())
                        {
                            if (rewardExpected && HasAnswerNowChar() && !lastOutputWasAnswerNow)
                            {
                                // Did we expect a reward and did not receive it?
                                // We were not done yet
                                if (input == " ")
                                {
                                    FullOutput = syntax.AnswerNowChar;
                                    lastOutputWasAnswerNow = true;
                                }
                            }
                            else
                            {
                                // We are receiving feedback
                                if (!HasRequestSeparator() || !IsNextRequestChar(input))
                                {
                                    FullFeedback += input;
                                }

                                if (HasRequestSeparator())
                                {
                                    if (IsNextRequestChar(input))
                                    {
                                        state = InputState.ReceivingInput;
                                        IsAllReady = true;
                                    }
                                }
                                else if (FullFeedback.Length == 1)
                                {
                                    state = InputState.ReceivingInput;
                                    IsAllReady = true;
                                }
                            }
                        }

                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
            else  // Delimiters not known
            {
                IsFirstStep = false;

                // No rewards mode, we need to handle the delimiter identifiers by state instead of by rewards.
                if (!RewardInInputOnly && NoRewardsExpectedMode())
                {
                    RewardInInputOnly = true;
                    state = InputState.InLongOutput;
                    ShouldSendOutputNow = true;
                    IsFirstStep = true;
                }

                if (RewardInInputOnly)
                {
                    switch(state)
                    {
                        case InputState.InLongOutput:
                            ShouldSendOutputNow = true;

                            if (input != " ")
                            {
                                // We started receiving feedback while being in output, emulate a negative reward.
                                state = InputState.ReceivingInput;

                                Debug.Assert(inputs.Length == rewards.Length || inputs.Length - 1 == rewards.Length);
                                if (rewards.Length == inputs.Length)
                                    rewards = rewards.Remove(rewards.Length - 2, 2) + "- ";
                                else
                                    rewards = rewards.Remove(rewards.Length - 1, 1) + '-';

                                SetReward("-", true);
                                ShouldSendOutputNow = false;
                            }
                            break;

                        case InputState.ReceivingInput:
                            if (IsTeacherSilent())
                            {
                                // No more feedback + input, start output
                                state = InputState.InLongOutput;
                                ShouldSendOutputNow = true;
                            }
                            break;

                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        private void CleanStoredInputAndRewardsIfBig()
        {
            if (inputs.Length >= 10000)
                inputs = inputs.Remove(0, 9000);

            if (rewards.Length >= 10000)
                rewards = rewards.Remove(0, 9000);
        }

        private bool IsNextRequestChar(string input)
        {
            if (HasRequestSeparator() && input == syntax.NextRequestChar)
            {
                if (syntax.WrongFeedbackWords == string.Empty)
                    return true;

                if (syntax.WrongFeedbackWords.Contains(FullFeedback))
                    return false;

                return true;
            }

            return false;
        }

        public string LastReward()
        {
            return rewards.Length > 0 ? 
                rewards[rewards.Length - 1].ToString() 
                : " ";
        }

        public bool IsTeacherSilent()
        {
            if (inputs.Length < SilenceLength)
                return false;

            return inputs.Substring(inputs.Length - SilenceLength, SilenceLength).Trim() == string.Empty &&
                   rewards.Substring(rewards.Length - (SilenceLength - 1), SilenceLength - 1).Trim() == string.Empty;
        }

        private void SetNextStateAfterInput()
        {
            if (HasFeedback())
            {
                state = InputState.ReceivingFeedback;
            }
            else
            {
                IsAllReady = true;
            }
        }

        public bool IsOutputLeft()
        {
            return FullOutput.Length > 0;
        }

        private bool IsInputReady()
        {
            return syntax.InputLength == FullInput.Length;
        }

        private bool HasLongOutput()
        {
            return syntax.FeedbackLength > 1;
        }

        private bool HasFeedback()
        {
            return syntax.FeedbackLength > 0;
        }

        public bool NoRewardsExpectedMode()
        {
            return answersWithoutReward > 2;
        }

        public bool HasAnswerNowChar()
        {
            return syntax.AnswerNowChar != string.Empty;
        }

        private bool HasRequestSeparator()
        {
            return !string.IsNullOrEmpty(syntax.NextRequestChar);
        }
    }
}
