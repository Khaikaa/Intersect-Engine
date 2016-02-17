﻿/*
    The MIT License (MIT)

    Copyright (c) 2015 JC Snider, Joe Bridges
  
    Website: http://ascensiongamedev.com
    Contact Email: admin@ascensiongamedev.com

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System;
using System.Windows.Forms;
using Intersect_Client.Classes;
using SFML.Window;

// ReSharper disable All

namespace Intersect_Client
{
    public static class GameMain
    {
        public static bool IsRunning = true;
        private static long _animTimer;
        public static void StartGame()
        {
            //Load Content/Options
            Database.CheckDirectories();
            Database.LoadOptions();

            if (Globals.IntroBG.Count == 0)
            {
                Globals.GameState = (int)Enums.GameStates.Menu;
            }

            //Load Graphics
            Graphics.InitGraphics();

            //Load Sounds
            Sounds.Init();
            Sounds.PlayMusic(Globals.MenuBGM, 3, 3, true);

            //Init Network
            Network.InitNetwork();

            //Start Game Loop
            while (IsRunning)
            {
                lock (Globals.GameLock)
                {
                    Network.CheckNetwork();
                    if (Globals.GameState == (int)Enums.GameStates.Menu)
                    {
                        ProcessMenu();
                    }
                    else
                    {
                        ProcessGame();
                    }
                    Graphics.DrawGame();
                    Sounds.Update();

                }
                Application.DoEvents();
            }

            //Destroy Game
            //TODO - Destroy Graphics and Networking peacefully
            //Network.Close();
            Gui.DestroyGwen();
            Graphics.RenderWindow.Close();
            Application.Exit();
        }

        private static void ProcessMenu()
        {
            if (!Globals.JoiningGame) return;
            if (Graphics.FadeAmt != 255f) return;
            //Check if maps are loaded and ready
            Globals.GameState = (int)Enums.GameStates.InGame;
            Gui.DestroyGwen();
            Gui.InitGwen();
            PacketSender.SendEnterGame();
        }

        private static void ProcessGame()
        {
            if (!Globals.GameLoaded)
            {
                if (Globals.LocalMaps[4] == -1) { return; }
                if (Globals.GameMaps == null) return;
                for (var i = 0; i < 9; i++)
                {
                    if (Globals.LocalMaps[i] <= -1) continue;
                    if (Globals.GameMaps.ContainsKey(Globals.LocalMaps[i]) && Globals.GameMaps[Globals.LocalMaps[i]] != null)
                    {
                        if (!Globals.GameMaps[Globals.LocalMaps[i]].ShouldLoad(i * 2 + 0)) continue;
                        if (Globals.GameMaps[Globals.LocalMaps[i]].MapLoaded == false)
                        {
                            return;
                        }
                        else if (Globals.GameMaps[Globals.LocalMaps[i]].MapRendered == false && Globals.RenderCaching == true)
                        {
                            Globals.GameMaps[Globals.LocalMaps[i]].PreRenderMap();
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                Globals.GameLoaded = true;
                Graphics.FadeStage = 1;
                return;
            }

            //Reset Single-Frame Variables
            Graphics.FogOffsetX = 0;
            Graphics.FogOffsetY = 0;


            //Update All Entities

            foreach (var en in Globals.Entities)
            {
                if (en.Value == null) continue;
                en.Value.Update();
            }
            foreach (var en in Globals.LocalEntities)
            {
                if (en.Value == null) continue;
                en.Value.Update();
            }

            for (int i = 0; i < Globals.EntitiesToDispose.Count; i++)
            {
                if (Globals.Entities[Globals.EntitiesToDispose[i]] == Globals.Me) continue;
                Globals.Entities.Remove(Globals.EntitiesToDispose[i]);
            }
            Globals.EntitiesToDispose.Clear();
            for (int i = 0; i < Globals.LocalEntitiesToDispose.Count; i++)
            {
                Globals.LocalEntities.Remove(Globals.LocalEntitiesToDispose[i]);
            }
            Globals.LocalEntitiesToDispose.Clear();

            //Update Maps
            bool handled = false;
            foreach (var map in Globals.GameMaps)
            {
                handled = false;
                if (map.Value == null) continue;
                for (var i = 0; i < 9; i++)
                {
                    if (Globals.LocalMaps[i] <= -1) continue;
                    if (map.Value.MyMapNum == Globals.LocalMaps[i])
                    {
                        map.Value.Update(true);
                        handled = true;
                    }
                }
                if (!handled)
                {
                    map.Value.Update(false);
                }
            }

            for (int i = 0; i < Globals.MapsToRemove.Count; i++)
            {
                Globals.GameMaps[Globals.MapsToRemove[i]].Dispose(false);
            }
            Globals.MapsToRemove.Clear();


            //Update Game Animations
            if (_animTimer < Environment.TickCount)
            {
                Globals.AnimFrame++;
                if (Globals.AnimFrame == 3) { Globals.AnimFrame = 0; }
                _animTimer = Environment.TickCount + 500;
            }
        }

        public static void JoinGame()
        {
            Globals.LoggedIn = true;
            Sounds.StopMusic(3f);
        }


    }


}