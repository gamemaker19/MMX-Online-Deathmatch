using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class ChatMenu : IMainMenu
    {
        public bool chatEnabled;
        private bool _typingChat;
        public bool typingChat { get { return _typingChat; } private set { _typingChat = value; } }
        public bool isTeamChat;
        public string currentTypedChat = "";
        public float chatBlinkTime = 0;
        public List<int> lastNChatFrames = new List<int>();
        public const int chatLines = 7;
        public int exitedChatFrames;
        public bool recentlyExited { get { return exitedChatFrames > 0; } }
        public List<ChatEntry> chatHistory = new List<ChatEntry>();
        public List<ChatEntry> chatFeed = new List<ChatEntry>();

        public ChatMenu()
        {
            if (Global.serverClient != null)
            {
                chatEnabled = true;
            }
            if (Global.debug)
            {
                chatEnabled = true;
            }
        }

        public bool isChatEnabled()
        {
            if (DevConsole.showConsole) return true;
            return chatEnabled && !Options.main.disableChat && !Global.isChatBanned;
        }

        public void update()
        {
            exitedChatFrames--;
            if (exitedChatFrames < 0) exitedChatFrames = 0;

            for (var i = this.chatFeed.Count - 1; i >= 0; i--)
            {
                var chatFeed = this.chatFeed[i];
                chatFeed.time += Global.spf;
                if (chatFeed.time > 20)
                {
                    this.chatFeed.Remove(chatFeed);
                }
            }

            if (isChatEnabled())
            {
                if (typingChat)
                {
                    chatBlinkTime += Global.spf;
                    if (chatBlinkTime >= 1f) chatBlinkTime = 0;
                    currentTypedChat = Helpers.getTypedString(currentTypedChat, 35);
                    if (Global.input.isPressed(Key.Enter) && !string.IsNullOrWhiteSpace(currentTypedChat))
                    {
                        currentTypedChat = Helpers.censor(currentTypedChat);
                        typingChat = false;
                        exitedChatFrames = 3;
                        addChatEntry();
                        lastNChatFrames.Add(Global.frameCount);
                        if (lastNChatFrames.Count > 5) lastNChatFrames.PopFirst();
                    }
                    if (Global.input.isPressedMenu(Control.MenuEnter) || (Global.input.isPressed(Key.Enter) && string.IsNullOrWhiteSpace(currentTypedChat)))
                    {
                        typingChat = false;
                        exitedChatFrames = 3;
                    }
                }
                else if (!Menu.inControlMenu)
                {
                    bool throttleChat = lastNChatFrames.Count == 5 && Global.frameCount - lastNChatFrames[0] < Global.normalizeFrames(600);
                    if (Global.debug)
                    {
                        throttleChat = false;
                    }

                    if (!throttleChat)
                    {
                        if (Global.input.isPressedMenu(Control.AllChat))
                        {
                            typingChat = true;
                            isTeamChat = false;
                            currentTypedChat = "";
                        }
                        else if (Global.input.isPressedMenu(Control.TeamChat) && Global.level?.gameMode?.isTeamMode == true)
                        {
                            typingChat = true;
                            isTeamChat = true;
                            currentTypedChat = "";
                        }
                    }
                }

                if (DevConsole.showConsole && !typingChat)
                {
                    currentTypedChat = "";
                    typingChat = true;
                    exitedChatFrames = 0;
                }
            }
        }

        public void openChat()
        {
            typingChat = true;
            currentTypedChat = "";
        }

        public void closeChat()
        {
            typingChat = false;
            currentTypedChat = "";
        }

        // Hook used by generic ChatMenu.
        public void addChatEntry()
        {
            var chatEntry = new ChatEntry(
                currentTypedChat,
                Global.level.mainPlayer.name,
                isTeamChat ? (int?)Global.level.mainPlayer.alliance : null,
                false,
                isSpectator: Global.level.mainPlayer.isSpectator);

            addChatEntry(chatEntry, true);
        }

        // Internal helper method to add a chat entry, useful for programmatic chat writes for "system" messages
        public void addChatEntry(ChatEntry chatEntry, bool sendRpc = false)
        {
            // If developer console is enabled, chat gets funneled to that as a command instead.
            if (DevConsole.showConsole && chatEntry.sender == Global.level.mainPlayer.name)
            {
                DevConsole.runCommand(chatEntry.message);
                return;
            }

            if (Global.level.gameMode.isTeamMode && chatEntry.alliance != null && chatEntry.alliance.Value != Global.level.mainPlayer.alliance) return;
            if (Global.level.players.Any(p => p.isMuted && p.name == chatEntry.sender)) return;
            if (!chatEntry.alwaysShow && (Options.main.disableChat || !chatEnabled)) return;

            chatFeed.Add(chatEntry);
            chatHistory.Add(chatEntry);
            if (chatFeed.Count > ChatMenu.chatLines) chatFeed.PopFirst();
            if (sendRpc)
            {
                chatEntry.sendRpc();
            }
        }

        public void render()
        {
            int topLeftX = 5;
            int chatLineHeight = 5;
            int topLeftY = 216 - (chatLineHeight * chatLines);
            uint chatFontSize = 12;
            for (var i = 0; i < chatFeed.Count; i++)
            {
                var chat = chatFeed[i];
                Color outlineColor = Color.Black;
                if (chat.alliance != null)
                {
                    outlineColor = (chat.alliance == GameMode.redAlliance ? Helpers.DarkRed : Helpers.DarkBlue);
                }
                Helpers.drawTextStd(TCat.Chat, chat.getDisplayMessage(), topLeftX, topLeftY + (i * chatLineHeight), Alignment.Left, fontSize: chatFontSize, outlineColor: outlineColor);
            }

            if (typingChat)
            {
                string chatDisplay = (isTeamChat ? "Team:" : "All:") + currentTypedChat;
                Color outlineColor = (isTeamChat ? Helpers.getAllianceColor() : Color.Black);

                if (DevConsole.showConsole)
                {
                    chatDisplay = "Console:" + currentTypedChat;
                    outlineColor = Color.Black;
                }

                int typedChatY = topLeftY + (chatLines * chatLineHeight) + 2;
                float width = Helpers.measureTextStd(TCat.Chat, chatDisplay, fontSize: chatFontSize).x;

                DrawWrappers.DrawRect(topLeftX - 2, typedChatY - 2, topLeftX + 125, typedChatY + chatLineHeight, true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, isWorldPos: false);
                Helpers.drawTextStd(TCat.Chat, chatDisplay, topLeftX, typedChatY, alignment: Alignment.Left, fontSize: chatFontSize, outlineColor: outlineColor);

                if (chatBlinkTime >= 0.5f)
                {
                    Helpers.drawTextStd(TCat.Chat, "<", topLeftX + width, typedChatY, alignment: Alignment.Left, fontSize: chatFontSize, outlineColor: outlineColor);
                }
            }
        }
    }
}
