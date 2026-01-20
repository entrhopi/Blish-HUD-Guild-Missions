using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Blish_HUD.GameService;
using static System.Net.Mime.MediaTypeNames;
using Image = Blish_HUD.Controls.Image;

namespace entrhopi.Guild_Missions
{

    [Export(typeof(Module))]
    public class Guild_Missions : Module
    {
        private static class Layout
        {
            public const int TopMargin = 10;
            public const int RightMargin = 5;
            public const int BottomMargin = 10;
            public const int LeftMargin = 9;
            public const int ButtonHeight = 30;
            public const int PanelSize = 56;
            public const int MaxResultCount = 7;
        }

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        internal static Module ModuleInstance;

        #region Constants        
        private Panel trekListPanel, savedTrekListPanel, contentPanel, listPanel;
        public List<Panel> resultPanels = new List<Panel>();
        Dictionary<int, int> savedGuildTreks = new Dictionary<int, int>();
        #endregion

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public Guild_Missions([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings)
        {

        }

        private Texture2D _guildMissionIcon;
        private Texture2D _guildTrekIcon;
        private Texture2D _guildBountyIcon;
        private Texture2D _guildRaceIcon;
        private Texture2D _guildPuzzleIcon;
        private Texture2D _guildChallengeIcon;

        private Texture2D _lockedIcon;
        private Texture2D _wipIcon;

        private Texture2D _waypointIcon;
        private Texture2D _rightArrowIcon;

        private Texture2D _closeTexture;

        internal string GuildMissionsTabName = Strings.Common.gmTabName;

        private WindowTab _moduleTab;
        private TextBox searchTextBox;

        private String ShortUserLocale = "en";

        Dictionary<int, Texture2D> _guildRaceMap = new Dictionary<int, Texture2D>();

        protected override void Initialize()
        {
            _guildMissionIcon = ContentsManager.GetTexture("528697.png");
            _guildTrekIcon = ContentsManager.GetTexture("1228320.png");
            _guildBountyIcon = ContentsManager.GetTexture("1228316.png");
            _guildRaceIcon = ContentsManager.GetTexture("1228319.png");
            _guildPuzzleIcon = ContentsManager.GetTexture("1228318.png");
            _guildChallengeIcon = ContentsManager.GetTexture("1228317.png");

            _lockedIcon = ContentsManager.GetTexture("1827421.png");
            _wipIcon = ContentsManager.GetTexture("2221493.png");

            _waypointIcon = ContentsManager.GetTexture("157354.png");
            _rightArrowIcon = ContentsManager.GetTexture("784266.png");

            _closeTexture = ContentsManager.GetTexture("close_icon.png");

            switch (GameService.Overlay.UserLocale.Value.ToString())
            {
                case "German":
                    ShortUserLocale = "de"; // German => de
                    break;
                case "English":
                    ShortUserLocale = "en"; // English => en
                    break;
                case "Spanish":
                    ShortUserLocale = "es"; // Spanish => es
                    break;
                case "French":
                    ShortUserLocale = "fr"; // French => fr
                    break;
            }
        }

        protected override void OnModuleLoaded(EventArgs e)
        {

            _moduleTab = Overlay.BlishHudWindow.AddTab(GuildMissionsTabName, _guildMissionIcon, GuildMissionsView(Overlay.BlishHudWindow));

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private Panel GuildMissionsView(WindowBase wndw)
        {
            var parentPanel = new Panel()
            {
                CanScroll = false,
                Size = wndw.ContentRegion.Size
            };

            var missionTypePanel = new Panel()
            {
                ShowBorder = true,
                Title = Strings.Common.gmTypeSelect,
                Size = new Point(265, parentPanel.Height - Layout.BottomMargin),
                Location = new Point(Layout.LeftMargin, Layout.TopMargin),
                Parent = parentPanel,
            };

            var guildTrekPanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(missionTypePanel.Width, Layout.PanelSize),
                Location = new Point(0, 0),
                Parent = missionTypePanel,
            };
            guildTrekPanel.Click += delegate { GuildTrekContent(); };
            new Image(_guildTrekIcon)
            {
                Size = new Point(Layout.PanelSize, Layout.PanelSize),
                Location = new Point(0, 0),
                Parent = guildTrekPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeTrek,
                Font = Content.DefaultFont16,
                Location = new Point(Layout.LeftMargin + Layout.PanelSize, Layout.PanelSize / 2 - 10),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = guildTrekPanel
            };

            var guildBountyPanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(missionTypePanel.Width, Layout.PanelSize),
                Location = new Point(0, Layout.PanelSize),
                Parent = missionTypePanel,
            };
            guildBountyPanel.Click += delegate { GuildBountyContent(); };
            new Image(_guildBountyIcon)
            {
                Size = new Point(Layout.PanelSize, Layout.PanelSize),
                Location = new Point(0, 0),
                Parent = guildBountyPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeBounty,
                Font = Content.DefaultFont16,
                Location = new Point(Layout.LeftMargin + Layout.PanelSize, Layout.PanelSize / 2 - 10),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = guildBountyPanel
            };

            var guildRacePanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(missionTypePanel.Width, Layout.PanelSize),
                Location = new Point(0, Layout.PanelSize * 2),
                Parent = missionTypePanel,
            };
            guildRacePanel.Click += delegate { GuildRaceContent(); };
            new Image(_guildRaceIcon)
            {
                Size = new Point(Layout.PanelSize, Layout.PanelSize),
                Location = new Point(0, 0),
                Parent = guildRacePanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeRace,
                Font = Content.DefaultFont16,
                Location = new Point(Layout.LeftMargin + Layout.PanelSize, Layout.PanelSize / 2 - 10),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = guildRacePanel
            };

            var guildChallengePanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(missionTypePanel.Width, Layout.PanelSize),
                Location = new Point(0, Layout.PanelSize * 3),
                Parent = missionTypePanel,
            };
            guildChallengePanel.Click += delegate { GuildChallengeContent(); };
            new Image(_guildChallengeIcon)
            {
                Size = new Point(Layout.PanelSize, Layout.PanelSize),
                Location = new Point(0, 0),
                Parent = guildChallengePanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeChallenge,
                Font = Content.DefaultFont16,
                Location = new Point(Layout.LeftMargin + Layout.PanelSize, Layout.PanelSize / 2 - 10),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = guildChallengePanel
            };

            var guildPuzzlePanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(missionTypePanel.Width, Layout.PanelSize),
                Location = new Point(0, Layout.PanelSize * 4),
                Parent = missionTypePanel,
            };
            guildPuzzlePanel.Click += delegate { GuildPuzzleContent(); };
            new Image(_guildPuzzleIcon)
            {
                Size = new Point(Layout.PanelSize, Layout.PanelSize),
                Location = new Point(0, 0),
                Parent = guildPuzzlePanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypePuzzle,
                Font = Content.DefaultFont16,
                Location = new Point(Layout.LeftMargin + Layout.PanelSize, Layout.PanelSize / 2 - 10),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = guildPuzzlePanel
            };


            contentPanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(parentPanel.Width - missionTypePanel.Right - Layout.RightMargin, parentPanel.Height - Layout.BottomMargin),
                Location = new Point(missionTypePanel.Right + Layout.LeftMargin, Layout.TopMargin),
                Parent = parentPanel,
            };

            return parentPanel;
        }

        private void GuildTrekContent()
        {
            contentPanel.ClearChildren();

            new Image(_guildTrekIcon)
            {
                Size = new Point(72, 72),
                Location = new Point(Layout.LeftMargin, 0),
                Parent = contentPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeTrek,
                Font = Content.DefaultFont32,
                Location = new Point(82, 18),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = contentPanel
            };
            new Label()
            {
                Text = "Searchbox mouse click clears current search,\nEnter key adds topmost item to saved list and clears search",
                Font = Content.DefaultFont16,
                Location = new Point(220, 18),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = contentPanel
            };

            searchTextBox = new TextBox()
            {
                PlaceholderText = Strings.Common.gmSearchPlaceholder,
                Size = new Point(358, 43),
                Font = GameService.Content.DefaultFont16,
                Location = new Point(Layout.LeftMargin, 72 + Layout.TopMargin),
                Parent = contentPanel,
            };
            searchTextBox.Click += delegate { ClearSearch(); };
            searchTextBox.TextChanged += SearchboxOnTextChanged;
            searchTextBox.EnterPressed += SearchboxEnterPressed;

            trekListPanel = new Panel()
            {
                ShowBorder = true,
                Title = Strings.Common.gmPanelSearchResults,
                Size = new Point(364, contentPanel.Height - searchTextBox.Bottom - Layout.BottomMargin),
                Location = new Point(Layout.LeftMargin - 3, searchTextBox.Bottom + Layout.TopMargin),
                Parent = contentPanel,
            };

            savedTrekListPanel = new Panel()
            {
                CanScroll = true,
                ShowBorder = true,
                Title = Strings.Common.gmPanelSavedTreks,
                Size = new Point(364, contentPanel.Height - searchTextBox.Bottom - Layout.ButtonHeight - Layout.BottomMargin),
                Location = new Point(trekListPanel.Right + Layout.LeftMargin, searchTextBox.Bottom + Layout.TopMargin),
                Parent = contentPanel,
            };

            var clearAllButton = new StandardButton()
            {
                Text = Strings.Common.gmButtonClearAll,
                Size = new Point(110, Layout.ButtonHeight),
                Location = new Point(trekListPanel.Right + 20, searchTextBox.Top - 1),
                Parent = contentPanel,
            };
            clearAllButton.Click += delegate { ClearWPList(); };

            var exportButton = new StandardButton()
            {
                Text = Strings.Common.gmButtonExport,
                Size = new Point(110, Layout.ButtonHeight),
                Location = new Point(trekListPanel.Right + 130 + Layout.LeftMargin, searchTextBox.Top - 1),
                Parent = contentPanel,
            };
            exportButton.Click += delegate { _ = ExportWPList(); };

            var importButton = new StandardButton()
            {
                Text = Strings.Common.gmButtonImport,
                Size = new Point(110, Layout.ButtonHeight),
                Location = new Point(trekListPanel.Right + 250 + Layout.LeftMargin, searchTextBox.Top - 1),
                Parent = contentPanel,
            };
            importButton.Click += delegate { ImportWPList(); };

            var sendToChatButton = new StandardButton()
            {
                Text = Strings.Common.gmButtonSendToChat,
                Size = new Point(364, Layout.ButtonHeight),
                Location = new Point(savedTrekListPanel.Left, savedTrekListPanel.Bottom),
                Parent = contentPanel,
            };
            sendToChatButton.Click += delegate { _ = SendAllSavedToClipboard(); };

            UpdateSavedWPList();
        }

        private void GuildRaceContent()
        {
            SimpleGuildMissionPanel(
                _guildRaceIcon,
                Strings.Common.gmTypeRace,
                @"XML\races.xml",
                "race");
        }

        private void GuildBountyContent()
        {
            SimpleGuildMissionPanel(
                _guildBountyIcon,
                Strings.Common.gmTypeBounty,
                @"XML\bounties.xml",
                "bounty",
                enableScrolling: true);
        }

        private void GuildChallengeContent()
        {
            SimpleGuildMissionPanel(
                _guildChallengeIcon,
                Strings.Common.gmTypeChallenge,
                @"XML\challenges.xml",
                "challenge");
        }

        private void GuildPuzzleContent()
        {
            SimpleGuildMissionPanel(
                _guildPuzzleIcon,
                Strings.Common.gmTypePuzzle,
                @"XML\puzzles.xml",
                "puzzle");
        }

        private void SimpleGuildMissionPanel(AsyncTexture2D icon, string title, string xmlPath, string xmlElement, bool enableScrolling = false)
        {
            // Clear existing content
            contentPanel.ClearChildren();

            // Add icon
            new Image(icon)
            {
                Size = new Point(72, 72),
                Location = new Point(Layout.LeftMargin, 0),
                Parent = contentPanel
            };

            // Add title label
            new Label()
            {
                Text = title,
                Font = Content.DefaultFont32,
                Location = new Point(82, 18),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = contentPanel
            };

            // Create list panel
            listPanel = new Panel()
            {
                ShowBorder = true,
                Title = Strings.Common.gmPanelList,
                Size = new Point(contentPanel.Width - Layout.LeftMargin, contentPanel.Height - Layout.BottomMargin),
                Location = new Point(Layout.LeftMargin - 3, 72 + Layout.TopMargin),
                Parent = contentPanel,
                CanScroll = enableScrolling
            };

            // Load XML, add rows to list panel
            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(xmlPath));
            int index = 0;
            foreach (var element in doc.Root.Elements(xmlElement))
            {
                AddInfoPanel(element, listPanel, index++);
            }
        }

        private void SearchboxOnTextChanged(object _, EventArgs __)
        {
            // Load user input
            string text = searchTextBox.Text;

            // Dispose of current search result
            trekListPanel.ClearChildren();
            if (string.IsNullOrWhiteSpace(text)) return;

            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\treks.xml"));
            int count = 0;
            foreach (var trek in doc.Root.Elements("trek"))
            {
                if (trek.Element("name_" + ShortUserLocale).Value.ToLower().StartsWith(text.ToLower()))
                {
                    AddTrekPanel(trek, trekListPanel, count++, true, false);

                    if (count >= Layout.MaxResultCount) break;
                }
            }
        }

        private void SearchboxEnterPressed(object _, EventArgs __)
        {
            // Load user input
            string text = searchTextBox.Text;

            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\treks.xml"));
            int count = 0;
            foreach (var trek in doc.Root.Elements("trek"))
            {
                if (trek.Element("name_" + ShortUserLocale).Value.ToLower().StartsWith(text.ToLower()))
                {
                    ToggleWaypoint((int)trek.Element("id"), (int)trek.Element("map_id"), true);
                    break;
                }
            }

            ClearSearch();

            searchTextBox.Focused = true;

            return;
        }

        private void ToggleWaypoint(int trek, int map, bool add)
        {
            if (add)
                savedGuildTreks.Add(trek, map);
            else
                savedGuildTreks.Remove(trek);
            UpdateSavedWPList();
        }

        private void ClearWPList()
        {
            savedGuildTreks.Clear();
            savedTrekListPanel.ClearChildren();
        }

        private void ClearSearch()
        {
            searchTextBox.Text = "";
        }

        private async Task CopyToClipboard(string text)
        {
            try
            {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(text);
                ScreenNotification.ShowNotification(Strings.Common.gmNotificationClipboardSaved, duration: 2);
            }
            catch (Exception)
            {
                ScreenNotification.ShowNotification(Strings.Common.gmNotificationClipboardError, ScreenNotification.NotificationType.Red, duration: 2);
            }
        }

        private async Task SendAllSavedToClipboard()
        {
            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\treks.xml"));

            var export = "";
            foreach (KeyValuePair<int, int> wp in savedGuildTreks.OrderBy(key => key.Value))
            {
                // Grab trek data from xml
                var trek = doc.Descendants("trek")
                    .Where(x => x.Element("id").Value == wp.Key.ToString())
                    .FirstOrDefault();

                if (trek == null) continue;

                export += trek.Element("name_" + ShortUserLocale).Value + " " + trek.Element("chat_link").Value + " ";
            }

            await CopyToClipboard(export);
        }

        private async Task ExportWPList()
        {
            var sb = new StringBuilder();
            sb.Append("BlishGM");

            foreach (KeyValuePair<int, int> wp in savedGuildTreks.OrderBy(key => key.Value))
            {
                sb.Append($";{wp.Key}");
            }

            await CopyToClipboard(sb.ToString());
        }

        private void ImportWPList()
        {
            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\treks.xml"));
            ClipboardUtil.WindowsClipboardService.GetTextAsync()
                .ContinueWith((import) => {
                    if (!import.IsFaulted)
                    {
                        if (!string.IsNullOrEmpty(import.Result))
                        {
                            int i = 0;
                            foreach (string wp in import.Result.Split(';'))
                            {
                                if (i == 0 && String.Equals(wp, "BlishGM"))
                                {
                                    i++;
                                    continue;
                                }
                                else if (i == 0 && !String.Equals(wp, "BlishGM")) return;

                                Logger.Warn(import.Exception, i + ":" + wp);

                                // Grab trek data from xml
                                var trek = doc.Descendants("trek")
                                    .Where(x => x.Element("id").Value == wp)
                                    .FirstOrDefault();

                                if (trek == null) continue;

                                ToggleWaypoint((int)trek.Element("id"), (int)trek.Element("map_id"), true);
                                i++;
                            }

                            ScreenNotification.ShowNotification(String.Format(Strings.Common.gmNotificationClipboardRead, (i - 1)), duration: 2);
                        }
                    }
                    else
                    {
                        Logger.Warn(import.Exception, "Failed to read clipboard text from system clipboard!");
                    }
                });
        }

        private void UpdateSavedWPList()
        {
            savedTrekListPanel.ClearChildren();

            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\treks.xml"));

            // Sort saved treks by map id
            int i = 0;
            foreach (KeyValuePair<int, int> wp in savedGuildTreks.OrderBy(key => key.Value))
            {
                // Grab trek data from xml
                var trek = doc.Descendants("trek")
                    .Where(x => x.Element("id").Value == wp.Key.ToString())
                    .FirstOrDefault();

                if (trek == null) continue;

                AddTrekPanel(trek, savedTrekListPanel, i, false, true);

                i++;
            }
        }

        private void AddTrekPanel(XElement trek, Panel parent, int position, bool add = false, bool remove = false)
        {

            Panel trekPanel = new Panel()
            {
                ShowBorder = false,
                //Title = trek.Element("name").Value + " (" + trek.Element("map_name").Value + ")",
                Size = new Point(parent.Width, 70),
                Location = new Point(Layout.LeftMargin, 5 + position * 70),
                Parent = parent
            };
            Image trekWPImage = new Image(_waypointIcon)
            {
                Size = new Point(50, 50),
                Location = new Point(0, 4),
                Parent = trekPanel
            };
            trekWPImage.Click += delegate
            {
                _ = CopyToClipboard(trek.Element("name_" + ShortUserLocale).Value + " " + trek.Element("chat_link").Value);
            };
            new Label()
            {
                Text = trek.Element("name_" + ShortUserLocale).Value + " (" + trek.Element("map_name_" + ShortUserLocale).Value + ")",
                Font = Content.DefaultFont16,
                Location = new Point(Layout.LeftMargin + 50, 3),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = trekPanel
            };
            new Label()
            {
                Text = trek.Element("waypoint_name_" + ShortUserLocale).Value,
                Font = Content.DefaultFont14,
                Location = new Point(Layout.LeftMargin + 50, 32),
                TextColor = Color.Silver,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = trekPanel
            };

            if (add)
            {
                Image addImage = new Image(_rightArrowIcon)
                {
                    Size = new Point(70, 70),
                    Location = new Point(parent.Width - 70, -10),
                    Parent = trekPanel
                };
                addImage.Click += delegate { ToggleWaypoint((int)trek.Element("id"), (int)trek.Element("map_id"), true); };
            }

            if (remove)
            {
                Image removeImage = new Image(_closeTexture)
                {
                    Size = new Point(20, 20),
                    Location = new Point(parent.Width - 40, 4),
                    Parent = trekPanel
                };
                removeImage.Click += delegate { ToggleWaypoint((int)trek.Element("id"), 0, false); };
            }
        }

        private void AddInfoPanel(XElement element, Panel parent, int position)
        {
            Panel elementPanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(parent.Width, 70),
                Location = new Point(Layout.LeftMargin, 5 + position * 70),
                Parent = parent
            };
            Image elementWPImage = new Image(_waypointIcon)
            {
                Size = new Point(50, 50),
                Location = new Point(0, 4),
                Parent = elementPanel
            };
            elementWPImage.Click += delegate
            {
                _ = CopyToClipboard(element.Element("name_" + ShortUserLocale).Value + " " + element.Element("chat_link").Value);
            };
            new Label()
            {
                Text = element.Element("name_" + ShortUserLocale).Value + " (" + element.Element("map_name_" + ShortUserLocale).Value + ")",
                Font = Content.DefaultFont16,
                Location = new Point(Layout.LeftMargin + 50, 3),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = elementPanel
            };
            new Label()
            {
                Text = element.Element("waypoint_name_" + ShortUserLocale).Value,
                Font = Content.DefaultFont14,
                Location = new Point(Layout.LeftMargin + 50, 32),
                TextColor = Color.Silver,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = elementPanel
            };
            var openWikiBttn = new StandardButton()
            {
                Text = Strings.Common.gmButtonWiki,
                Size = new Point(110, Layout.ButtonHeight),
                Location = new Point(parent.Width - 110 - 50, 10),
                Parent = elementPanel
            };
            openWikiBttn.Click += delegate { Process.Start(element.Element("wiki_link_" + ShortUserLocale).Value); };
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload here
            Overlay.BlishHudWindow.RemoveTab(_moduleTab);
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }


}
