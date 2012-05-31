using System;
using TShockAPI;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using Community.CsharpSqlite;
using Community.CsharpSqlite.SQLiteClient;
using System.Data;
using System.Collections.Generic;
using System.Net.Mail;

namespace sqlProvider
{
    public class TSdb
    {
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        public static SqlTable TSEtable;

        public TSdb()
        {
            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            TSEtable = new SqlTable("TSEmailer",
                new SqlColumn("TSEindex", MySqlDbType.Int32) { AutoIncrement = true, Primary = true, Unique = true, NotNull = true },
                new SqlColumn("PlayerID", MySqlDbType.Int32) { Unique = true, NotNull = true },
                new SqlColumn("PlayerName", MySqlDbType.Text) { Length = 250, NotNull = true },
                new SqlColumn("RecordType", MySqlDbType.Int32) { NotNull = true }, //RecordTypes: ['1' = Player Settings] ['2' = OnJoin notifing address])
                new SqlColumn("address", MySqlDbType.Text) { Length = 250 },
                new SqlColumn("players", MySqlDbType.Text) { Length = 10, DefaultValue = "true" },
                new SqlColumn("admins", MySqlDbType.Text) { Length = 10, DefaultValue = "true" },
                new SqlColumn("eblast", MySqlDbType.Text) { Length = 10, DefaultValue = "true" },
                new SqlColumn("reply", MySqlDbType.Text) { Length = 10, DefaultValue = "true" },
                new SqlColumn("notify", MySqlDbType.Text) { Length = 10, DefaultValue = "true" }
                );
            SetupDBTable();
        }

        public MailAddressCollection GetOnJoinEmails(int PlayerID)
        {
            //Log.Info("Getting list of onjoin emails matching PlayerID:" + PlayerID);
            List<SqlValue> wheres = new List<SqlValue>{
                    NewSQLV("PlayerID", PlayerID),
                    NewSQLV("RecordType",2)};
            List<Object> addresses = SQLEditor.ReadColumn(TSEtable.Name, "address", wheres);
            List<Object> names = SQLEditor.ReadColumn(TSEtable.Name, "PlayerName", wheres);
            if (addresses.Count > 0)
            {
                MailAddressCollection thisList = new MailAddressCollection();
                for (int i = 0; i < addresses.Count; i++)
                {
                    thisList.Add(new MailAddress(addresses[i].ToString(), names[i].ToString()));
                }
                return thisList;
            }
                return new MailAddressCollection();
        }

        public List<SqlValue> GetSettings(int PlayerIndex)
        {
            List<SqlValue> wheres = new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex), NewSQLV("RecordType", 1) };
            List<SqlValue> settings = new List<SqlValue>();
            settings.Add(NewSQLV("PlayerName", SQLEditor.ReadColumn(TSEtable.Name, "PlayerName", wheres)[0].ToString()));
            settings.Add(NewSQLV("address", SQLEditor.ReadColumn(TSEtable.Name, "address", wheres)[0].ToString()));
            settings.Add(NewSQLV("players", SQLEditor.ReadColumn(TSEtable.Name, "players", wheres)[0].ToString()));
            settings.Add(NewSQLV("admins", SQLEditor.ReadColumn(TSEtable.Name, "admins", wheres)[0].ToString()));
            settings.Add(NewSQLV("eblast", SQLEditor.ReadColumn(TSEtable.Name, "eblast", wheres)[0].ToString()));
            settings.Add(NewSQLV("reply", SQLEditor.ReadColumn(TSEtable.Name, "reply", wheres)[0].ToString()));
            settings.Add(NewSQLV("notify", SQLEditor.ReadColumn(TSEtable.Name, "notify", wheres)[0].ToString()));
            return settings;
        }

        public List<SqlValue> GetSettings(string PlayerName)
        {
            return GetSettings(GetPlayerIndex(PlayerName));
        }

        public List<String> GetOnJoinList(int PlayerIndex)
        {
            // Retrieve list of player names
            List<String> PlayerList = new List<String>();

            //Log.Info("TSdb.GetOnJoinList - Preparing to get playerID");
            string playerID = SQLEditor.ReadColumn(TSEtable.Name, "PlayerID", new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex), NewSQLV("RecordType", 1) })[0].ToString();

            //Log.Info("TSdb.GetOnJoinList - playerID= '" + playerID + "' . Preparing to get lstValues");
            List<Object> LstValues = SQLEditor.ReadColumn(TSEtable.Name, "PlayerName", new List<SqlValue> { NewSQLV("PlayerID", playerID), NewSQLV("RecordType", 2) });

            //Log.Info("TSdb.GetOnJoinList - Found count:" + LstValues.Count.ToString() + " LstValues. Loading values into PlayerList");
            foreach (object value in LstValues)
            {
                //Log.Info("TSdb.GetOnJoinList - PlayerList adding: " + value.ToString());
                PlayerList.Add(value.ToString());
            }
            return PlayerList;
        }

        public List<String> GetOnJoinList(string PlayerName)
        {
            return GetOnJoinList(GetPlayerIndex(PlayerName));
        }

        public bool GetAllowPlayers(int PlayerIndex)
        {
            return Boolean.Parse(SQLEditor.ReadColumn(TSEtable.Name, "players",
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) })[0].ToString());
        }

        public void SetAllowPlayers(int PlayerIndex, bool value)
        {
            SQLEditor.UpdateValues(TSEtable.Name,
                new List<SqlValue> { NewSQLV("players", value) },
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) });
        }

        public bool GetAllowAdmins(int PlayerIndex)
        {
            return Boolean.Parse(SQLEditor.ReadColumn(TSEtable.Name, "admins", 
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) })[0].ToString());
        }

        public void SetAllowAdmins(int PlayerIndex, bool value)
        {
            SQLEditor.UpdateValues(TSEtable.Name,
                new List<SqlValue> { NewSQLV("admins", value) },
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) });
        }

        public bool GetAllowEblast(int PlayerIndex)
        {
            return Boolean.Parse(SQLEditor.ReadColumn(TSEtable.Name, "eblast", 
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) })[0].ToString());
        }

        public void SetAllowEblast(int PlayerIndex, bool value)
        {
            SQLEditor.UpdateValues(TSEtable.Name,
                new List<SqlValue> { NewSQLV("eblast", value) },
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) });
        }

        public bool GetAllowReply(int PlayerIndex)
        {
            return Boolean.Parse(SQLEditor.ReadColumn(TSEtable.Name, "reply",
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) })[0].ToString());
        }

        public void SetAllowReply(int PlayerIndex, bool value)
        {
            SQLEditor.UpdateValues(TSEtable.Name,
                new List<SqlValue> { NewSQLV("reply", value) },
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) });
        }

        public bool GetAllowNotify(int PlayerIndex)
        {
            return Boolean.Parse(SQLEditor.ReadColumn(TSEtable.Name, "notify",
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) })[0].ToString());
        }

        public void SetAllowNotify(int PlayerIndex, bool value)
        {
            SQLEditor.UpdateValues(TSEtable.Name,
                new List<SqlValue> { NewSQLV("notify", value) },
                new List<SqlValue> { NewSQLV("TSEindex", PlayerIndex) });
        }

        public MailAddress GetPlayerEmail(int index)
        {
            return new MailAddress(
                SQLEditor.ReadColumn(TSEtable.Name, "address", new List<SqlValue> { NewSQLV("TSEindex", index) })[0].ToString(),
                SQLEditor.ReadColumn(TSEtable.Name, "PlayerName", new List<SqlValue> { NewSQLV("TSEindex", index) })[0].ToString()
                );
        }

        public void SetOnJoinAddress(int curPlayerIndex, string curPlayerName, int onjPlayerIndex)
        {
            string curAddress = GetPlayerEmail(curPlayerIndex).Address;

            string onjPlayerID = SQLEditor.ReadColumn(TSEtable.Name, "PlayerID",
                new List<SqlValue> { NewSQLV("TSEindex", onjPlayerIndex) })[0].ToString();
            //set OnJoin address as RecordType = 2
            SQLEditor.InsertValues(TSEtable.Name, new List<SqlValue>{
                NewSQLV("RecordType",2),
                NewSQLV("PlayerID",onjPlayerID),
                NewSQLV("PlayerName",curPlayerName),
                NewSQLV("address",curAddress)});
        }

        public bool RemoveOnJoinAddress(int curPlayerIndex, int onjPlayerIndex)
        {
            string onjPlayerID = SQLEditor.ReadColumn(TSEtable.Name, "PlayerID",
                new List<SqlValue> { NewSQLV("TSEindex", onjPlayerIndex) })[0].ToString();

            string curPlayerAddress = GetPlayerEmail(curPlayerIndex).Address;

            List<SqlValue> where = new List<SqlValue> { NewSQLV("PlayerID", onjPlayerID), NewSQLV("RecordType", 2), NewSQLV("Address", curPlayerAddress) };
            if (SQLEditor.ReadColumn(TSEtable.Name, "TSEindex", where).Count > 0)
            {
                SQLWriter.DeleteRow(TSEtable.Name, where);
                return true;
            }
            return false;
        }

        public string SetPlayerAddress(TSPlayer player, string address)
        {
            //Get the index of the players account settings
            Int32 plIndex = GetPlayerIndex(player.Name);

            if (plIndex != -1)
            {
                SQLEditor.UpdateValues(TSEtable.Name, new List<SqlValue> { 
                    NewSQLV("Address", address), NewSQLV("PlayerName", player.Name) },
                    new List<SqlValue> { NewSQLV("TSEindex", plIndex) });
                return "Your email address has been updated.";
            }
            else
            {
                int NewPlayerID = GetNextTSEPlayerID();
                
                List<SqlValue> values = new List<SqlValue>{
                    NewSQLV("PlayerID", NewPlayerID),
                    NewSQLV("PlayerName", player.Name),
                    NewSQLV("RecordType",1),
                    NewSQLV("Address", address),
                    NewSQLV("players",true),
                    NewSQLV("admins",true),
                    NewSQLV("eblast",true),
                    NewSQLV("reply",true),
                    NewSQLV("notify",true)
                };
                SQLEditor.InsertValues(TSEtable.Name, values);
                return "Your email address is now registered with the server.";
            }
        }

        public string RemoveAddress(TSPlayer player)
        {
            Int32 index = GetPlayerIndex(player.Name);
            if (index != -1)
            {
                int PlayerID = GetTSEPlayerID(index);
                SQLWriter.DeleteRow(TSEtable.Name, new List<SqlValue> { NewSQLV("PlayerID", PlayerID) });
                return "Your account settings have been removed from the server!";
            }
            return "Your account does not exist on the server!";
        }

        public Int32 GetPlayerIndex(string PlayerName)
        {
            List<SqlValue> values = new List<SqlValue>(){
                NewSQLV("PlayerName", PlayerName),
                NewSQLV("RecordType",1)
                 };
            List<Object> results = SQLEditor.ReadColumn(TSEtable.Name, "TSEindex", values);
            
            if (results.Count > 0)
            {
                return Int32.Parse(results[0].ToString());
            }
            // Return -1 if the player is not registered
            return -1;
        }

        public Int32 GetTSEPlayerID(int PlayerIndex)
        {
            List<SqlValue> values = new List<SqlValue>(){
                    NewSQLV("TSEindex", PlayerIndex),
                    NewSQLV("RecordType",1)
                     };
            List<Object> results = SQLEditor.ReadColumn(TSEtable.Name, "PlayerID", values);
            if (results.Count > 0)
            {
                return Int32.Parse(results[0].ToString());
            }
            // Return -1 if the player is not registered
            return -1;
        }

        

        public Int32 GetNextTSEPlayerID()
        {
            List<SqlValue> wheres = new List<SqlValue>(){
                NewSQLV("RecordType",1)
                 };
            List<Object> results = SQLEditor.ReadColumn(TSEtable.Name, "PlayerID", wheres);

            if (results.Count > 0)
            {
                return Int32.Parse(results[results.Count - 1].ToString()) + 1;
            }
            return 1;
        }

        public DataTable GetDataTable(List<SqlValue> wheres)
        {
            DataTable table = new DataTable();
            for (int i = 0; i < TSEtable.Columns.Count; i++)
            {
                table.Columns.Add(TSEtable.Columns[i].Name.ToString(), TSEtable.Columns[i].Type.GetType());

                List<object> colValues = SQLEditor.ReadColumn(TSEtable.Name.ToString(), TSEtable.Columns[i].Name.ToString(), wheres);

                for (int x = 0; x < colValues.Count; x++)
                {
                    while (table.Rows.Count - 1 < x)
                    {
                        table.NewRow();
                    }
                    table.Rows[x].SetField(i, colValues[x]);
                }
            }

            return table;
        }

        public DataTable GetDataTable(List<String> columns, List<SqlValue> wheres)
        {
            DataTable table = new DataTable();
            for (int i = 0; i < columns.Count; i++)
            {
                int j = TSEtable.Columns.FindIndex((e)=> {return (e.Name == columns[i]);});
                
                table.Columns.Add(TSEtable.Columns[j].Name, TSEtable.Columns[j].Type.GetType());

                List<object> colValues = SQLEditor.ReadColumn(TSEtable.Name, TSEtable.Columns[j].Name, wheres);

                for (int x = 0; x < colValues.Count; x++)
                {
                    while (table.Rows.Count - 1 < x)
                    {
                        table.NewRow();
                    }
                    table.Rows[x].SetField(i, colValues[x]);
                }
            }

            return table;
        }

        public SqlValue NewSQLV(string Column, string Value)
        {
            return new SqlValue(Column, "'" + Value + "'");
        }

        public SqlValue NewSQLV(string Column, int Value)
        {
            return new SqlValue(Column, Value);
        }

        public SqlValue NewSQLV(string Column, bool Value)
        {
            string ValueBol = "False";
            if (Value)
            {
                ValueBol = "True";
            }
            return new SqlValue(Column, "'" + ValueBol + "'");
        }

        public SqlValue NewSQLV(string Column, object Value)
        {
            return new SqlValue(Column, "'" + Value + "'");
        }
        
        public static void SetupDBTable()
        {
            try
            {
                SQLWriter.EnsureExists(TSEtable);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("TSEmailer - Error with DB table setup");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("TSEmailer - DB Exception");
                Log.Error(ex.ToString());
            }
        }
    }
}