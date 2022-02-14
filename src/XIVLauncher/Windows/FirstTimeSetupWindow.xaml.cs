﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using CheapLoc;
using IWshRuntimeLibrary;
using XIVLauncher.Addon;
using XIVLauncher.Game;
using XIVLauncher.Windows.ViewModel;

namespace XIVLauncher.Windows
{
    /// <summary>
    ///     Interaction logic for FirstTimeSetup.xaml
    /// </summary>
    public partial class FirstTimeSetup : Window
    {
        public bool WasCompleted { get; private set; } = false;

        private readonly string actPath;

        public FirstTimeSetup()
        {
            InitializeComponent();

            this.DataContext = new FirstTimeSetupViewModel();

            var detectedPath = Util.TryGamePaths();

            if (detectedPath != null) GamePathEntry.Text = detectedPath;

            this.actPath = this.FindAct();

#if !XL_NOAUTOUPDATE
            if (EnvironmentSettings.IsDisableUpdates || Util.GetBuildOrigin() != "goatcorp/FFXIVQuickLauncher")
            {
#endif
                CustomMessageBox.Show(
                    $"You're running a version of XIVLauncher modified for macOS.\n\n If you are having authentication/update trouble, please do not conatact the goatplace discord but rather https://discord.gg/5wsgJePv.",
                    "XIVLauncher Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
#if !XL_NOAUTOUPDATE
            }
#endif
        }

        public static string GetShortcutTargetFile(string path)
        {
            var shell = new WshShell();
            var shortcut = (IWshShortcut) shell.CreateShortcut(path);

            return shortcut.TargetPath;
        }

        private string FindAct()
        {
            try
            {
                var shortcutDir = new DirectoryInfo(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs",
                    "Advanced Combat Tracker"));
                var shortcutPath = Path.Combine(shortcutDir.FullName, "ACT - Advanced Combat Tracker.lnk");

                return System.IO.File.Exists(shortcutPath) ? GetShortcutTargetFile(shortcutPath) : null;
            }
            catch
            {
                return null;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (SetupTabControl.SelectedIndex == 0)
            {
                if (string.IsNullOrEmpty(GamePathEntry.Text))
                {
                    CustomMessageBox.Show(Loc.Localize("GamePathEmptyError", "Please select a game path."), "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error, false, false);
                    return;
                }

                if (!Util.LetChoosePath(GamePathEntry.Text))
                {
                    CustomMessageBox.Show(Loc.Localize("GamePathSafeguardError", "Please do not select the \"game\" or \"boot\" folder of your FFXIV installation, and choose the folder that contains these instead."), "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!Util.IsValidFfxivPath(GamePathEntry.Text))
                {
                    CustomMessageBox.Show(Loc.Localize("GamePathInvalidError", "The folder you selected has no FFXIV installation.\nXIVLauncher will install FFXIV the first time you log in."), "XIVLauncher",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            if (SetupTabControl.SelectedIndex == 2)
            {
                // Check if ACT is installed, if it isn't, just skip this step
                if (string.IsNullOrEmpty(this.actPath))
                {
                    SetupTabControl.SelectedIndex++;
                    NextButton_Click(null, null);
                    return;
                }
            }

            if (SetupTabControl.SelectedIndex == 4)
            {
                App.Settings.GamePath = new DirectoryInfo(GamePathEntry.Text);
                App.Settings.IsDx11 = Dx11RadioButton.IsChecked == true;
                App.Settings.Language = (ClientLanguage) LanguageComboBox.SelectedIndex;
                App.Settings.InGameAddonEnabled = HooksCheckBox.IsChecked == true;
                App.Settings.SteamIntegrationEnabled = SteamCheckBox.IsChecked == true;

                App.Settings.AddonList = new List<AddonEntry>();

                if (ActCheckBox.IsChecked == true)
                {
                    App.Settings.AddonList.Add(new AddonEntry
                    {
                        IsEnabled = true,
                        Addon = new GenericAddon
                        {
                            Path = this.actPath
                        }
                    });
                }

                WasCompleted = true;
                Close();
            }

            SetupTabControl.SelectedIndex++;
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            Dx9DisclaimerTextBlock.Visibility = Visibility.Visible;
        }

        private void Dx9RadioButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Dx9DisclaimerTextBlock.Visibility = Visibility.Hidden;
        }
    }
}