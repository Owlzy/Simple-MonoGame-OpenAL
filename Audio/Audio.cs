using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using P3.AudioLib;
using System.Collections.Generic;

namespace P3
{
    public static class Audio
    {
        private static readonly List<Sound> playingInstances = new List<Sound>();

        public static float MasterVolume { get; private set; }

        public enum MuteType { All, Sound, Music, VO };

        private struct MuteConfig
        {
            public bool all;
            public bool sound;
            public bool music;
            public bool vo;

            public MuteConfig(bool all = false, bool sound = false, bool music = false, bool vo = false)
            {
                this.all = all;
                this.sound = sound;
                this.music = music;
                this.vo = vo;
            }
        }

        private static Sound music;
        private static Sound vo;
        private static MuteConfig mute = new MuteConfig();
        private static bool playingVO = false;

        public static void Update()
        {
            for (int i = 0; i < playingInstances.Count; i++)
            {
                var instance = playingInstances[i];

                if (instance.state == SoundState.Stopped)
                {
                    if (instance == vo)
                        playingVO = false;

                    playingInstances.Remove(instance);

                    if (!instance.IsDisposed)
                        instance.Dispose();

                    instance = null;
                }
                else if (instance.IsComplete)
                {
                    instance.Stop();
                }
            } 
        }

        /// <summary>
        ///    Plays a music track
        /// </summary>
        /// <returns>The song played <see cref="Song" />.</returns>
        /// 
        public static Song PlayMusic(string name, bool loop = true)
        {
            var music = Game.Instance.Assets.GetMusic(name);
            Microsoft.Xna.Framework.Media.MediaPlayer.IsRepeating = loop;
            Microsoft.Xna.Framework.Media.MediaPlayer.Play(music);
            return music;
        }

        /// <summary>
        ///    Stops current music track
        /// </summary>
        public static void StopMusic()
        {
            Microsoft.Xna.Framework.Media.MediaPlayer.Stop();
        }

        /// <summary>
        ///    Plays an audio clip
        /// </summary>
        /// <returns>The sound effect played <see cref="SoundEffect" />.</returns>
        public static Sound PlaySound(string name, bool loop = false)
        {
            var sound = Game.Instance.Assets.GetSound(name);
            sound.Looping = loop;
            sound.Play();
            playingInstances.Add(sound);
            return sound;
        } 

        /// <summary>
        ///    Plays an audio clip
        /// </summary>
        /// <returns>The sound effect played <see cref="Sound" />.</returns>
        public static Sound PlaySound(string[] names, bool loop = false)
        {
            var sound = Game.Instance.Assets.GetSound(Utils.SelectRandom(names));
            sound.Looping = loop;
            sound.Play();
            playingInstances.Add(sound);
            return sound;
        }

        /// <summary>
        ///    Plays one shot audio clip
        /// </summary>
        /// <returns>The sound effect played <see cref="Sound" />.</returns>
        public static Sound PlayOneShot(string name)
        {
            return PlaySound(name);
        }

        /// <summary>
        ///    Plays one shot audio clip
        /// </summary>
        /// <returns>The sound effect played <see cref="Sound" />.</returns>
        public static Sound PlayOneShot(string[] names)
        {
            return PlaySound(names);
        }

        /// <summary>
        ///    Plays a VO
        /// </summary>
        /// <returns>The sound effect played <see cref="SoundEffectInstance" />.</returns>
        public static Sound PlayVO(string name, float volume = 1f, bool force = false)
        {
            if ((playingVO && !force))
                return vo;

            StopVO();

            vo = Game.Instance.Assets.GetSound(name);
            // vo.Volume = volume;
            vo.Play();

            playingVO = true;
            playingInstances.Add(vo);

            return vo;
        }

        /// <summary>
        ///    Plays a VO
        /// </summary>
        /// <returns>The sound effect played <see cref="Sound" />.</returns>
        public static Sound PlayVO(string[] names, float volume = 1f, bool force = false)
        {
            if ((playingVO && !force))
                return vo;

            StopVO();

            vo = Game.Instance.Assets.GetSound(Utils.SelectRandom(names));
            // vo.Volume = volume;
            vo.Play();

            playingVO = true;
            playingInstances.Add(vo);

            return vo;
        }

        /// <summary>
        ///     Stops current VO from playing
        /// </summary>
        public static void StopVO()
        {
            if (vo != null)
            {
                vo.Stop();
                vo = null;
            }
        }

        /// <summary>
        ///     Mutes audio track. By default mutes all. 
        /// </summary>
        /// <param name="type">The track <see cref="MuteType" /> to be muted .</param>
        public static void Mute(MuteType type = MuteType.All)
        {
            switch (type)
            {
                case MuteType.All:
                    mute.all = true;
                    break;
                case MuteType.Sound:
                    mute.sound = true;
                    break;
                case MuteType.Music:
                    mute.music = true;
                    break;
                case MuteType.VO:
                    mute.vo = true;
                    break;
            }

            if (type == MuteType.All)
            {
                SoundEffect.MasterVolume = 0f;
                MasterVolume = 0f;
                Microsoft.Xna.Framework.Media.MediaPlayer.IsMuted = true;

                for (int i = 0; i < playingInstances.Count; i++)
                {
                    playingInstances[i].Volume = 0f;
                }

            }
            else if (type == MuteType.Sound)
            {
                SoundEffect.MasterVolume = 0f;
                MasterVolume = 0f;

                for (int i = 0; i < playingInstances.Count; i++)
                {
                    playingInstances[i].Volume = 0f;
                }

            }
            else if (type == MuteType.Music)
            {
                Microsoft.Xna.Framework.Media.MediaPlayer.IsMuted = true;
            }
        }

        /// <summary>
        ///     UnMutes audio track. By default mutes all. 
        /// </summary>
        /// <param name="type">The track <see cref="MuteType" /> to be unmuted .</param>
        public static void UnMute(MuteType type = MuteType.All)
        {
            switch (type)
            {
                case MuteType.All:
                    mute.all = false;
                    break;
                case MuteType.Sound:
                    mute.sound = false;
                    break;
                case MuteType.Music:
                    mute.music = false;
                    break;
                case MuteType.VO:
                    mute.vo = false;
                    break;
            }

            if (type == MuteType.All)
            {
                SoundEffect.MasterVolume = 1f;
                MasterVolume = 1f;
                Microsoft.Xna.Framework.Media.MediaPlayer.IsMuted = false;

                for (int i = 0; i < playingInstances.Count; i++)
                {
                    playingInstances[i].Volume = 1f;
                }

            }
            else if (type == MuteType.Sound)
            {
                SoundEffect.MasterVolume = 1f;
                MasterVolume = 1f;

                for (int i = 0; i < playingInstances.Count; i++)
                {
                    playingInstances[i].Volume = 1f;
                }

            }
            else if (type == MuteType.Music)
            {
                Microsoft.Xna.Framework.Media.MediaPlayer.IsMuted = false;
            }
        }

        /// <summary>
        ///     Checks if track is muted.  Defaults to all.
        /// </summary>
        /// <param name="type">The track <see cref="MuteType" /> to be muted .</param>
        /// <returns>The resulting <see cref="bool" />.</returns>
        public static bool IsMute(MuteType type = MuteType.All)
        {
            switch (type)
            {
                case MuteType.All: return mute.all;
                case MuteType.Sound: return mute.sound;
                case MuteType.Music: return mute.music;
                case MuteType.VO: return mute.vo;
            }
            return false;
        }
    }
}
