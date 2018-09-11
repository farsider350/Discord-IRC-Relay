/*  Discord IRC Relay - A Discord & IRC bot that relays messages 
 *
 *  Copyright (C) 2018 Michael Flaherty // michaelwflaherty.com // michaelwflaherty@me.com
 * 
 * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation, either version 3 of the License, or (at your option) 
 * any later version.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT 
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS 
 * FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along with 
 * this program. If not, see http://www.gnu.org/licenses/.
 */

using System.Threading.Tasks;
using Discord;

namespace IRCRelay
{
    public class Session
    {
        public enum TargetBot {
            Discord,
            IRC
        };

        private Discord discord;
        private IRC irc;
        private dynamic config;
        private bool alive;

        public bool IsAlive { get => alive; }
        public IRC Irc { get => irc; }
        internal Discord Discord { get => discord; }

        public Session(dynamic config)
        {
            this.config = config;
            alive = true;
        }

        public void Kill()
        {
            discord.Dispose();
            irc.Client.RfcQuit();

            Discord.Log(new LogMessage(LogSeverity.Critical, "KillSesh", "Session killed."));
            this.alive = false;
        }

        public async Task StartSession()
        {
            this.discord = new Discord(config, this);
            this.irc = new IRC(config, this);

            await Discord.Log(new LogMessage(LogSeverity.Critical, "StartSesh", "Session started."));
            await discord.SpawnBot();
            await irc.SpawnBot();
        }

        public async Task Recover(TargetBot bot) {
            switch (bot)
            {
                /* Recovering IRC can be annoying, so if IRC fails we'll just kill everything.
                 * This change's intention is to prevent discord reconnnects from nipping the IRC
                 * bot in and out of the server annoyingly on discord failures (which happens often)
                 */
                case TargetBot.IRC: // kill, and let main loop remake
                    this.Kill();
                    break;
                case TargetBot.Discord: // discord died, try new spawn.
                    discord.Dispose();
                    await discord.SpawnBot();
                    break;
            }
        }

        public void SendMessage(TargetBot dest, string message, string username = "")
        {
            switch (dest)
            {
                case TargetBot.Discord:
                    discord.SendMessageAllToTarget(config.DiscordGuildName, message, config.DiscordChannelName);
                    break;
                case TargetBot.IRC:
                    irc.SendMessage(username, message);
                    break;
            }
        }
    }
}
