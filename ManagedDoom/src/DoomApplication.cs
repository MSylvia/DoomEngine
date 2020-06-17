﻿using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using SFML.Graphics;
using SFML.Window;
using ManagedDoom.SoftwareRendering;

namespace ManagedDoom
{
    public sealed class DoomApplication : IDisposable
    {
        private RenderWindow window;
        private CommonResource resource;
        private SfmlRenderer renderer;
        private SfmlAudio audio;

        private DoomMenu menu;

        private ApplicationState state;

        private OpeningSequence opening;

        private TicCmd[] cmds;
        private GameOptions options;
        private DoomGame game;

        private List<DoomEvent> events;

        //private Demo demo;

        public DoomApplication()
        {
            try
            {
                var style = Styles.Close | Styles.Titlebar;
                window = new RenderWindow(new VideoMode(640, 400), "Managed Doom", style);
                window.Clear(new Color(128, 128, 128));
                window.Display();

                resource = new CommonResource("DOOM2.WAD");
                renderer = new SfmlRenderer(window, resource, true);
                audio = new SfmlAudio(resource.Wad);

                menu = new DoomMenu(this);

                state = ApplicationState.Opening;

                opening = new OpeningSequence();

                cmds = new TicCmd[Player.MaxPlayerCount];
                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    cmds[i] = new TicCmd();
                }

                options = new GameOptions();
                options.Skill = GameSkill.Hard;
                options.GameMode = resource.Wad.GameMode;
                options.MissionPack = resource.Wad.MissionPack;
                options.Episode = 1;
                options.Map = 1;
                options.Players[0].InGame = true;

                //demo = new Demo("test.lmp");
                //options = demo.Options;

                game = new DoomGame(resource, options);
                game.Audio = audio;

                events = new List<DoomEvent>();

                window.Closed += (sender, e) => window.Close();

                window.KeyPressed += KeyPressed;
                window.KeyReleased += KeyReleased;

                window.SetFramerateLimit(35);
            }
            catch (Exception e)
            {
                ExceptionDispatchInfo.Capture(e).Throw();
            }
        }

        public void Run()
        {
            while (window.IsOpen)
            {
                window.DispatchEvents();
                DoEvents();
                Update();
            }
        }

        public void NewGame()
        {
            game.DoAction(GameAction.NewGame);
            state = ApplicationState.Game;
        }

        private void DoEvents()
        {
            foreach (var e in events)
            {
                if (menu.DoEvent(e))
                {
                    continue;
                }

                if (state == ApplicationState.Game)
                {
                    if (game.DoEvent(e))
                    {
                        continue;
                    }
                }
            }
            events.Clear();
        }

        private void Update()
        {
            menu.Update();

            if (state == ApplicationState.Opening)
            {
                opening.Update();
            }
            else if (state == ApplicationState.Game)
            {
                UserInput.BuildTicCmd(cmds[0]);
                //demo.ReadCmd(cmds);
                game.Update(cmds);
            }

            renderer.Render(this);
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (events.Count < 64)
            {
                var de = new DoomEvent();
                de.Type = EventType.KeyDown;
                de.Key = (DoomKeys)e.Code;
                events.Add(de);
            }
        }

        private void KeyReleased(object sender, KeyEventArgs e)
        {
            if (events.Count < 64)
            {
                var de = new DoomEvent();
                de.Type = EventType.KeyUp;
                de.Key = (DoomKeys)e.Code;
                events.Add(de);
            }
        }

        public void Dispose()
        {
            if (audio != null)
            {
                audio.Dispose();
                audio = null;
            }

            if (renderer != null)
            {
                renderer.Dispose();
                renderer = null;
            }

            if (resource != null)
            {
                resource.Dispose();
                resource = null;
            }

            if (window != null)
            {
                window.Dispose();
                window = null;
            }
        }

        public ApplicationState State => state;
        public OpeningSequence Opening => opening;
        public DoomGame Game => game;
        public DoomMenu Menu => menu;
        public GameMode GameMode => resource.Wad.GameMode;
    }
}
