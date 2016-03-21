﻿//
//  PatchProtocolHandler.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Launchpad.Launcher.Handlers.Protocols
{
	/// <summary>
	/// Patch protocol handler.
	/// This class is the base class for all file transfer protocols, providing
	/// a common framework for protocols to adhere to. It abstracts away the actual
	/// functionality, and reduces the communication with other parts of the launcher
	/// down to requests in, files out.
	///
	/// By default, the patch protocol handler does not know anything specific about
	/// the actual workings of the protocol.
	/// </summary>
	internal abstract class PatchProtocolHandler
	{
		protected PatchProtocolHandler()
		{
			ModuleInstallFinishedArgs = new ModuleInstallationFinishedArgs();
			ModuleInstallFailedArgs = new ModuleInstallationFailedArgs();
		}

		/// <summary>
		/// The config handler reference.
		/// </summary>
		protected ConfigHandler Config = ConfigHandler._instance;

		public event ModuleDownloadProgressChangedEventHandler ModuleDownloadProgressChanged;
		public event ModuleCopyProgressChangedEventHandler ModuleCopyProgressChanged;
		public event ModuleVerifyProgressChangedEventHandler ModuleVerifyProgressChanged;
		public event ModuleUpdateProgressChangedEventHandler ModuleUpdateProgressChanged;

		public event ModuleInstallationFinishedEventHandler ModuleInstallationFinished;
		public event ModuleInstallationFailedEventHandler ModuleInstallationFailed;

		protected readonly ModuleProgressChangedArgs ModuleDownloadProgressArgs = new ModuleProgressChangedArgs();
		protected readonly ModuleProgressChangedArgs ModuleCopyProgressArgs = new ModuleProgressChangedArgs();
		protected readonly ModuleProgressChangedArgs ModuleVerifyProgressArgs = new ModuleProgressChangedArgs();
		protected readonly ModuleProgressChangedArgs ModuleUpdateProgressArgs = new ModuleProgressChangedArgs();

		protected ModuleInstallationFinishedArgs ModuleInstallFinishedArgs;
		protected ModuleInstallationFailedArgs ModuleInstallFailedArgs;

		/// <summary>
		/// Determines whether this instance can provide patches. Checks for an active connection to the
		/// patch provider (file server, distributed hash tables, hyperspace compression waves etc.)
		/// </summary>
		/// <returns><c>true</c> if this instance can provide patches; otherwise, <c>false</c>.</returns>
		public abstract bool CanPatch();

		/// <summary>
		/// Checks whether or not the launcher has a new patch available.
		/// </summary>
		/// <returns><c>true</c>, if there's a patch available, <c>false</c> otherwise.</returns>
		public abstract bool IsLauncherOutdated();

		/// <summary>
		/// Checks whether or not the game has a new patch available.
		/// </summary>
		/// <returns><c>true</c>, if there's a patch available, <c>false</c> otherwise.</returns>
		public abstract bool IsGameOutdated();

		/// <summary>
		/// Installs or updates the launcher as neccesary.
		/// </summary>
		public abstract void InstallLauncher();

		/// <summary>
		/// Installs or updates the the game as neccesary.
		/// </summary>
		public abstract void InstallGame();

		/// <summary>
		/// Downloads the latest version of the launcher.
		/// </summary>
		protected abstract void DownloadLauncher();

		/// <summary>
		/// Downloads the latest version of the game.
		/// </summary>
		protected abstract void DownloadGame();

		/// <summary>
		/// Copies the game to the installation directory.
		/// Normal copying procedures are provided by PatchProtocolHandler, but can be overridden as neccesary.
		/// </summary>
		protected virtual void CopyGame()
		{
			if (Directory.Exists(ConfigHandler.GetTempGameDownloadDir()))
			{
				ModuleCopyProgressArgs.Module = EModule.Game;
				ModuleCopyProgressArgs.IndicatorLabelMessage = "Copying game to installation directory...";

				List<string> gameFiles = Directory.EnumerateFiles(ConfigHandler.GetTempGameDownloadDir(), "*", SearchOption.AllDirectories).ToList();
				foreach (string gameFile in gameFiles)
				{
					ModuleCopyProgressArgs.ProgressBarMessage = String.Format("Copying {0} to installation directory...", Path.GetFileName(gameFile));
					OnModuleCopyProgressChanged();

					// Copy the file
					string basePath = gameFile.Replace(ConfigHandler.GetTempGameDownloadDir(), "");
					string destinationPath = Config.GetGamePath(true) + basePath;
					File.Copy(gameFile, destinationPath, true);
				}
			}
		}

		/// <summary>
		/// Verifies and repairs the launcher files.
		/// </summary>
		public abstract void VerifyLauncher();

		/// <summary>
		/// Verifies and repairs the game files.
		/// </summary>
		public abstract void VerifyGame();

		public abstract void UpdateLauncher();

		public abstract void UpdateGame();

		protected void OnModuleDownloadProgressChanged()
		{
			if (ModuleDownloadProgressChanged != null)
			{
				ModuleDownloadProgressChanged(this, ModuleDownloadProgressArgs);
			}
		}

		protected void OnModuleCopyProgressChanged()
		{
			if (ModuleCopyProgressChanged != null)
			{
				ModuleCopyProgressChanged(this, ModuleCopyProgressArgs);
			}
		}

		protected void OnModuleVerifyProgressChanged()
		{
			if (ModuleVerifyProgressChanged != null)
			{
				ModuleVerifyProgressChanged(this, ModuleVerifyProgressArgs);
			}
		}

		protected void OnModuleUpdateProgressChanged()
		{
			if (ModuleUpdateProgressChanged != null)
			{
				ModuleUpdateProgressChanged(this, ModuleUpdateProgressArgs);
			}
		}

		protected void OnModuleInstallationFinished()
		{
			if (ModuleInstallationFinished != null)
			{
				ModuleInstallationFinished(this, ModuleInstallFinishedArgs);
			}
		}

		protected void OnModuleInstallationFailed()
		{
			if (ModuleInstallationFailed != null)
			{
				ModuleInstallationFailed(this, ModuleInstallFailedArgs);
			}
		}
	}

	/// <summary>
	/// A list of modules that can be downloaded and reported on.
	/// </summary>
	public enum EModule : byte
	{
		Launcher,
		Game
	}

	/*
		Common events for all patching protocols
	*/
	public delegate void ModuleInstallationProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);
	public delegate void ModuleDownloadProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);
	public delegate void ModuleCopyProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);
	public delegate void ModuleVerifyProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);
	public delegate void ModuleUpdateProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);

	public delegate void ModuleInstallationFinishedEventHandler(object sender,ModuleInstallationFinishedArgs e);
	public delegate void ModuleInstallationFailedEventHandler(object sender,ModuleInstallationFailedArgs e);

	/*
		Common arguments for all patching protocols
	*/
	public sealed class ModuleProgressChangedArgs : EventArgs
	{
		public EModule Module
		{
			get;
			set;
		}

		public string ProgressBarMessage
		{
			get;
			set;
		}

		public string IndicatorLabelMessage
		{
			get;
			set;
		}

		public double ProgressFraction
		{
			get;
			set;
		}
	}

	public sealed class ModuleInstallationFinishedArgs : EventArgs
	{
		public EModule Module
		{
			get;
			set;
		}
	}

	public sealed class ModuleInstallationFailedArgs : EventArgs
	{
		public EModule Module
		{
			get;
			set;
		}
	}
}

