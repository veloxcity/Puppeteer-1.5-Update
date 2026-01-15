using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;
using Verse;
using static HarmonyLib.AccessTools;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class TwitchToolkitMod
	{
		static readonly Action<object, OnMessageReceivedArgs> OnMessageReceived = Tools.GetOptionalMethod<Action<object, OnMessageReceivedArgs>>("ToolkitCore.TwitchWrapper", "OnMessageReceived");
		static readonly Func<string, object> GetViewer = Tools.GetOptionalMethod<Func<string, object>>("TwitchToolkit.Viewers", "GetViewer");
		static readonly MethodInfo m_GetViewerCoins = Method("TwitchToolkit.Viewer:GetViewerCoins");
		static readonly FieldRef<bool> UnlimitedCoins = Tools.GetOptionalStaticFieldRef<bool>("TwitchToolkit.ToolkitSettings", "UnlimitedCoins");
		static readonly Func<List<string>> ParseViewersFromJsonAndFindActiveViewers = Tools.GetOptionalMethod<Func<List<string>>>("TwitchToolkit.Viewers", "ParseViewersFromJsonAndFindActiveViewers");

		public static bool Exists => true
			&& OnMessageReceived != null
			&& GetViewer != null
			&& m_GetViewerCoins != null
			&& UnlimitedCoins != null;

		public static void RefreshViewers()
		{
			var usernames = ParseViewersFromJsonAndFindActiveViewers?.Invoke();
			if (usernames != null)
				foreach (string username in usernames)
					_ = GetViewer(username);
		}

		public static int GetCurrentCoins(string userName)
		{
			if (Exists == false) return -1;
			var userNameLowerCase = userName.ToLower();
			var viewer = GetViewer(userNameLowerCase);
			if (viewer == null) return -2;
			if (UnlimitedCoins()) return 99999999;
			return (int)m_GetViewerCoins.Invoke(viewer, new object[0]);
		}

		public static string[] GetAllCommands()
		{
			var t_DefDatabase = typeof(DefDatabase<>).MakeGenericType(TypeByName("TwitchToolkit.Command"));
			return Traverse.Create(t_DefDatabase)
				.Field("defsList").GetValue<IEnumerable>().Cast<Def>()
				.Select(def => MakeDeepCopy<TTCommand>(def))
				.Where(cmd => !cmd.requiresAdmin && !cmd.requiresMod && cmd.enabled)
				.Select(cmd => cmd.command)
				.OrderBy(txt => txt)
				.ToArray();
		}

		static readonly FieldRef<bool> MinifiableBuildings = Tools.GetOptionalStaticFieldRef<bool>("TwitchToolkit.ToolkitSettings", "MinifiableBuildings");
		public static string[] GetFilteredItems(string searchTerm = null)
		{
			var minifiableBuildings = MinifiableBuildings();
			return DefDatabase<ThingDef>.AllDefs
				.Where(def => (def.tradeability.TraderCanSell() || ThingSetMakerUtility.CanGenerate(def)) && (def.building == null || def.Minifiable || minifiableBuildings) && (def.FirstThingCategory != null || def.race != null) && def.BaseMarketValue > 0f)
				.Select(def => def.label.Replace(" ", ""))
				.Where(name => searchTerm == null || searchTerm == "" || name.ToLower().Contains(searchTerm.ToLower()))
				.OrderBy(name => name)
				.ToArray();
		}

		public static void SendMessage(string userId, string userName, string message)
		{
			// Tools.LogWarning($"USER {userName} SEND {message}");
			var userNameLowerCase = userName.ToLower();

			if (Exists == false) return;

			var tags = new Dictionary<string, string>
			{
				["user-id"] = userId,
				["user-type"] = "viewer",
				["color"] = "#FFFFFF",
			};
			if (message.StartsWith("!") == false) message = $"!{message}";
			var ircMessage = new IrcMessage(TwitchLib.Client.Enums.Internal.IrcCommand.Unknown, new string[] { "", message }, userNameLowerCase, tags);
			var channelEmotes = new MessageEmoteCollection();
			var chatMessage = new ChatMessage("Puppeteer", ircMessage, ref channelEmotes, false);

			var messageArgs = new OnMessageReceivedArgs() { ChatMessage = chatMessage };
			OnMessageReceived(null, messageArgs);
		}


		[HarmonyPatch]
		static class TwitchWrapper_SendChatMessage_Patch
		{
			static readonly MethodBase method = Method("ToolkitCore.TwitchWrapper:SendChatMessage");
			static readonly Regex parser = new Regex("(.*)\\@([^ ]+)(.*)");
			static readonly Regex byRemover = new Regex(" by$");

			public static bool Prepare()
			{
				return method != null;
			}

			public static MethodBase TargetMethod()
			{
				return method;
			}

			public static bool Prefix(string message)
			{
				if (!message.Contains('→') && !message.StartsWith("@"))
                {
					return true;
                }

				/*var match = parser.Match(message);
				if (!match.Success)
				{
					return true;
				}*/

				var username = getname(message);
				var newmessage = getmessage(message);
				var puppeteer = State.Instance.PuppeteerForViewerName(username);

				if (username == "" || newmessage == "")
                {
					string[] test = message.Split(' ');
					puppeteer = State.Instance.PuppeteerForViewerName(test[0]);
					if(puppeteer == null)
                    {
						return true;
					}
					newmessage = message.Substring(test[0].Length + 1);
				}

				if (puppeteer == null || puppeteer.connected == false) return true;

				Controller.instance.SendChatMessage(puppeteer.vID, newmessage);
				if (!PuppeteerMod.Settings.sendChatResponsesToTwitch)
                {
					return false;
                }
				return true;
			}

			private static string getname(string message)
            {
				string sendback = "";

				if (message.Contains("→"))
				{
					var parts = message.Split('→').ToList();
					var userName = parts[0].Trim();
					if (userName.StartsWith("@")) userName = userName.Substring(1);
					sendback = userName;
				}
				else if(message.StartsWith("@"))
				{
					string[] name = message.Split(' ');
					sendback = name[0].Substring(1);
				}

				return sendback;
            }

			private static string getmessage(string message)
            {
				string sendback = "";

				if (message.Contains("→"))
				{
					var parts = message.Split('→').ToList();
					var m1 = parts[1].Trim();
					sendback = m1;
				}
				else if (message.StartsWith("@"))
				{
					int i = message.IndexOf(" ") + 1;
					string str = message.Substring(i);
					sendback = str;
				}

				return sendback;
            }
		}
	}

	class TTCommand
	{
#pragma warning disable 649
		public bool requiresAdmin;
		public bool requiresMod;
		public bool enabled;
		public string command;
#pragma warning restore 649
	}
}
