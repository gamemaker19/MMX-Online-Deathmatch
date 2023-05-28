using SFML.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum SoundPool
    {
        Regular,
        Voice,
        CharOverride
    }

    // Blueprint for a sound should live here.
    public class SoundBufferWrapper
    {
        public SoundBuffer soundBuffer;
        public SoundPool soundPool;
        public int volume0To100 = 100;
        public bool isVoice;
        public int frameDelay;
        public string soundKey;
        public bool waitForExistingVoiceClipToEnd;
        public bool stopAllOtherVoiceClips;
        public int? charNum;

        public float volume
        {
            get
            {
                return volume0To100 / 100f;
            }
        }

        public SoundBufferWrapper(string fileName, string filePath, SoundPool soundPool)
        {
            soundKey = fileName;
            soundBuffer = new SoundBuffer(filePath);
            this.soundPool = soundPool;

            var pieces = filePath.Replace('\\', '/').Split('/');
            for (int i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                var subpieces = piece.Split('.');
                foreach (var subpiece in subpieces)
                {
                    if (Helpers.parseFileDotParam(subpiece, 'v', out int v))
                    {
                        volume0To100 = v;
                    }
                    else if (Helpers.parseFileDotParam(subpiece, 'f', out int f))
                    {
                        frameDelay = f;
                    }
                    else if (Helpers.parseFileDotParam(subpiece, 'w', out int w))
                    {
                        waitForExistingVoiceClipToEnd = true;
                    }
                    else if (Helpers.parseFileDotParam(subpiece, 's', out int s))
                    {
                        stopAllOtherVoiceClips = true;
                    }
                    else if (subpiece == "mmx")
                    {
                        charNum = 0;
                    }
                    else if (subpiece == "zero")
                    {
                        charNum = 1;
                    }
                    else if (subpiece == "vile")
                    {
                        charNum = 2;
                    }
                    else if (subpiece == "axl")
                    {
                        charNum = 3;
                    }
                    else if (subpiece == "sigma")
                    {
                        charNum = 4;
                    }
                }
            }
        }

        public Sound createSound()
        {
            Sound sound;
            if (soundPool == SoundPool.Regular)
            {
                sound = new Sound(Global.soundBuffers[soundKey].soundBuffer);
            }
            else if (soundPool == SoundPool.Voice)
            {
                sound = new Sound(Global.voiceBuffers[soundKey].soundBuffer);
            }
            else
            {
                sound = new Sound(Global.charSoundBuffers[soundKey].soundBuffer);
            }
            return sound;
        }
    }

    // Instance of a sound should live here.
    public class SoundWrapper
    {
        public SoundBufferWrapper soundBuffer;
        public Sound sound;
        public Actor actor;
        public bool deleted;

        public SoundWrapper(SoundBufferWrapper soundBuffer, Actor actor)
        {
            this.soundBuffer = soundBuffer;
            this.actor = actor;
            sound = soundBuffer.createSound();
            Global.sounds.Add(this);
            update();
        }

        public void play()
        {
            if (deleted) return;
            if (soundBuffer.soundPool == SoundPool.Voice)
            {
                var existingVoice = Global.sounds.FirstOrDefault(s => s != this && s.actor == actor && s.soundBuffer.soundPool == SoundPool.Voice);
                if (existingVoice != null)
                {
                    if (soundBuffer.waitForExistingVoiceClipToEnd)
                    {
                        return;
                    }
                    else // if (soundBuffer.stopAllOtherVoiceClips)
                    {
                        existingVoice.sound.Stop();
                    }
                }
            }
            sound.Play();
        }

        public void update()
        {
            if (deleted) return;
            if (actor != null && Global.level == null)
            {
                sound.Stop();
            }
            if (actor != null)
            {
                sound.Volume = actor.getSoundVolume() * soundBuffer.volume;
                // Point pos = actor.getSoundPos();
                // sound.Position = new SFML.System.Vector3f(pos.x, 0, pos.y);
            }
            else
            {
                sound.Volume = 100 * Options.main.soundVolume;
            }
        }
    }
}
