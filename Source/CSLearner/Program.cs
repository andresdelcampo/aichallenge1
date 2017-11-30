//**********************************************************************************************
// File:   Program.cs
// Author: Andrés del Campo Novales
// General A.I. Challenge, 2017. All rights reserved within challenge rules and guidelines.
//
// Entry point. Handles the network connectivity and basic interface with teacher.
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
using NetMQ;
using NetMQ.Sockets;
using CSLearnerLib;

namespace CSLearner
{
    public class Program
    {
        private static readonly Display Display = new Display();
        private static readonly Learner Learner = new Learner();
        private const bool Interactive = false;

        static void Main()
        {
            using (var client = new PairSocket())
            {
                client.Connect("tcp://127.0.0.1:5556");
                client.SendFrame("hello");

                bool firstRun = true;

                // talk
                while (true)
                {
                    // receive reward
                    string reward = client.ReceiveFrameString();
                    if (reward == "-1")
                        reward = "-";
                    else if (reward == "1")
                        reward = "+";
                    else
                        reward = " ";

                    if (firstRun)
                    {
                        reward = string.Empty;
                        firstRun = false;
                    }
                    else
                    {
                        if (!Interactive)
                            Learner.RegisterReward(reward);
                    }

                    // receive teacher/env char
                    string teacherChar = client.ReceiveFrameString();

                    Display.ShowStep(reward, teacherChar);

                    // reply
                    string reply;
                    if (Interactive)
                    {
                        var key = Console.ReadKey();
                        reply = key.KeyChar.ToString();
                    }
                    else
                    {
                        reply = Learner.Answer(teacherChar);
                    }

                    Display.ShowReply(reply);
                    client.SendFrame(reply);
                }
            }
        }
    }
}
