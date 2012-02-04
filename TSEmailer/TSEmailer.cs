/*
 * TSEmailer - An Email System for TShock Terraria Dedicated Server
 * Commands:
 *      - /email - Displays usage example and "Try '/email help' For list of TSEmail commands...".
 *      - /email help - Displays list of subcommands.
 *      - /email <command> help - Displays command usage example.
 *      - /email <user> <message> - Sends email to <user> only if their email address is registered.
 *      - /email settings - Displays email address registered and display option statuses. Display unread message count.
 *      - /email address <email address> - Set self email address.
 *      - /email players [true|false] - Allow/Disallow other players to email directly.
 *      - /email admins [true|false] - Allow/Disallow admins to email directly.
 *      - /email eblast [true|false] - Allow/Disallow receiving email directed to all registered users.
 *      - /email reply [true|false] - Allow/Disallow use sending player's email address as the 'reply-to' address.
 *      - /email notify [true|false] - Allow/Disallow sending of email when player joins server.
 *      - /email onjoin <user> - Send email to self when <user> has joined the server.
 *
 * JSON Options:
 *      - smtpserver  - SMTP relay server IP address or hostname
 *      - smtpport    - SMTP server port
 *      - mapiserver  - MAPI server for receiving messages
 *      - mapiport    - MAPI server port
 *      - smtpuser    - SMTP username for servers requiring authentication. If "" authentication is not required.
 *      - smtppass    - SMTP password for servers requiring authentication. If smtpuser is "" this is ignored.
 *      - mapiuser    - MAPI username
 *      - mapipass    - MAPI password
 *      - mailtimer   - Delay in minutes for mail checking.
 *      - jointimer   - Delay in seconds to allow joining player to disable sending "onjoin" notifications.
 *      - newplayer   - TRUE|FALSE sending of notification emails onjoin of unrecongnized players.
 * 
 * DEVTASKS:
 * Check tsemail.json for email relay settings.
 * Check for tables in DB at runtime and create them if nonexistant.
 * Player OnJoin, add their ID to TSEplayertable.
 * Prompt unrecognized players to register their email.
 * Check mail relay for messages and display them to user if they are on. Leave unread until recipient joins then display.
 */

using System;
using Terraria;
using System.IO;
using jsonConfig;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;
using System.Collections.Generic;
using System.Data;
using sqlProvider;
using smtpWrapper;

namespace TSEmailer
{
    [APIVersion(1, 11)]
    public class TSEmailer : TerrariaPlugin
    {
        public static jsConfig TSEConfig { get; set; }
        internal static string TSEConfigPath { get { return Path.Combine(TShock.SavePath, "TSEconfig.json"); } }
        public static TSdb TSESql;
        public static TSEsmtp TSEsender;

        public override Version Version
        {
            get { return new Version("0.0.0.1"); }
        }

        public override string Name
        {
            get { return "TerrariaServer Emailer"; }
        }

        public override string Author
        {
            get { return "Travis Dieckmann"; }
        }

        public override string Description
        {
            get { return "TSEmailer is a plugin designed to allow sending of email directly to fellow players."; }
        }

        public TSEmailer(Main game)
            : base(game)
        {
            Order = 4;
            TSEConfig = new jsConfig();
        }

        public override void Initialize()
        {
            SetupConfig();
            TSESql = new TSdb();
            InitSMTP();
            Commands.ChatCommands.Add(new Command("", OnEmail, "email"));
        }

        private void OnEmail(CommandArgs args)
        {
            Color ErrColor = Color.Plum;

            if (args.Player != null)
            {
                switch (args.Parameters[0].ToString())
                {
                    case "settings":
                        GetStatus(args.Player);
                        break;
                    case "address":
                        SetAddress(args.Player, args.Parameters[1]);
                        break;
                    case "players":
                        args.Player.SendMessage("/email players - A work in progress...>", ErrColor);
                        break;
                    case "admins":
                        args.Player.SendMessage("/email admins - A work in progress...>", ErrColor);
                        break;
                    case "eblast":
                        args.Player.SendMessage("/email eblast - A work in progress...>", ErrColor);
                        break;
                    case "reply":
                        args.Player.SendMessage("/email reply - A work in progress...>", ErrColor);
                        break;
                    case "notify":
                        args.Player.SendMessage("/email address - A work in progress...>", ErrColor);
                        break;
                    case "onjoin":
                        SetOnJoin(args.Player, args.Parameters[1]);
                        break;
                    case "testemail":
                        SendTestEmail(args.Parameters[1]);
                        break;
                    case "help":
                        args.Player.SendMessage("example: /email <player> <\"Email message body\">", ErrColor);
                        args.Player.SendMessage("Email Commands:", ErrColor);
                        args.Player.SendMessage("  /email address <youraddress@yourdomain.com> - Sets your email address.", ErrColor);
                        args.Player.SendMessage("  /email help1 - Displays more help.", ErrColor);
                        args.Player.SendMessage("  /email help2 - Displays even more help.", ErrColor);
                        break;
                    case "help1": 
                        args.Player.SendMessage("Email Commands:", ErrColor);
                        args.Player.SendMessage("  /email settings - Displays your current settings.", ErrColor);
                        args.Player.SendMessage("  /email address <youraddress@yourdomain.com> - Sets your email address.", ErrColor);
                        args.Player.SendMessage("  /email players [true|false] - Allow/Disallow other players to email directly.", ErrColor);
                        args.Player.SendMessage("  /email admins [true|false] - Receive email from admins.", ErrColor);
                        args.Player.SendMessage("  /email eblast [true|false] - Receive email directed to all registered users.", ErrColor);
                        break;
                    case "help2":
                        args.Player.SendMessage("Email Commands:", ErrColor);
                        args.Player.SendMessage("  /email reply [true|false] - Users can reply to your email address.", ErrColor);
                        args.Player.SendMessage("  /email notify [true|false] - Others to be notified when you join the server.", ErrColor);
                        args.Player.SendMessage("  /email onjoin <user> - Receive an email when <user> has joined the server.", ErrColor);
                        args.Player.SendMessage("  /email onjoinlist - List of users names that you receive notifications.", ErrColor);
                        args.Player.SendMessage("  /email remove <user> - Remove user from your notification list.", ErrColor);
                        break;
                    default:
                        SendEmail(args.Player, args.Parameters[1], args.Parameters[2]);
                        break;
                }
            }
        }

        private void SetAddress(TSPlayer player, string address)
        {
            if (player != null)
            {
                if(address != "")
                {
                    player.SendMessage(TSESql.SetPlayerAddress(player, address), Color.Blue);
                }
                else
                {
                    player.SendMessage(TSESql.RemoveAddress(player), Color.Blue);
                }
            }
        }

        private void SendEmail(TSPlayer player, string rcptPlayer, string body)
        {
            Color ErrColor = Color.Plum;

            if (TSESql.GetPlayerIndex(player.UserID) != -1)
            {
                int rcptIndex = TSESql.GetPlayerIndex(rcptPlayer);
                if (rcptIndex != -1)
                {
                    
                }
                else
                {
                    player.SendMessage("The player specified isn't registered with the server!", ErrColor);
                }
            }
            else
            {
                player.SendMessage("Your email address is not registered with the server!", ErrColor);
            }
        }

        private void SetOnJoin(TSPlayer player, string PlayerName)
        {
            if (player != null)
            {
                player.SendMessage(TSESql.SetOnJoinAddress(player.UserID, PlayerName), Color.Blue);
            }
        }

        private void GetStatus(TSPlayer player)
        {
            if (player != null)
            {
                Color ErrColor = Color.Blue;
                int index = TSESql.GetPlayerIndex(player.UserID);
                if (index != -1)
                {
                    List<SqlValue> settings = TSESql.GetSettings(index);
                    player.SendMessage("Your email address: " + settings[1].Value.ToString(), ErrColor);
                    player.SendMessage("Players can email you: " + settings[2].Value.ToString(), ErrColor);
                    player.SendMessage("Admins can email you: " + settings[3].Value.ToString(), ErrColor);
                    player.SendMessage("You can receive email blasts: " + settings[4].Value.ToString(), ErrColor);
                    player.SendMessage("Players can reply directly to your email: " + settings[5].Value.ToString(), ErrColor);
                    player.SendMessage("Allow others to be notified when you join the server: " + settings[6].Value.ToString(), ErrColor);
                }
                else
                {
                    player.SendMessage("You have not registered with the server!", ErrColor);
                }
                
            }
        }

        public static void SendTestEmail(string recipient)
        {
            try
            {
                TSEsender.SendEmail(
                    recipient,
                    TSEConfig.smtpaddress,
                    "Test Subject",
                    "Test Body");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("TSEmailer - Error in config file");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("TSEmailer - Config Exception");
                Log.Error(ex.ToString());
            }
            

        }

        public static void SetupConfig()
        {
            try
            {
                if (File.Exists(TSEConfigPath))
                {
                    TSEConfig = jsConfig.Read(TSEConfigPath);
                    // Add all the missing config properties in the json file
                }
                TSEConfig.Write(TSEConfigPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("TSEmailer - Error in config file");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("TSEmailer - Config Exception");
                Log.Error(ex.ToString());
            }
        }

        public static void InitSMTP()
        {
            if (TSEConfig.smtpuser != "")
            {
                TSEsender = new TSEsmtp(
                    TSEConfig.smtpserver,
                    TSEConfig.smtpport,
                    TSEConfig.smtpuser,
                    TSEConfig.smtppass,
                    TSEConfig.smtptls);
            }
            else
            {
                TSEsender = new TSEsmtp(
                    TSEConfig.smtpserver,
                    TSEConfig.smtpport);
            }
        }
    }
}
