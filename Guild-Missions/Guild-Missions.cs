using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Pathing.Behaviors;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Blish_HUD.GameService;

namespace entrhopi.Guild_Missions
{

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Guild_Missions : Blish_HUD.Modules.Module
    {

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        internal static Module ModuleInstance;
        #region Constants

        private const int TOP_MARGIN = 10;
        private const int RIGHT_MARGIN = 5;
        private const int BOTTOM_MARGIN = 10;
        private const int LEFT_MARGIN = 9;

        private const int BUTTON_HEIGHT = 30;

        private const int MAX_RESULT_COUNT = 7;
        
        private Panel trekListPanel, savedTrekListPanel, contentPanel, listPanel, infoPanel;
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

        private int panelsize = 56;

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

            _guildRaceMap.Add(1, ContentsManager.GetTexture("racemaps/bear_lope.jpg"));
            _guildRaceMap.Add(2, ContentsManager.GetTexture("racemaps/chicken_run.jpg"));
            _guildRaceMap.Add(3, ContentsManager.GetTexture("racemaps/crab_scuttle.jpg"));
            _guildRaceMap.Add(4, ContentsManager.GetTexture("racemaps/devourer_burrow.jpg"));
            _guildRaceMap.Add(5, ContentsManager.GetTexture("racemaps/ghost_wolf_run.jpg"));
            _guildRaceMap.Add(6, ContentsManager.GetTexture("racemaps/quaggan_paddle.jpg"));
            _guildRaceMap.Add(7, ContentsManager.GetTexture("racemaps/spider_scurry.jpg"));

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

        protected override async Task LoadAsync()
        {

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
                Size = new Point(265, parentPanel.Height - BOTTOM_MARGIN),
                Location = new Point(LEFT_MARGIN, TOP_MARGIN),
                Parent = parentPanel,
            };

            var guildTrekPanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(missionTypePanel.Width, panelsize),
                Location = new Point(0, 0),
                Parent = missionTypePanel,
            };
            guildTrekPanel.Click += delegate { guildTrekContent(); };
            new Image(_guildTrekIcon)
            {
                Size = new Point(panelsize, panelsize),
                Location = new Point(0, 0),
                Parent = guildTrekPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeTrek,
                Font = Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN + panelsize, panelsize / 2 - 10),
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
                Size = new Point(missionTypePanel.Width, panelsize),
                Location = new Point(0, panelsize),
                Parent = missionTypePanel,
            };
            guildBountyPanel.Click += delegate { guildBountyContent(); };
            new Image(_guildBountyIcon)
            {
                Size = new Point(panelsize, panelsize),
                Location = new Point(0, 0),
                Parent = guildBountyPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeBounty,
                Font = Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN + panelsize, panelsize / 2 - 10),
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
                Size = new Point(missionTypePanel.Width, panelsize),
                Location = new Point(0, panelsize * 2),
                Parent = missionTypePanel,
            };
            guildRacePanel.Click += delegate { guildRaceContent(); };
            new Image(_guildRaceIcon)
            {
                Size = new Point(panelsize, panelsize),
                Location = new Point(0, 0),
                Parent = guildRacePanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeRace,
                Font = Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN + panelsize, panelsize / 2 - 10),
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
                Size = new Point(missionTypePanel.Width, panelsize),
                Location = new Point(0, panelsize * 3),
                Parent = missionTypePanel,
            };
            guildChallengePanel.Click += delegate { guildChallengeContent(); };
            new Image(_guildChallengeIcon)
            {
                Size = new Point(panelsize, panelsize),
                Location = new Point(0, 0),
                Parent = guildChallengePanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeChallenge,
                Font = Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN + panelsize, panelsize / 2 - 10),
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
                Size = new Point(missionTypePanel.Width, panelsize),
                Location = new Point(0, panelsize * 4),
                Parent = missionTypePanel,
            };
            guildPuzzlePanel.Click += delegate { guildPuzzleContent(); };
            new Image(_guildPuzzleIcon)
            {
                Size = new Point(panelsize, panelsize),
                Location = new Point(0, 0),
                Parent = guildPuzzlePanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypePuzzle,
                Font = Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN + panelsize, panelsize / 2 - 10),
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
                Size = new Point(parentPanel.Width - missionTypePanel.Right - RIGHT_MARGIN, parentPanel.Height - BOTTOM_MARGIN),
                Location = new Point(missionTypePanel.Right + LEFT_MARGIN, TOP_MARGIN),
                Parent = parentPanel,
            };

            return parentPanel;
        }

        private void guildTrekContent()
        {
            contentPanel.ClearChildren();

            new Image(_guildTrekIcon)
            {
                Size = new Point(72, 72),
                Location = new Point(LEFT_MARGIN, 0),
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

            searchTextBox = new TextBox()
            {
                PlaceholderText = Strings.Common.gmSearchPlaceholder,
                Size = new Point(358, 43),
                Font = GameService.Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN, 72 + TOP_MARGIN),
                Parent = contentPanel,
            };
            searchTextBox.Click += delegate { ClearSearch(); };
            searchTextBox.TextChanged += SearchboxOnTextChanged;

            trekListPanel = new Panel()
            {
                ShowBorder = true,
                Title = Strings.Common.gmPanelSearchResults,
                Size = new Point(364, contentPanel.Height - searchTextBox.Bottom - BOTTOM_MARGIN),
                Location = new Point(LEFT_MARGIN - 3, searchTextBox.Bottom + TOP_MARGIN),
                Parent = contentPanel,
            };

            savedTrekListPanel = new Panel()
            {
                CanScroll = true,
                ShowBorder = true,
                Title = Strings.Common.gmPanelSavedTreks,
                Size = new Point(364, contentPanel.Height - searchTextBox.Bottom - BUTTON_HEIGHT - BOTTOM_MARGIN),
                Location = new Point(trekListPanel.Right + LEFT_MARGIN, searchTextBox.Bottom + TOP_MARGIN),
                Parent = contentPanel,
            };

            var clearAllButton = new StandardButton()
            {
                Text = Strings.Common.gmButtonClearAll,
                Size = new Point(110, BUTTON_HEIGHT),
                Location = new Point(trekListPanel.Right + 20, searchTextBox.Top - 1),
                Parent = contentPanel,
            };
            clearAllButton.Click += delegate { ClearWPList(); };

            var exportButton = new StandardButton()
            {
                Text = Strings.Common.gmButtonExport,
                Size = new Point(110, BUTTON_HEIGHT),
                Location = new Point(trekListPanel.Right + 130 + LEFT_MARGIN, searchTextBox.Top - 1),
                Parent = contentPanel,
            };
            exportButton.Click += delegate { ExportWPList(); };

            var importButton = new StandardButton()
            {
                Text = Strings.Common.gmButtonImport,
                Size = new Point(110, BUTTON_HEIGHT),
                Location = new Point(trekListPanel.Right + 250 + LEFT_MARGIN, searchTextBox.Top - 1),
                Parent = contentPanel,
            };
            importButton.Click += delegate { ImportWPList(); };

            var sendToChatButton = new StandardButton()
            {
                Text = Strings.Common.gmButtonSendToChat,
                Size = new Point(364, BUTTON_HEIGHT),
                Location = new Point(savedTrekListPanel.Left, savedTrekListPanel.Bottom),
                Parent = contentPanel,
            };
            sendToChatButton.Click += delegate { sendToChat(); };

            UpdateSavedWPList();
        }

        private void guildRaceContent()
        {
            contentPanel.ClearChildren();

            new Image(_guildRaceIcon)
            {
                Size = new Point(72, 72),
                Location = new Point(LEFT_MARGIN, 0),
                Parent = contentPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeRace,
                Font = Content.DefaultFont32,
                Location = new Point(82, 18),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = contentPanel
            };

            listPanel = new Panel()
            {
                ShowBorder = true,
                Title = Strings.Common.gmPanelList,
                Size = new Point(contentPanel.Width - LEFT_MARGIN, contentPanel.Height - BOTTOM_MARGIN),
                Location = new Point(LEFT_MARGIN - 3, 72 + TOP_MARGIN),
                Parent = contentPanel,
            };

            // Dispose of current search result
            listPanel.ClearChildren();

            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\races.xml"));

            int i = 0;
            foreach (var race in doc.Root.Elements("race"))
            {
                ViewInfoPanelWiki(race, listPanel, i);
                i++;
            }
        }

        private void guildBountyContent()
        {
            contentPanel.ClearChildren();

            new Image(_guildBountyIcon)
            {
                Size = new Point(72, 72),
                Location = new Point(LEFT_MARGIN, 0),
                Parent = contentPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeBounty,
                Font = Content.DefaultFont32,
                Location = new Point(82, 18),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = contentPanel
            };

            listPanel = new Panel()
            {
                ShowBorder = true,
                Title = Strings.Common.gmPanelList,
                Size = new Point(contentPanel.Width - LEFT_MARGIN, contentPanel.Height - BOTTOM_MARGIN),
                Location = new Point(LEFT_MARGIN - 3, 72 + TOP_MARGIN),
                Parent = contentPanel,
            };

            // Dispose of current search result
            listPanel.ClearChildren();

            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\bounties.xml"));

            int i = 0;
            foreach (var bounty in doc.Root.Elements("bounty"))
            {
                ViewInfoPanelWiki(bounty, listPanel, i);
                i++;
            }

            listPanel.CanScroll = true;
        }

        private void guildChallengeContent()
        {
            contentPanel.ClearChildren();

            new Image(_guildChallengeIcon)
            {
                Size = new Point(72, 72),
                Location = new Point(LEFT_MARGIN, 0),
                Parent = contentPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypeChallenge,
                Font = Content.DefaultFont32,
                Location = new Point(82, 18),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = contentPanel
            };

            listPanel = new Panel()
            {
                ShowBorder = true,
                Title = Strings.Common.gmPanelList,
                Size = new Point(contentPanel.Width - LEFT_MARGIN, contentPanel.Height - BOTTOM_MARGIN),
                Location = new Point(LEFT_MARGIN - 3, 72 + TOP_MARGIN),
                Parent = contentPanel,
            };

            // Dispose of current search result
            listPanel.ClearChildren();

            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\challenges.xml"));

            int i = 0;
            foreach (var challenge in doc.Root.Elements("challenge"))
            {
                ViewInfoPanelWiki(challenge, listPanel, i);
                i++;
            }
        }

        private void guildPuzzleContent()
        {
            contentPanel.ClearChildren();

            new Image(_guildPuzzleIcon)
            {
                Size = new Point(72, 72),
                Location = new Point(LEFT_MARGIN, 0),
                Parent = contentPanel
            };
            new Label()
            {
                Text = Strings.Common.gmTypePuzzle,
                Font = Content.DefaultFont32,
                Location = new Point(82, 18),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = contentPanel
            };

            listPanel = new Panel()
            {
                ShowBorder = true,
                Title = Strings.Common.gmPanelList,
                Size = new Point(contentPanel.Width - LEFT_MARGIN, contentPanel.Height - BOTTOM_MARGIN),
                Location = new Point(LEFT_MARGIN - 3, 72 + TOP_MARGIN),
                Parent = contentPanel,
            };

            // Dispose of current search result
            listPanel.ClearChildren();

            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\puzzles.xml"));

            int i = 0;
            foreach (var puzzle in doc.Root.Elements("puzzle"))
            {
                ViewInfoPanelWiki(puzzle, listPanel, i);
                i++;
            }
        }

        private void SearchboxOnTextChanged(object sender, EventArgs e)
        {
            int i = 0;

            // Load user input
            string searchText = searchTextBox.Text;

            // Dispose of current search result
            trekListPanel.ClearChildren();

            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\treks.xml"));

            foreach(var trek in doc.Root.Elements("trek"))
            {
                if (trek.Element("name_" + ShortUserLocale).Value.ToLower().StartsWith(searchText.ToLower()))
                //if (trek.Element("name").Value.ToLower().Contains(searchText.ToLower()))
                {
                    AddTrekPanel(trek, trekListPanel, i, true, false);

                    i++;
                    if (i >= MAX_RESULT_COUNT) break;
                }
            }
        }

        private void AddWPToList(int trekID, int mapID)
        {
            if (savedGuildTreks.ContainsKey(trekID) == false)
            {
                savedGuildTreks.Add(trekID, mapID);
                UpdateSavedWPList();
            }

        }

        private void RemoveWPFromList(int trek)
        {
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

        private void sendToChat()
        {
            XDocument doc = XDocument.Load(ContentsManager.GetFileStream(@"XML\treks.xml"));

            int i = 0;
            var export = "";
            foreach (KeyValuePair<int, int> wp in savedGuildTreks.OrderBy(key => key.Value))
            {
                // Grab trek data from xml
                var trek = doc.Descendants("trek")
                    .Where(x => x.Element("id").Value == wp.Key.ToString())
                    .FirstOrDefault();

                if (trek == null) continue;

                export += trek.Element("name_" + ShortUserLocale).Value + " " + trek.Element("chat_link").Value + " ";

                i++;
            }

            ClipboardUtil.WindowsClipboardService.SetTextAsync(export).ContinueWith((clipboardResult) =>
            {
                if (clipboardResult.IsFaulted)
                {
                    ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
                }
                else
                {
                    ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
                }
            });
        }

        private void ExportWPList()
        {
            int i = 0;
            var export = "BlishGM";
            foreach (KeyValuePair<int, int> wp in savedGuildTreks.OrderBy(key => key.Value))
            {
                i++;
                export = export + ';' + wp.Key.ToString();
            }

            ClipboardUtil.WindowsClipboardService.SetTextAsync(export).ContinueWith((clipboardResult) =>
            {
                if (clipboardResult.IsFaulted)
                {
                    ScreenNotification.ShowNotification(Strings.Common.gmNotificationClipboardError, ScreenNotification.NotificationType.Red, duration: 2);
                }
                else
                {
                    ScreenNotification.ShowNotification(String.Format(Strings.Common.gmNotificationClipboardSaved, (i)), duration: 2);
                }
            });
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

                                AddWPToList((int)trek.Element("id"), (int)trek.Element("map_id"));
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
                Location = new Point(LEFT_MARGIN, 5 + position * 70),
                Parent = parent
            };
            Image trekPanelWPImage = new Image(_waypointIcon)
            {
                Size = new Point(50, 50),
                Location = new Point(0, 4),
                Parent = trekPanel
            };
            trekPanelWPImage.Click += delegate
            {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(trek.Element("name_" + ShortUserLocale).Value + " " + trek.Element("chat_link").Value).ContinueWith((clipboardResult) =>
                {
                    if (clipboardResult.IsFaulted)
                    {
                        ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
                    }
                    else
                    {
                        ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
                    }
                });
            };
            new Label()
            {
                Text = trek.Element("name_" + ShortUserLocale).Value + " (" + trek.Element("map_name_" + ShortUserLocale).Value + ")",
                Font = Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN + 50, 3),
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
                Location = new Point(LEFT_MARGIN + 50, 32),
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
                addImage.Click += delegate { AddWPToList((int)trek.Element("id"), (int)trek.Element("map_id")); };
            }

            if (remove)
            {
                Image removeImage = new Image(_closeTexture)
                {
                    Size = new Point(20, 20),
                    Location = new Point(parent.Width - 40, 4),
                    Parent = trekPanel
                };
                removeImage.Click += delegate { RemoveWPFromList((int)trek.Element("id")); };
            }
        }

        private void ViewInfoPanel(XElement element, Panel parent, int position, String type, int offset = 0)
        {
            Panel trekPanel = new Panel()
            {
                ShowBorder = false,
                //Title = trek.Element("name").Value + " (" + trek.Element("map_name").Value + ")",
                Size = new Point(parent.Width, 70),
                Location = new Point(LEFT_MARGIN, 5 + position * 70),
                Parent = parent
            };
            Image trekPanelWPImage = new Image(_waypointIcon)
            {
                Size = new Point(50, 50),
                Location = new Point(0, 4),
                Parent = trekPanel
            };
            trekPanelWPImage.Click += delegate
            {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(element.Element("name_" + ShortUserLocale).Value + " " + element.Element("chat_link").Value).ContinueWith((clipboardResult) =>
                {
                    if (clipboardResult.IsFaulted)
                    {
                        ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
                    }
                    else
                    {
                        ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
                    }
                });
            };
            new Label()
            {
                Text = element.Element("name_" + ShortUserLocale).Value + " (" + element.Element("map_name_" + ShortUserLocale).Value + ")",
                Font = Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN + 50, 3),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = trekPanel
            };
            new Label()
            {
                Text = element.Element("waypoint_name_" + ShortUserLocale).Value,
                Font = Content.DefaultFont14,
                Location = new Point(LEFT_MARGIN + 50, 32),
                TextColor = Color.Silver,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = trekPanel
            };
            Image addImage = new Image(_rightArrowIcon)
            {
                Size = new Point(70, 70),
                Location = new Point(parent.Width - 70 - offset, -10),
                Parent = trekPanel
            };
            addImage.Click += delegate { DisplayInfo((int)element.Element("id"), type, element); };
        }

        private void ViewInfoPanelWiki(XElement element, Panel parent, int position)
        {
            Panel trekPanel = new Panel()
            {
                ShowBorder = false,
                Size = new Point(parent.Width, 70),
                Location = new Point(LEFT_MARGIN, 5 + position * 70),
                Parent = parent
            };
            Image trekPanelWPImage = new Image(_waypointIcon)
            {
                Size = new Point(50, 50),
                Location = new Point(0, 4),
                Parent = trekPanel
            };
            trekPanelWPImage.Click += delegate
            {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(element.Element("name_" + ShortUserLocale).Value + " " + element.Element("chat_link").Value).ContinueWith((clipboardResult) =>
                {
                    if (clipboardResult.IsFaulted)
                    {
                        ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
                    }
                    else
                    {
                        ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
                    }
                });
            };
            new Label()
            {
                Text = element.Element("name_" + ShortUserLocale).Value + " (" + element.Element("map_name_" + ShortUserLocale).Value + ")",
                Font = Content.DefaultFont16,
                Location = new Point(LEFT_MARGIN + 50, 3),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = trekPanel
            };
            new Label()
            {
                Text = element.Element("waypoint_name_" + ShortUserLocale).Value,
                Font = Content.DefaultFont14,
                Location = new Point(LEFT_MARGIN + 50, 32),
                TextColor = Color.Silver,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = trekPanel
            };
            var openWikiBttn = new StandardButton()
            {
                Text = Strings.Common.gmButtonWiki,
                Size = new Point(110, BUTTON_HEIGHT),
                Location = new Point(parent.Width - 110 - 50, 10),
                Parent = trekPanel
            };
            openWikiBttn.Click += delegate { Process.Start(element.Element("wiki_link_" + ShortUserLocale).Value); };
        }

        private void DisplayInfo(int v, String type, XElement element)
        {
            int offset = 0;

            infoPanel.ClearChildren();
            infoPanel.Title = Strings.Common.gmPanelInfo + ": " + element.Element("name_" + ShortUserLocale).Value;

            if (element.Element("wiki_link_" + ShortUserLocale) != null)
            {
                var openWikiBttn = new StandardButton()
                {
                    Text = Strings.Common.gmButtonWiki,
                    Size = new Point(110, 30),
                    Location = new Point(4, 4),
                    Parent = infoPanel,
                };
                openWikiBttn.Click += delegate { Process.Start(element.Element("wiki_link_" + ShortUserLocale).Value); };
                offset += 40;
            }

            switch (type)
            {
                case "race":
                    new Image(_guildRaceMap[v])
                    {
                        Size = new Point(310, 500),
                        Location = new Point(4, 4 + offset),
                        Parent = infoPanel
                    };
                    break;
                default:
                    break;
            }
        }

        protected override void Update(GameTime gameTime)
        {

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
