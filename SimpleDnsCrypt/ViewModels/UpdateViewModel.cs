﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using minisign;
using SimpleDnsCrypt.Models;
using SimpleDnsCrypt.Tools;

namespace SimpleDnsCrypt.ViewModels
{
    /// <summary>
    ///     The application update view model
    /// </summary>
    [Export]
    public class UpdateViewModel : Screen
    {
        private byte[] _installer;
        private string _installerPath;
        private bool _isInstallerDownloaded;

        private bool _isSignatureDownloaded;
        private bool _isUpdatingInstaller;
        private bool _isUpdatingSignature;

        private string _signature;

        /// <summary>
        ///     Xaml constructor.
        /// </summary>
        public UpdateViewModel()
        {
        }

        /// <summary>
        ///     UpdateViewModel constructor.
        /// </summary>
        [ImportingConstructor]
        public UpdateViewModel(Update update)
        {
            _isSignatureDownloaded = false;
            _isInstallerDownloaded = false;
            StartUpdateAsync(update);
        }


        /// <summary>
        ///     The path of the downloaded and validated installer file.
        /// </summary>
        public string InstallerPath
        {
            get { return _installerPath; }
            set
            {
                _installerPath = value;
                NotifyOfPropertyChange(() => InstallerPath);
            }
        }

        /// <summary>
        ///     Indicates if the signature download is completed.
        /// </summary>
        public bool IsSignatureDownloaded
        {
            get { return _isSignatureDownloaded; }
            set
            {
                _isSignatureDownloaded = value;
                NotifyOfPropertyChange(() => IsSignatureDownloaded);
            }
        }

        /// <summary>
        ///     Indicates if the installer download is completed.
        /// </summary>
        public bool IsInstallerDownloaded
        {
            get { return _isInstallerDownloaded; }
            set
            {
                _isInstallerDownloaded = value;
                NotifyOfPropertyChange(() => IsInstallerDownloaded);
            }
        }

        /// <summary>
        ///     Indicates if the signature is downloading.
        /// </summary>
        public bool IsUpdatingSignature
        {
            get { return _isUpdatingSignature; }
            set
            {
                _isUpdatingSignature = value;
                NotifyOfPropertyChange(() => IsUpdatingSignature);
            }
        }

        /// <summary>
        ///     Indicates if the installer is downloading.
        /// </summary>
        public bool IsUpdatingInstaller
        {
            get { return _isUpdatingInstaller; }
            set
            {
                _isUpdatingInstaller = value;
                NotifyOfPropertyChange(() => IsUpdatingInstaller);
            }
        }

        /// <summary>
        ///     Do an update.
        /// </summary>
        /// <param name="update">The update to perform.</param>
        public async void StartUpdateAsync(Update update)
        {
            try
            {
                IsUpdatingSignature = true;
                IsUpdatingInstaller = true;
                await DownloadSignatureAsync(update.Signature);
                await DownloadInstallerAsync(update.Installer);

                if ((IsInstallerDownloaded) && (IsSignatureDownloaded))
                {
                    var s = _signature.Split('\n');
                    var trimmedComment = s[2].Replace("trusted comment: ", "").Trim();
                    var trustedCommentBinary = Encoding.UTF8.GetBytes(trimmedComment);
                    var loadedSignature = Minisign.LoadSignature(Convert.FromBase64String(s[1]), trustedCommentBinary,
                        Convert.FromBase64String(s[3]));
                    var publicKey = Minisign.LoadPublicKeyFromString(update.Publickey);
                    var valid = Minisign.ValidateSignature(_installer, loadedSignature, publicKey);

                    if (valid)
                    {
                        var path = Path.Combine(Path.GetTempPath(), update.Installer.Name);
                        File.WriteAllBytes(path, _installer);
                        if (File.Exists(path))
                        {
                            InstallerPath = path;
                            TryClose(true);
                        }
                    }
                    else
                    {
                        //failed
                        InstallerPath = string.Empty;
                    }
                }
                else
                {
                    //failed
                    InstallerPath = string.Empty;
                }
            }
            catch (Exception)
            {
            }
            TryClose(false);
        }

        /// <summary>
        ///     Download the given signature file.
        /// </summary>
        /// <param name="remoteSignature">The signature to download.</param>
        /// <returns></returns>
        public async Task DownloadSignatureAsync(Signature remoteSignature)
        {
            _signature =
                await ApplicationUpdater.DownloadRemoteSignatureAsync(remoteSignature.Uri).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_signature))
            {
                IsSignatureDownloaded = true;
            }
            IsUpdatingSignature = false;
        }

        /// <summary>
        ///     Download the given installer file.
        /// </summary>
        /// <param name="remoteInstaller">The installer to download.</param>
        /// <returns></returns>
        public async Task DownloadInstallerAsync(Installer remoteInstaller)
        {
            _installer =
                await ApplicationUpdater.DownloadRemoteInstallerAsync(remoteInstaller.Uri).ConfigureAwait(false);
            if (_installer != null)
            {
                IsInstallerDownloaded = true;
            }
            IsUpdatingInstaller = false;
        }
    }
}