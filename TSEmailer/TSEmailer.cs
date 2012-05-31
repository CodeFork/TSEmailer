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
using System.IO;
using System.Text;
using System.Data;
using System.Net.Mail;
using System.ComponentModel;
using System.Collections.Generic;
using Terraria;
using jsonConfig;
using TShockAPI;
using TShockAPI.DB;
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
            get { return new Version("0.1.0.0"); }
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
            
            Hooks.NetHooks.GreetPlayer += OnGreetPlayer;
        }

        protected override void Dispose(bool disposing)
        {
            /* Ensure that we are actually disposing.
             */
            if (disposing)
            {
                /* Using the -= operator, we remove our method
                 * from the hook.
                 */
                Hooks.NetHooks.GreetPlayer -= OnGreetPlayer;

                TSEsender.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnEmail(CommandArgs args)
        {
            Color ErrColor = Color.Plum;
            
            if (args.Player != null)
            {
                if (args.Parameters.Count == 0)
                {
                    DisplayHelp(args.Player, 0);
                    return;
                }

                switch (args.Parameters[0].ToString())
                {
                    case "settings":
                        if (args.Parameters.Count != 1)
                        {
                            DisplayHelp(args.Player, 0);
                            break;
                        }
                        GetStatus(args.Player);
                        break;
                    case "address":
                        if (args.Parameters.Count > 1)
                        {
                            SetAddress(args.Player, args.Parameters[1]);
                            break;
                        }
                        RemovePlayer(args.Player);
                        break;
                    case "players":
                        if (args.Parameters.Count != 2)
                        {
                            DisplayHelp(args.Player, 1);
                            break;
                        }
                        PlayerSetting(args.Player, args.Parameters[1]);
                        break;
                    case "admins":
                        if (args.Parameters.Count != 2)
                        {
                            DisplayHelp(args.Player, 1);
                            break;
                        }
                        AdminSetting(args.Player, args.Parameters[1]);
                        break;
                    case "eblast":
                        if (args.Parameters.Count != 2)
                        {
                            DisplayHelp(args.Player, 1);
                            break;
                        }
                        EblastSetting(args.Player, args.Parameters[1]);
                        break;
                    case "reply":
                        if (args.Parameters.Count != 2)
                        {
                            DisplayHelp(args.Player, 2);
                            break;
                        }
                        ReplySetting(args.Player, args.Parameters[1]);
                        break;
                    case "notify":
                        if (args.Parameters.Count != 2)
                        {
                            DisplayHelp(args.Player, 2);
                            break;
                        }
                        NotifySetting(args.Player, args.Parameters[1]);
                        break;
                    case "onjoin":
                        if (args.Parameters.Count != 2)
                        {
                            DisplayHelp(args.Player, 1);
                            break;
                        }
                        SetOnJoin(args.Player, args.Parameters[1]);
                        break;
                    case "onjoinlist":
                        if (args.Parameters.Count == 1 )
                        {
                            OnJoinList(args.Player, "1");
                        }
                        if (args.Parameters.Count == 2)
                        {
                            OnJoinList(args.Player, args.Parameters[1]);
                        }
                        DisplayHelp(args.Player, 2);
                        break;
                    case "remove":
                        if (args.Parameters.Count != 2)
                        {
                            DisplayHelp(args.Player, 2);
                            break;
                        }
                        RemoveOnJoin(args.Player, args.Parameters[1]);
                        break;
                    case "testemail":
                        if (args.Parameters.Count != 2)
                        {
                            DisplayHelp(args.Player, 1);
                            break;
                        }
                        SendTestEmail(args.Parameters[1]);
                        break;
                    case "help":
                        DisplayHelp(args.Player, 0);
                        break;
                    case "help1":
                        DisplayHelp(args.Player, 1);
                        break;
                    case "help2":
                        DisplayHelp(args.Player, 2);
                        break;
                    default:
                        if (TSESql.GetPlayerIndex(args.Player.Name) != -1)
                        {
                            if (args.Parameters.Count == 2)
                            {
                                SendEmail(args.Player, args.Parameters[0], args.Parameters[1]);
                                break;
                            }
                            DisplayHelp(args.Player, 0);
                            break;
                        }
                        NotRegistered(args.Player);
                        break;
                }
            }
        }

        private void OnGreetPlayer(int who, HandledEventArgs args)
        {
            TSPlayer player = TShock.Players[who];
            int pIndex = TSESql.GetPlayerIndex(player.Name);
            //Log.Info(player.Name + " joined the server with TSE index of:" + pIndex.ToString());

            //Checks for OnJoin email sending
            if (pIndex == -1)
            {
                player.SendMessage(Main.worldName + " is equiped with TSEmailer.", Color.Plum);
                player.SendMessage("Terraria Server Email System!", Color.Plum);
                player.SendMessage("More Details: /email help", Color.Plum);
                return;
            }
            player.SendMessage("TSEmailer - More Details: /email help", Color.Plum);
            if (!TSESql.GetAllowNotify(pIndex))
            {
                player.SendMessage("TSEmailer: Notifications will not be sent per your settings...", Color.Blue);
                return;
            }
            /*
             * #### Insert timer or sleep code HERE! ####
             player.SendMessage("TSEmailer: notifications will be sent in " + TSEConfig.jointimer.ToString() + " seconds...", Color.Plum);
            */

            SendOnJoinEmails(player);
        }

        private void SetAddress(TSPlayer player, string address)
        {
            if (player != null)
            {
                Log.Info("Setting player: " + player.Name + " Address: " + address);
                player.SendMessage(TSESql.SetPlayerAddress(player, address), Color.Blue);
            }
        }

        private void RemovePlayer(TSPlayer player)
        {
            if (player != null)
            {
                Log.Info("Removing player: " + player.Name);
                player.SendMessage(TSESql.RemoveAddress(player), Color.Blue);
            }
        }

        private void PlayerSetting(TSPlayer player, string stValue)
        {
            if (player != null)
            {
                bool value;
                if (stValue.ToUpper().Contains("TRUE"))
                {
                    value = true;
                }
                else if (stValue.ToUpper().Contains("FALSE"))
                {
                    value = false;
                }
                else
                {
                    DisplayHelp(player, 1);
                    return;
                }
            
                int index = TSESql.GetPlayerIndex(player.Name);
                if (index == -1)
                {
                    NotRegistered(player);
                    return;
                }
                TSESql.SetAllowPlayers(index, value);
                player.SendMessage("Settings updated!", Color.Red);
                player.SendMessage("Players can email you: " + TSESql.GetAllowPlayers(TSESql.GetPlayerIndex(player.Name)), Color.Blue);
            }
        }

        private void AdminSetting(TSPlayer player, string stValue)
        {
            if (player != null)
            {
                bool value;
                if (stValue.ToUpper().Contains("TRUE"))
                {
                    value = true;
                }
                else if (stValue.ToUpper().Contains("FALSE"))
                {
                    value = false;
                }
                else
                {
                    DisplayHelp(player, 1);
                    return;
                }
            
                int index = TSESql.GetPlayerIndex(player.Name);
                if (index == -1)
                {
                    NotRegistered(player);
                    return;
                }
                TSESql.SetAllowAdmins(index, value);
                player.SendMessage("Settings updated!", Color.Red);
                player.SendMessage("Admins can email you: " + TSESql.GetAllowAdmins(TSESql.GetPlayerIndex(player.Name)), Color.Blue);
            }
        }

        private void EblastSetting(TSPlayer player, string stValue)
        {
            if (player != null)
            {
                bool value;
                if (stValue.ToUpper().Contains("TRUE"))
                {
                    value = true;
                }
                else if (stValue.ToUpper().Contains("FALSE"))
                {
                    value = false;
                }
                else
                {
                    DisplayHelp(player, 1);
                    return;
                }
            
                int index = TSESql.GetPlayerIndex(player.Name);
                if (index == -1)
                {
                    NotRegistered(player);
                    return;
                }
                TSESql.SetAllowEblast(index, value);
                player.SendMessage("Settings updated!", Color.Red);
                player.SendMessage("You can receive email blasts: " + TSESql.GetAllowEblast(TSESql.GetPlayerIndex(player.Name)), Color.Blue);
            }
        }

        private void ReplySetting(TSPlayer player, string stValue)
        {
            if (player != null)
            {
                bool value;
                if (stValue.ToUpper().Contains("TRUE"))
                {
                    value = true;
                }
                else if (stValue.ToUpper().Contains("FALSE"))
                {
                    value = false;
                }
                else
                {
                    DisplayHelp(player, 2);
                    return;
                }
            
                int index = TSESql.GetPlayerIndex(player.Name);
                if (index == -1)
                {
                    NotRegistered(player);
                    return;
                }
                TSESql.SetAllowReply(index, value);
                player.SendMessage("Settings updated!", Color.Red);
                player.SendMessage("Players can reply directly to your email: " + TSESql.GetAllowReply(TSESql.GetPlayerIndex(player.Name)), Color.Blue);
            }
        }

        private void NotifySetting(TSPlayer player, string stValue)
        {
            if (player != null)
            {
                bool value;
                if (stValue.ToUpper().Contains("TRUE"))
                {
                    value = true;
                }
                else if (stValue.ToUpper().Contains("FALSE"))
                {
                    value = false;
                }
                else
                {
                    DisplayHelp(player, 2);
                    return;
                }
            
                int index = TSESql.GetPlayerIndex(player.Name);
                if (index == -1)
                {
                    NotRegistered(player);
                    return;
                }
                TSESql.SetAllowNotify(index, value);
                player.SendMessage("Settings updated!", Color.Red);
                player.SendMessage("Allow others to be notified when you join the server: " + TSESql.GetAllowNotify(TSESql.GetPlayerIndex(player.Name)), Color.Blue);
            }
        }

        private void SendEmail(TSPlayer player, string rcptPlayer, string msgBody)
        {
            Color ErrColor = Color.Plum;
            int pIndex = TSESql.GetPlayerIndex(player.Name);
            int rcptIndex = TSESql.GetPlayerIndex(rcptPlayer);
            Log.Info("SendEmail - Checking sender and recipient are registered");
            if (pIndex == -1)
            {
                NotRegistered(player);
                return;
            }
            if (rcptIndex == -1)
            {
                NotRegistered(player, rcptPlayer);
                return;
            }

            if (EmailerHasAuthority(pIndex, rcptIndex))
            {
                Log.Info("SendEmail - Building email message");
                string Subject = "Terraria Server - " + Main.worldName + ":" + player.Name + " - TSEmailer";
                MailAddressCollection recipient = new MailAddressCollection();
                recipient.Add(TSESql.GetPlayerEmail(rcptIndex));

                StringBuilder body = new StringBuilder();
                body.AppendLine("Terraria player: " + player.Name + " sent you the following message:");
                body.AppendLine();
                body.AppendLine(msgBody);
                body.AppendLine();
                body.AppendLine();
                body.AppendLine(player.Name + " is digging in " + Main.worldName + "...");
                body.AppendLine("Message generated by TSEmailer - The Terraria Server Email System!");
                body.AppendLine("Email sent: " + DateTime.Now.ToString());
                TSEsender.SendEmail(
                    new MailAddress(TSEConfig.smtpaddress, TSEConfig.sendas),
                    recipient,
                    new MailAddressCollection(),
                    new MailAddressCollection(),
                    Subject,
                    body.ToString());
                Log.Info("SendEmail - Email message sent");
                return;
            }
            player.SendMessage("You are not authorized to send email to " + rcptPlayer + ".", Color.Red);
        }

        private void SetOnJoin(TSPlayer player, string PlayerName)
        {
            if (player != null)
            {
                int curIndex = TSESql.GetPlayerIndex(player.Name);
                int plIndex = TSESql.GetPlayerIndex(PlayerName);
                //check curPlayerID has email address set
                if (curIndex == -1)
                {
                    NotRegistered(player);
                    return;
                }
                //check PlayerName has email address registered
                if (plIndex == -1)
                {
                    NotRegistered(player, PlayerName);
                    return;
                }
                TSESql.SetOnJoinAddress(curIndex, player.Name, plIndex);
                if (!TSESql.GetAllowNotify(plIndex))
                {
                    player.SendMessage(PlayerName + " does not allow you to be notified.", Color.Red);
                    return;
                }
                player.SendMessage("You will be notified when " + PlayerName + " joins the server.", Color.Blue);
                
            }
        }

        private void RemoveOnJoin(TSPlayer player, string PlayerName)
        {
            if (player != null)
            {
                int curIndex = TSESql.GetPlayerIndex(player.Name);
                int plIndex = TSESql.GetPlayerIndex(PlayerName);
                //check curPlayerID has email address set
                if (curIndex == -1)
                {
                    NotRegistered(player);
                    return;
                }
                //check PlayerName has email address registered
                if (plIndex == -1)
                {
                    NotRegistered(player, PlayerName);
                    return;
                }
                
                player.SendMessage("You will no longer be notified when " + PlayerName + " joins the server.", Color.Red);    
            }
        }

        private void OnJoinList(TSPlayer player, string strPage)
        {
            int index = TSESql.GetPlayerIndex(player.Name);
            if (index == -1)
            {
                NotRegistered(player);
                return;
            }

            int page;
            if (!(Int32.TryParse(strPage, out page)))
            {
                page = 1;
            }

            Log.Info("TSEmailer.OnJoinList - Page requested:" + page.ToString());

            // List players
            List<String> PlayerList = TSESql.GetOnJoinList(index);
            Log.Info("TSEmailer.OnJoinList - PlayerList count:" + PlayerList.Count);

            if (PlayerList.Count < 1)
            {
                player.SendMessage("You will not be sent any emails when players join the server.");
                return;
            }

            int pageCnt = PlayerList.Count / 4;
            if ((PlayerList.Count % 4) > 0)
            {
                Log.Info("TSEmailer.OnJoinList - Page count +1");
                pageCnt++;
            }

            if (page > pageCnt)
            {
                Log.Info("TSEmailer.OnJoinList - Page specified is greater than actual page count");
                page = 1;
            }

            player.SendMessage("Email messages will be sent when these players join the server:");

            int NameIndex;
            for (int i = 0; i < 4; i++)
            {
                NameIndex = ((page - 1) * 4) + i;
                Log.Info("TSEmailer.OnJoinList - NameIndex:" + NameIndex.ToString());

                if(!(NameIndex >= PlayerList.Count))
                {
                    Log.Info("TSEmailer.OnJoinList - Player to send messages about: " + PlayerList[NameIndex]);
                    player.SendMessage((NameIndex + 1).ToString() + ".) " + PlayerList[NameIndex]);
                }
            }
            player.SendMessage("Page " + page.ToString() + " of " + pageCnt.ToString() + " Enter: '/email onjoinlist <page number>' for more...");
        }

        private void SendOnJoinEmails(TSPlayer player)
        {
            int pIndex = TSESql.GetPlayerIndex(player.Name);
            MailAddressCollection bccAddresses = TSESql.GetOnJoinEmails(TSESql.GetTSEPlayerID(pIndex));
            if (bccAddresses.Count > 0)
            {
                StringBuilder body = new StringBuilder();
                body.AppendLine("Terraria player: " + player.Name + " has joined the server and is digging in " + Main.worldName + "...");
                body.AppendLine();
                body.AppendLine("Message generated by TSEmailer - The Terraria Server Email System!");
                body.AppendLine("Email sent: " + DateTime.Now.ToString());

                Log.Info("Notifying " + bccAddresses.Count.ToString() + " player(s) that " + player.Name + " has joined the server.");
                TSEsender.SendEmail(
                    new MailAddress(TSEConfig.smtpaddress, TSEConfig.sendas),
                    new MailAddressCollection(),
                    new MailAddressCollection(),
                    bccAddresses,
                    "Terraria Server - " + Main.worldName + " - TSEmailer",
                    body.ToString());
            }
        }

        private bool EmailerHasAuthority(int SenderIndex, int ReceptIndex)
        {
            bool auth = false;
            

            return auth;
        }

        private void GetStatus(TSPlayer player)
        {
            if (player != null)
            {
                Color ErrColor = Color.Blue;
                int index = TSESql.GetPlayerIndex(player.Name);
                if (index == -1)
                {
                    NotRegistered(player);
                    return;
                }
                List<SqlValue> settings = TSESql.GetSettings(index);
                player.SendMessage("Your email address: " + settings[1].Value.ToString(), ErrColor);
                player.SendMessage("Players can email you: " + settings[2].Value.ToString(), ErrColor);
                player.SendMessage("Admins can email you: " + settings[3].Value.ToString(), ErrColor);
                player.SendMessage("You can receive email blasts: " + settings[4].Value.ToString(), ErrColor);
                player.SendMessage("Players can reply directly to your email: " + settings[5].Value.ToString(), ErrColor);
                player.SendMessage("Allow others to be notified when you join the server: " + settings[6].Value.ToString(), ErrColor);
            }
        }

        public static void DisplayHelp(TSPlayer player, int section)
        {
            Color ErrColor = Color.Plum;
            
            switch (section)
            {
                case 1:
                    player.SendMessage("Email Commands:", ErrColor);
                    player.SendMessage("  /email settings - Displays your current settings.", ErrColor);
                    player.SendMessage("  /email address <youraddress@yourdomain.com> - Sets your email address.", ErrColor);
                    player.SendMessage("  /email players [true|false] - Allow/Disallow other players to email directly.", ErrColor);
                    player.SendMessage("  /email admins [true|false] - Receive email from admins.", ErrColor);
                    player.SendMessage("  /email eblast [true|false] - Receive email directed to all registered users.", ErrColor);
                    break;
                case 2:
                    player.SendMessage("Email Commands:", ErrColor);
                    player.SendMessage("  /email reply [true|false] - Users can reply to your email address.", ErrColor);
                    player.SendMessage("  /email notify [true|false] - Others to be notified when you join the server.", ErrColor);
                    player.SendMessage("  /email onjoin <user> - Receive an email when <user> has joined the server.", ErrColor);
                    player.SendMessage("  /email onjoinlist - List of users names that you receive notifications.", ErrColor);
                    player.SendMessage("  /email remove <user> - Remove user from your notification list.", ErrColor);
                    break;
                default:
                    player.SendMessage("example: /email <player> <\"Email message body\">", ErrColor);
                    player.SendMessage("Email Commands:", ErrColor);
                    player.SendMessage("  /email address <youraddress@yourdomain.com> - Sets your email address.", ErrColor);
                    player.SendMessage("  /email help1 - Displays more help.", ErrColor);
                    player.SendMessage("  /email help2 - Displays even more help.", ErrColor);
                    break;
            }
        }

        public static void NotRegistered(TSPlayer player, string PlayerName)
        {
            player.SendMessage("Player, " + PlayerName + " has not registered with the server!", Color.Red);
        }

        public static void NotRegistered(TSPlayer player)
        {
            player.SendMessage("You have not registered with the server!", Color.Red);
            player.SendMessage("To Register: /email address <youremail@yourdomain.com>", Color.Red);
        }

        public static void SendTestEmail(string recipient)
        {
            Log.Info("Preparing email test message to: " + recipient);
            try
            {
                TSEsender.SendEmail(
                    TSEConfig.smtpaddress,
                    recipient,
                    "TSEmailer - Test Email",
                    DateTime.Now.ToString() + " - Email is working...");
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
