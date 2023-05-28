using SFML.Audio;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MMXOnline
{
    public class MusicWrapper
    {
        private Music music;
        public string musicPath;
        public float startPos;
        public float endPos;
        public string name;
        public bool debugLoop;
        private float _volume = 100;
        private bool destroyed;
        public bool loop;

        // Music source variables
        public Point? musicSourcePos;
        public Actor musicSourceActor;
        public bool moveWithActor;

        public float volume
        {
            get
            {
                return _volume;
            }
            set
            {
                _volume = value;
                updateVolume();
            }
        }

        public MusicWrapper()
        {

        }

        public MusicWrapper(string musicPath, double startPos, double endPos, bool loop = true)
        {
            this.musicPath = musicPath;
            this.loop = loop;
            music = new Music(musicPath);
            music.Loop = loop;
            name = Path.GetFileNameWithoutExtension(musicPath);
            this.startPos = (float)(startPos);
            this.endPos = (float)(endPos);
        }

        public MusicWrapper clone()
        {
            return new MusicWrapper(musicPath, startPos, endPos, loop);
        }

        public void play()
        {
            music?.Play();
        }

        public void stop()
        {
            music?.Stop();
        }

        public void update()
        {
            if (music == null) return;
            if (music.Loop && music.PlayingOffset.AsSeconds() > endPos)
            {
                music.PlayingOffset = SFML.System.Time.FromSeconds(startPos);
            }
            if (debugLoop)
            {
                Global.debugString1 = "music start: " + startPos.ToString();
                Global.debugString2 = "music end: " + endPos.ToString();
                if (Keyboard.IsKeyPressed(Keyboard.Key.Equal)) endPos += 0.001f;
                if (Keyboard.IsKeyPressed(Keyboard.Key.Hyphen)) endPos -= 0.001f;

                if (Keyboard.IsKeyPressed(Keyboard.Key.RBracket)) startPos += 0.001f;
                if (Keyboard.IsKeyPressed(Keyboard.Key.LBracket)) startPos -= 0.001f;
            }
        }

        public void setNearEnd()
        {
            if (music == null) return;
            music.PlayingOffset = SFML.System.Time.FromSeconds(endPos - 1);
            debugLoop = true;
        }

        public void updateVolume()
        {
            if (music == null) return;
            music.Volume = volume * Options.main.musicVolume;
        }

        public void updateMusicSource()
        {
            if (musicSourceActor != null && moveWithActor)
            {
                musicSourcePos = musicSourcePos.Value.add(musicSourceActor.deltaPos);
            }
        }

        public void updateMusicSourceVolume(Point listenerPos)
        {
            float dist = (listenerPos.distanceTo(musicSourcePos.Value) - 75) / (Global.screenW - 75);
            dist = Helpers.clamp01(dist);
            volume = (1 - MathF.Pow(dist, 2)) * 100;
        }

        public void destroy()
        {
            if (!destroyed)
            {
                destroyed = true;
            }
            else
            {
                return;
            }

            music?.Stop();                      
        }
    }
}
