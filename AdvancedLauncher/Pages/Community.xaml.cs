﻿// ======================================================================
// DIGIMON MASTERS ONLINE ADVANCED LAUNCHER
// Copyright (C) 2014 Ilya Egorov (goldrenard@gmail.com)

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
// ======================================================================

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AdvancedLauncher.Environment;
using AdvancedLauncher.Service;
using AdvancedLauncher.Validators;
using DMOLibrary;
using DMOLibrary.Database.Entity;
using DMOLibrary.Profiles;

namespace AdvancedLauncher.Pages {

    public partial class Community : AbstractPage {

        private delegate void DoOneText(string text);

        private AbstractWebProfile webProfile;

        private Guild CURRENT_GUILD = new Guild() {
            Id = -1
        };

        protected override void InitializeAbstractPage() {
            InitializeComponent();
        }

        public Community() {
            TDBlock_.TabChanged += OnTabChanged;
        }

        protected override void ProfileChanged() {
            //Очищаем все поля и списки
            GMaster.Text = GRank.Text = GRep.Text = GTop.Text = GDCnt.Text = GTCnt.Text = "-";
            TDBlock_.ClearAll();
            chkbox_IsDetailed.IsChecked = false;

            //Загружаем новый список серверов
            ComboBoxServer.ItemsSource = LauncherEnv.Settings.CurrentProfile.DMOProfile.ServerList;

            //Активируем новый профиль
            webProfile = LauncherEnv.Settings.CurrentProfile.DMOProfile.GetWebProfile();

            //Если есть название гильдии в ротации, вводим его и сервер
            if (!string.IsNullOrEmpty(LauncherEnv.Settings.CurrentProfile.Rotation.Guild)) {
                foreach (Server serv in ComboBoxServer.Items) {
                    //Ищем сервер с нужным идентификатором и выбираем его
                    if (serv.Id == LauncherEnv.Settings.CurrentProfile.Rotation.ServerId + 1) {
                        ComboBoxServer.SelectedValue = serv;
                        break;
                    }
                }
                GuildNameTextBox.Text = LauncherEnv.Settings.CurrentProfile.Rotation.Guild;
            } else {
                GuildNameTextBox.Text = LanguageEnv.Strings.CommGuildName;
                if (ComboBoxServer.Items.Count > 0) {
                    ComboBoxServer.SelectedIndex = 0;
                }
            }
        }

        private void OnTabChanged(object sender, int tabNum) {
            if (tabNum == 1) {
                TamerTab.IsEnabled = false;
                DigimonTab.IsEnabled = true;
            } else {
                TamerTab.IsEnabled = true;
                DigimonTab.IsEnabled = false;
            }
        }

        private void OnTamerTabClick(object sender, RoutedEventArgs e) {
            if (CURRENT_GUILD.Id != -1) {
                TDBlock_.ShowTamers();
            }
        }

        private void OnDigimonTabClick(object sender, RoutedEventArgs e) {
            if (CURRENT_GUILD.Id != -1) {
                TDBlock_.ShowDigimons(CURRENT_GUILD.Tamers);
            }
        }

        private void OnStatusChanged(object sender, DownloadStatus status) {
            switch (status.Code) {
                case DMODownloadStatusCode.GETTING_GUILD: {
                        LoadProgressStatus.Text = LanguageEnv.Strings.CommSearchingGuild;
                        break;
                    }
                case DMODownloadStatusCode.GETTING_TAMER: {
                        LoadProgressStatus.Text = string.Format(LanguageEnv.Strings.CommGettingTamer, status.Info);
                        break;
                    }
            }
            LoadProgressBar.Maximum = status.MaxProgress;
            LoadProgressBar.Value = status.Progress;
        }

        private void OnDownloadCompleted(object sender, DMODownloadResultCode code, Guild result) {
            BlockControls(false);

            webProfile.DownloadStarted -= OnDownloadStarted;
            webProfile.DownloadCompleted -= OnDownloadCompleted;
            webProfile.StatusChanged -= OnStatusChanged;

            ProgressBlock.Opacity = 0;
            switch (code) {
                case DMODownloadResultCode.OK: {
                        CURRENT_GUILD = result;
                        UpdateInfo(CURRENT_GUILD);
                        TDBlock_.ShowTamers(CURRENT_GUILD.Tamers);
                        break;
                    }
                case DMODownloadResultCode.CANT_GET: {
                        Utils.MSG_ERROR(LanguageEnv.Strings.CantGetError);
                        break;
                    }
                case DMODownloadResultCode.DB_CONNECT_ERROR: {
                        Utils.MSG_ERROR(LanguageEnv.Strings.DBConnectionError);
                        break;
                    }
                case DMODownloadResultCode.NOT_FOUND: {
                        Utils.MSG_ERROR(LanguageEnv.Strings.GuildNotFoundError);
                        break;
                    }
                case DMODownloadResultCode.WEB_ACCESS_ERROR: {
                        Utils.MSG_ERROR(LanguageEnv.Strings.ConnectionError);
                        break;
                    }
            }
        }

        private void OnDownloadStarted(object sender) {
            BlockControls(true);
            LoadProgressBar.Value = 0;
            LoadProgressBar.Maximum = 100;
            LoadProgressStatus.Text = string.Empty;
            ProgressBlock.Opacity = 1;
        }

        private void OnGetInfoClick(object sender, RoutedEventArgs e) {
            if (IsValidName(GuildNameTextBox.Text)) {
                webProfile.DownloadStarted += OnDownloadStarted;
                webProfile.DownloadCompleted += OnDownloadCompleted;
                webProfile.StatusChanged += OnStatusChanged;

                AbstractWebProfile.GetActualGuildAsync(this.Dispatcher,
                    webProfile,
                    (Server)ComboBoxServer.SelectedValue,
                    GuildNameTextBox.Text,
                    (bool)chkbox_IsDetailed.IsChecked,
                    1);
            }
        }

        public void BlockControls(bool block) {
            LauncherEnv.Settings.OnProfileLocked(block);
            GuildNameTextBox.IsEnabled = !block;
            ComboBoxServer.IsEnabled = !block;
            button_getinfo.IsEnabled = !block;
            chkbox_IsDetailed.IsEnabled = !block;
        }

        public void UpdateInfo(Guild g) {
            GMaster.Text = g.Master.Name;
            GRank.Text = g.Rank.ToString();
            GRep.Text = g.Rep.ToString();
            Tamer bestTamer = g.Tamers.Aggregate((t1, t2) => (t1.Rank > t2.Rank ? t1 : t2));
            GTop.Text = bestTamer.Name;
            int count = g.Tamers.Select(o => o.Digimons.Count).Aggregate((x, y) => x + y);
            GDCnt.Text = count.ToString();
            GTCnt.Text = g.Tamers.Count.ToString();
        }

        #region Обработка поля ввода имени гильдии

        private void OnNameGotFocus(object sender, RoutedEventArgs e) {
            if (GuildNameTextBox.Text == LanguageEnv.Strings.CommGuildName) {
                GuildNameTextBox.Foreground = Brushes.Black;
                GuildNameTextBox.Text = string.Empty;
            }
        }

        private void OnNameLostFocus(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(GuildNameTextBox.Text)) {
                GuildNameTextBox.Foreground = Brushes.Gray;
                GuildNameTextBox.Text = LanguageEnv.Strings.CommGuildName;
            }
        }

        public static bool IsValidName(string name) {
            if (name == LanguageEnv.Strings.CommGuildName) {
                Utils.MSG_ERROR(LanguageEnv.Strings.CommGuildNameEmpty);
                return false;
            }
            GuildNameValidationRule validationRule = new GuildNameValidationRule();
            ValidationResult result = validationRule.Validate(name, new System.Globalization.CultureInfo(1, false));
            if (!result.IsValid) {
                Utils.MSG_ERROR(result.ErrorContent.ToString());
            }
            return result.IsValid;
        }

        #endregion Обработка поля ввода имени гильдии
    }
}