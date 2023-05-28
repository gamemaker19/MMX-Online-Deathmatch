using SFML.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public class LoopingSound
    {
        public bool stopped = false;
        int times = 0;
        Actor actor;
        Sound startClip;
        Sound loopClip;
        Sound stopClip;
        bool startClipStarted = false;
        bool startClipDone = false;
        float prevLoopPos;
        public bool destroyed;

        public LoopingSound(string startClipStr, string loopClipStr, Actor actor)
        {
            startClip = new Sound(Global.soundBuffers[startClipStr].soundBuffer);
            loopClip = new Sound(Global.soundBuffers[loopClipStr].soundBuffer);
            loopClip.Loop = true;
            this.actor = actor;
            Global.level.loopingSounds.Add(this);
        }

        public LoopingSound(string startClipStr, string stopClipStr, string loopClipStr, Actor actor) : this(startClipStr, loopClipStr, actor)
        {
            this.stopClip = new Sound(Global.soundBuffers[stopClipStr].soundBuffer);
        }

        public bool isPlaying()
        {
            return !stopped && startClip.Status == SoundStatus.Playing || loopClip.Status == SoundStatus.Playing;
        }

        public void play()
        {
            if (stopped) return;
            stopClip?.Stop();

            startClip.Volume = actor.getSoundVolume();
            loopClip.Volume = actor.getSoundVolume();

            if (!startClipDone)
            {
                if (!startClipStarted)
                {
                    startClipStarted = true;
                    startClip.Play();
                }
                else if (startClip.Status == SoundStatus.Stopped)
                {
                    startClipDone = true;
                    loopClip.Play();
                }
            }
            else
            {
                var currentLoopPos = loopClip.PlayingOffset.AsMilliseconds();
                if (currentLoopPos < prevLoopPos)
                {
                    times++;
                    if (times > 6 && stopClip == null)
                    {
                        loopClip.Stop();
                    }
                }
                prevLoopPos = currentLoopPos;
            }
        }

        public void stop(bool resetTimes = true)
        {
            if (stopped) return;
            if (resetTimes)
            {
                times = 0;
            }
            startClip?.Stop();
            loopClip?.Stop();
            stopped = true;
        }

        public void stopRev(float progress)
        {
            stop();
            reset();

            if (stopClip != null && stopClip.Status != SoundStatus.Playing)
            {
                stopClip.Volume = actor.getSoundVolume();
                stopClip.PlayingOffset = stopClip.SoundBuffer.Duration * (1 - progress);
                stopClip?.Play();
            }

            startClip.PlayingOffset = startClip.SoundBuffer.Duration * progress;
        }

        public void reset()
        {
            stopped = false;
            times = 0;
            startClip?.Stop();
            loopClip?.Stop();
            startClipStarted = false;
            startClipDone = false;
        }

        public void destroy()
        {
            stop();
            destroyed = true;
            startClip?.Dispose();
            loopClip?.Dispose();
            stopClip?.Dispose();
            startClip = null;
            loopClip = null;
            stopClip = null;
            Global.level.loopingSounds.Remove(this);
        }
    }
}
