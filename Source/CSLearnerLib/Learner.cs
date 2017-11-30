//**********************************************************************************************
// File:   Feedback.cs
// Author: Andrés del Campo Novales
//
// This class is the main entry point for the learner. It delegates to the rule classes to 
// determine an output based on the input, and also handles the feedback and to
// switch context / mappings as needed.
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
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;

namespace CSLearnerLib
{
    public class Learner
    {
        private readonly Brain brain = new Brain();
        private readonly State state = new State();

        private string lastInput = string.Empty;
        private string lastCharOutput = string.Empty;
        private string lastOutput = string.Empty;
        private string lastReward = string.Empty;

        private readonly CircList<string> lastSuccessInputs = new CircList<string>(2000);
        private readonly CircList<string> lastSuccessOutputs = new CircList<string>(2000);

        private int consecutiveWins;
        private int consecutiveLoses;
        private int rewards;

        private Syntax Syntax => brain.Syntax;
        private MappingRules MappingRules => brain.MappingRules;
        private WordGenericRules WordGenericRules => brain.WordGenericRules;
        private CharGenericRules CharGenericRules => brain.CharGenericRules;
        private MathRules MathRules => brain.MathRules;
        private CharGenericSizeRules CharGenericSizeRules => brain.CharGenericSizeRules;
        private FeedbackWords FeedbackWords => Syntax.FeedbackWords;

        public Learner()
        {
            state.NewTask(Syntax);
        }

        public void RegisterReward(string reward, bool fromInput = false)
        {
            bool delimitersKnown = state.DelimitersKnown;

            bool stateOk = state.SetReward(reward, fromInput);

            if (delimitersKnown)
            {
                switch (reward)
                {
                    case "+":
                        rewards++;
                        lastReward = reward;

                        string input = lastInput;
                        Syntax.RemoveExtraSpaces(ref input);

                        // Was this the result of an empty input? -bail out, this was being out of sync, silent, etc.
                        if (input == string.Empty || 
                           (input  == " " && !Syntax.IsSingleCharMode()))
                            break;

                        MappingRules.Successful(input, lastOutput);
                        AbstractGenericRules(input, lastOutput);

                        consecutiveWins++;
                        consecutiveLoses = 0;
                        break;

                    case "-":
                        rewards--;
                        lastReward = reward;

                        // This will force transition to feedback -we don't want to do that in rewards in input mode
                        // because we are giving the reward AFTER the feedback instead of BEFORE.
                        if (!fromInput)
                            state.ClearOutput();

                        SwitchToNewTaskIfNeeded(stateOk);

                        input = lastInput;
                        Syntax.RemoveExtraSpaces(ref input);

                        // Was this the result of an empty input? -bail out, this was being out of sync, silent, etc.
                        if (input == string.Empty ||
                            (input == " " && !Syntax.IsSingleCharMode()))
                            break;

                        MappingRules.Failed(input, lastOutput);

                        consecutiveWins = 0;
                        consecutiveLoses++;
                        break;

                    case " ":
                        // Do nothing
                        break;

                    default:
                        throw new ArgumentException("Reward is invalid: " + reward);
                }
            }
        }

        private void AbstractGenericRules(string input, string output)
        {
            if (!Syntax.IsSingleCharMode())
            {
                string feedback = Syntax.RemoveFeedbackWords(output);

                for (int i = 0; i < lastSuccessInputs.Length; i++)
                {
                    bool mathRuleFound = MathRules.AbstractGenericRule(input, feedback, lastSuccessInputs[i], lastSuccessOutputs[i], Syntax.AnswerNowChar);
                    if (!mathRuleFound)
                    {
                        bool newRuleFound = CharGenericRules.AbstractGenericRule(input, feedback, lastSuccessInputs[i], lastSuccessOutputs[i], Syntax.AnswerNowChar);
                        if (newRuleFound)
                        {
                            CharGenericSizeRules.AbstractGenericRuleFromLastAdded(CharGenericRules, Syntax.AnswerNowChar);
                        }

                        WordGenericRules.AbstractGenericRule(input, feedback, lastSuccessInputs[i], lastSuccessOutputs[i], Syntax.AnswerNowChar);
                    }
                }

                AddToLastSuccessIfNotRepeated(input, feedback);
            }
        }

        private void AddToLastSuccessIfNotRepeated(string input, string output)
        {
            for (int i=lastSuccessInputs.Length-1; i>=0; i--)
            {
                if (lastSuccessInputs[i] == input && lastSuccessOutputs[i] == output)
                    return;
            }

            lastSuccessInputs.Add(input);
            lastSuccessOutputs.Add(output);
        }

        private void SwitchToNewTaskIfNeeded(bool stateOk)
        {
            // Have we changed task -when seeing a failure after successful 
            // attempts and actually failing a known or expected rule?
            Rule rule = MappingRules.Retrieve(lastInput);

            if (consecutiveLoses > 100)
            {
                SwitchToNewTask();
            }
            else if (MappingRules.IsFailedRule(rule, lastInput, lastOutput))
            {
                // We may be answering feedback including verbose feedback like "wrong! ".
                // We can readjust our wrong feedback words to include those.
                FeedbackWords.LearnWrongFeedbackWords();
                if (FeedbackWords.WrongFeedbackWords != string.Empty)
                    return;

                if (rewards < 4 &&
                    Syntax.FeedbackLength > 1 &&
                    Syntax.FeedbackRealChars < Syntax.FeedbackLength)
                {
                    // We may be including feedback characters that were bogus
                    Syntax.FeedbackRealChars++;
                    MappingRules.ResetRules();
                    rewards = 0;
                }
                else
                {
                    // Failing a confirmed rule is a bad thing anytime otherwise
                    SwitchToNewTask();
                }
            }
            else if (!stateOk)  // Keeping separate as reaction may vary later on
            {
                // Wrong multi character mode?
                SwitchToNewTask();
            }
            else if (consecutiveWins >= 10)
            {
                if (MappingRules.IsFailed(rule, lastInput, lastOutput))
                {
                    SwitchToNewTask();
                }
            }
            else
            {
                string output = MathRules.ApplyMatchingRule(lastInput);
                if (output != string.Empty &&
                    output == lastOutput)
                {
                    MathRules.FailedRule(lastInput);
                }
                else
                {
                    output = CharGenericRules.ApplyMatchingRule(lastInput);
                    if (output != string.Empty &&
                        output == lastOutput)
                    {
                        CharGenericRules.FailedRule(lastInput);

                        output = CharGenericSizeRules.ApplyMatchingRule(lastInput);
                        if (output != string.Empty &&
                            output == lastOutput)
                        {
                            CharGenericSizeRules.FailedRule(lastInput);
                        }

                        FeedbackWords.LearnAndSetWrongFeedbackWordsIfEmpty();
                    }
                    else
                    {
                        output = WordGenericRules.ApplyMatchingRule(lastInput);
                        if (output != string.Empty &&
                            output == lastOutput)
                        {
                            WordGenericRules.FailedRule(lastInput);

                            FeedbackWords.LearnAndSetWrongFeedbackWordsIfEmpty();
                        }
                    }
                }
            }
        }

        private void SwitchToNewTask()
        {
            brain.NewTask(rewards > 3);
            state.NewTask(Syntax);
            rewards = 0;
            consecutiveWins = 0;
            consecutiveLoses = 0;
        }

        //**********************************************************************************************
        // Answer
        //**********************************************************************************************
        public string Answer(string input)
        {
            string output = string.Empty;

            // Register the alphabet character
            if (!brain.Alphabet.Contains(input))
            {
                brain.Alphabet.Add(input);
            }

            state.ProcessState(input);

            if (state.DelimitersKnown)
            {
                // Obtain the recommended answer on single char rules
                if (Syntax.IsSingleCharMode())
                {
                    output = AnswerWithRules(input);
                    Debug.Assert(output.Length == 1);
                }
                else   // or from feedback mode
                {
                    output = FeedbackModeAnswer();
                }
            }
            else if (state.RewardInInputOnly)
            {
                if (state.IsFirstStep)
                    lastOutput = string.Empty;

                if (state.ShouldSendOutputNow)
                {
                    // Try with the current answer now char
                    if (lastOutput == string.Empty && state.HasAnswerNowChar())
                        output = Syntax.AnswerNowChar;
                    else
                    {
                        // Look for another one not tried in the alphabet
                        foreach (string entry in brain.Alphabet)
                        {
                            if (!lastOutput.Contains(entry) && entry != " ")
                            {
                                output = entry;
                                break;
                            }
                        }
                    }

                    lastOutput += output;
                }
                else
                {
                    output = " ";
                    lastOutput = string.Empty;
                }
            }
            else if (state.IsTeacherSilent())
            {
                if (state.DelimitersKnown)
                {
                    state.NewTask(Syntax);
                    rewards = 0;
                    output = " ";
                }
                else
                {
                    output = state.HasAnswerNowChar() ? Syntax.AnswerNowChar 
                                                      : FeedbackModeAnswer();
                }
            }
            else
            {
                // Detect expectations of new task set / instance (single char vs feedback mode, delimiters, etc)
                output = " ";
            }

            lastInput = state.FullInput;
            lastCharOutput = output;

            Debug.Assert(output.Length == 1);
            return output;
        }

        private string FeedbackModeAnswer()
        {
            string output = " ";

            if (state.IsAllReady)
            {
                Debug.Assert(state.FullFeedback.Length >= 1);

                output = " ";

                // Always add the feedback to learn wrong feedback words from it
                if (lastReward == "-")
                    FeedbackWords.Add(state.FullFeedback);

                // Parse last reward if it is coming in the input only
                if (state.NoRewardsExpectedMode())
                {
                    string reward = FeedbackWords.ParseFeedbackForRewards(state.FullFeedback);
                    RegisterReward(reward, true);
                }

                // Save the rule unless we don't have the input -we didn't know the structure before
                if (state.FullInput != string.Empty && lastReward != "+")
                {
                    string input = state.FullInput;
                    Syntax.RemoveExtraSpaces(ref input);

                    string valuableFeedback = state.FullFeedback;
                    Syntax.RemoveExtraSpaces(ref valuableFeedback);

                    if (valuableFeedback.Length > 1)
                        valuableFeedback = valuableFeedback.Trim();

                    valuableFeedback = Syntax.RemoveFeedbackWords(valuableFeedback);

                    // It's kind of a hypothesis
                    if (Syntax.FeedbackRealChars > 0)
                    {
                        int charsToKeep = Math.Min(Syntax.FeedbackRealChars, valuableFeedback.Length);

                        valuableFeedback = valuableFeedback.Substring(valuableFeedback.Length - charsToKeep, charsToKeep);
                        Debug.Assert(valuableFeedback.Length >= 1);
                    }

                    MappingRules.Successful(input, valuableFeedback);
                    AbstractGenericRules(input, valuableFeedback);
                }

                // New sentence, clear the reward
                lastReward = string.Empty;
            }
            else if (state.IsOutputLeft())
            {
                output = state.GetOutput();
            }
            else if (state.ShouldSendOutputNow || state.IsTeacherSilent())
            {
                string input = state.FullInput;
                Syntax.RemoveExtraSpaces(ref input);

                if (input == string.Empty)
                    input = " ";

                string fullOutput = AnswerWithRules(input);
                output = state.SetOutput(fullOutput);
            }

            return output;
        }

        private string AnswerWithRules(string input)
        {
            string output = string.Empty;

            // Do we already have an answer for this?
            Rule rule = MappingRules.Retrieve(input);
            if (rule != null)
            {
                output = Syntax.RemoveFeedbackWords(rule.Output);
            }

            // Try then generic rules
            if (string.IsNullOrEmpty(output))
                output = MathRules.ApplyMatchingRule(input);
            if (string.IsNullOrEmpty(output))
                output = CharGenericRules.ApplyMatchingRule(input);
            if (string.IsNullOrEmpty(output))
                output = WordGenericRules.ApplyMatchingRule(input);
            if (string.IsNullOrEmpty(output))
                output = CharGenericSizeRules.ApplyMatchingRule(input);
            if (string.IsNullOrEmpty(output))
                output = MathRules.ApplyCompoundRollingRule(input, Syntax.AnswerNowChar);
            if (string.IsNullOrEmpty(output))
                output = CharGenericRules.ApplyCompoundMatchingRule(input);
            if (string.IsNullOrEmpty(output))
                output = WordGenericRules.ApplyCompoundMatchingRule(input);
            if (string.IsNullOrEmpty(output))
                output = CharGenericRules.ApplyClosestRule(input);
            if (string.IsNullOrEmpty(output) && !Syntax.IsSingleCharMode())
                output = GetClosestSuccessfulMatch(input);
            if (string.IsNullOrEmpty(output) && !Syntax.IsSingleCharMode() && lastSuccessOutputs.GetLastAdded() != null)
                output = Syntax.RemoveFeedbackWords(lastSuccessOutputs.GetLastAdded());

            if (string.IsNullOrEmpty(output))
            {
                // Is it a uniform output task?
                if (MappingRules.IsUniform() &&
                    (rule == null || rule.HasFailedOutput(MappingRules.UniformValue) == false))
                {
                    output = MappingRules.UniformValue;
                }
                // Favor the input unless we know it failed before
                else if ((rule == null || rule.HasFailedOutput(input) == false) && !state.IsTeacherSilent())
                {
                    output = input;
                }
                else
                {
                    // Then look first for the most favorable outcomes from other rules
                    foreach (string entry in MappingRules.RetrieveOutputsSortedByFreq())
                    {
                        if (rule == null || rule.HasFailedOutput(entry) == false)
                        {
                            output = entry;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(output))
                    {
                        // Last, look for another one not tried in the alphabet
                        foreach (string entry in brain.Alphabet)
                        {
                            if ((rule == null || rule.HasFailedOutput(entry) == false) 
                                && !lastOutput.Contains(entry)
                                && entry != " ")
                            {
                                output = entry;
                                break;
                            }
                        }
                    }
                }
            }

            if (output == string.Empty)
                output = lastCharOutput;

            // We have just been outputting but not enough for the teacher to respond.
            // Used mostly for scenarios that will testing the alphabet until triggering an answer.
            if (lastCharOutput != " " && input == " " && state.LastReward() == " ")
                lastOutput += output;
            else
                lastOutput = output;

            return output;
        }

        private string GetClosestSuccessfulMatch(string input)
        {
            string bestOutput = string.Empty;
            float bestMatchScore = 0;
            string[] inputWords = Syntax.GetWords(input);

            for (int i = 0; i < lastSuccessInputs.Length; i++)
            {
                string[] successInputWords = Syntax.GetWords(lastSuccessInputs[i]);
                float score = 0;

                foreach (string word in successInputWords)
                {
                    if (inputWords.Contains(word))
                        score++;
                }

                if (score > bestMatchScore)
                {
                    bestOutput = Syntax.RemoveFeedbackWords(lastSuccessOutputs[i]);
                    bestMatchScore = score;
                }
            }

            return bestOutput;
        }
    }
}
