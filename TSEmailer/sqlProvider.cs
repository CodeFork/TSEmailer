using System;
using TShockAPI;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using Community.CsharpSqlite;
using Community.CsharpSqlite.SQLiteClient;
using System.Data;
using System.Collections.Generic;

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
                new SqlColumn("notify", MySqlDbType.Text) { Length = 10, DefaultValue = "true" },
                new SqlColumn("onjoin", MySqlDbType.Text) { Length = 250 }
                );
            SetupDBTable();
        }

        public DataTable GetOnJoinEmails(int PlayerID)
        {
            if (GetPlayerIndex(PlayerID) != -1)
            {
                List<String> columns = new List<String>{"PlayerName","address"};
                List<SqlValue> wheres = new List<SqlValue>{
                    new SqlValue("PlayerID", PlayerID),
                    new SqlValue("RecordType",2)};
                return GetDataTable(columns, wheres);
            }
            return new DataTable();
        }

        public List<SqlValue> GetSettings(int PlayerIndex)
        {
            List<SqlValue> settings = new List<SqlValue>();
            settings.Add(new SqlValue("PlayerName", SQLEditor.ReadColumn(TSEtable.Name, "PlayerName", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0]));
            settings.Add(new SqlValue("address", SQLEditor.ReadColumn(TSEtable.Name, "address", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0]));
            settings.Add(new SqlValue("players", SQLEditor.ReadColumn(TSEtable.Name, "players", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0]));
            settings.Add(new SqlValue("admins", SQLEditor.ReadColumn(TSEtable.Name, "admins", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0]));
            settings.Add(new SqlValue("eblast", SQLEditor.ReadColumn(TSEtable.Name, "eblast", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0]));
            settings.Add(new SqlValue("reply", SQLEditor.ReadColumn(TSEtable.Name, "reply", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0]));
            settings.Add(new SqlValue("notify", SQLEditor.ReadColumn(TSEtable.Name, "notify", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0]));
            return settings;
        }

        public List<SqlValue> GetSettings(string PlayerName)
        {
            List<SqlValue> settings = new List<SqlValue>();
            settings.Add(new SqlValue("PlayerName", SQLEditor.ReadColumn(TSEtable.Name, "PlayerName", new List<SqlValue> { new SqlValue("PlayerName", PlayerName) })[0]));
            settings.Add(new SqlValue("address", SQLEditor.ReadColumn(TSEtable.Name, "address", new List<SqlValue> { new SqlValue("PlayerName", PlayerName) })[0]));
            settings.Add(new SqlValue("players", SQLEditor.ReadColumn(TSEtable.Name, "players", new List<SqlValue> { new SqlValue("PlayerName", PlayerName) })[0]));
            settings.Add(new SqlValue("admins", SQLEditor.ReadColumn(TSEtable.Name, "admins", new List<SqlValue> { new SqlValue("PlayerName", PlayerName) })[0]));
            settings.Add(new SqlValue("eblast", SQLEditor.ReadColumn(TSEtable.Name, "eblast", new List<SqlValue> { new SqlValue("PlayerName", PlayerName) })[0]));
            settings.Add(new SqlValue("reply", SQLEditor.ReadColumn(TSEtable.Name, "reply", new List<SqlValue> { new SqlValue("PlayerName", PlayerName) })[0]));
            settings.Add(new SqlValue("notify", SQLEditor.ReadColumn(TSEtable.Name, "notify", new List<SqlValue> { new SqlValue("PlayerName", PlayerName) })[0]));
            return settings;
        }

        public bool GetAllowPlayers(int PlayerIndex)
        {
            return Boolean.Parse(SQLEditor.ReadColumn(TSEtable.Name, "players", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0].ToString());
        }

        public bool GetAllowAdmins(int PlayerIndex)
        {
            return Boolean.Parse(SQLEditor.ReadColumn(TSEtable.Name, "admins", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0].ToString());
        }

        public bool GetAllowEblast(int PlayerIndex)
        {
            return Boolean.Parse(SQLEditor.ReadColumn(TSEtable.Name, "eblast", new List<SqlValue> { new SqlValue("TSEindex", PlayerIndex) })[0].ToString());
        }

        public List<SqlValue> GetPlayerEmail(int index)
        {
            List<SqlValue> settings = new List<SqlValue>();
            settings.Add(new SqlValue("PlayerName", SQLEditor.ReadColumn(TSEtable.Name, "PlayerName", new List<SqlValue> { new SqlValue("TSEindex", index) })[0]));
            settings.Add(new SqlValue("address", SQLEditor.ReadColumn(TSEtable.Name, "address", new List<SqlValue> { new SqlValue("TSEindex", index) })[0]));

            return settings;
        }

        public string SetOnJoinAddress(int curPlayerID, string PlayerName)
        {
            //check curPlayerID has email address set
            Int32 curIndex = GetPlayerIndex(curPlayerID);
            if (curIndex != -1)
            {
                //check PlayerName has email address registered
                Int32 plIndex = GetPlayerIndex(PlayerName);
                if (plIndex != -1)
                {
                    //set OnJoin address as RecordType = 2
                    string address = SQLEditor.ReadColumn(TSEtable.Name, "address", new List<SqlValue> { new SqlValue("TSEindex", plIndex) })[0].ToString();
                    SQLEditor.InsertValues(TSEtable.Name, new List<SqlValue>{
                        new SqlValue("RecordType",2),
                        new SqlValue("PlayerID",curPlayerID),
                        new SqlValue("PlayerName",PlayerName),
                        new SqlValue("address",address)});
                    return "You will now receive an email when " + PlayerName + " joins the server.";
                }
                return PlayerName + "has not registered an email address with the server.";
            }
            return "Please register your email address: '/email address <yourname@yourdomain.com>'";
        }

        public string SetPlayerAddress(TSPlayer player, string address)
        {
            //Get the index of the players account settings
            Int32 plIndex = GetPlayerIndex(player.UserID);

            if (plIndex != -1)
            {
                SQLEditor.UpdateValues(TSEtable.Name, new List<SqlValue> { 
                    new SqlValue("Address", address), new SqlValue("PlayerName", player.Name) },
                    new List<SqlValue> { new SqlValue("TSEindex", plIndex) });
                return "Your email address has been updated.";
            }
            else
            {
                List<SqlValue> values = new List<SqlValue>{
                    new SqlValue("PlayerID", player.UserID),
                    new SqlValue("PlayerName", player.Name),
                    new SqlValue("RecordType",1),
                    new SqlValue("Address", address)};
                SQLEditor.InsertValues(TSEtable.Name, values);
                return "Your email address is now registered with the server.";
            }
        }

        public string RemoveAddress(TSPlayer player)
        {
            Int32 index = GetPlayerIndex(player.UserID);
            if (index != -1)
            {
                SQLWriter.DeleteRow(TSEtable.Name, new List<SqlValue> { new SqlValue("TSEindex", index) });
                return "Your account settings have been removed from the server!";
            }
            return "Your account does not exist on the server!";
        }

        public Int32 GetPlayerIndex(string PlayerName)
        {
            List<SqlValue> values = new List<SqlValue>(){
                new SqlValue("PlayerName", PlayerName),
                new SqlValue("RecordType",1)
                 };
            List<Object> results = SQLEditor.ReadColumn(TSEtable.Name, "TSEindex", values);
            if (results.Count > 0)
            {
                return Int32.Parse(results[0].ToString());
            }
            // Return -1 if the player is not registered
            return -1;
        }

        public Int32 GetPlayerIndex(int playerID)
        {
            List<SqlValue> values = new List<SqlValue>(){
                new SqlValue("PlayerID", playerID),
                new SqlValue("RecordType",1)
                 };
            List<Object> results = SQLEditor.ReadColumn(TSEtable.Name, "TSEindex", values);
            if (results.Count > 0)
            {
                return Int32.Parse(results[0].ToString());
            }
            // Return -1 if the player is not registered
            return -1;
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
                table.Columns.Add(TSEtable.Columns[j].Name.ToString(), TSEtable.Columns[j].Type.GetType());

                List<object> colValues = SQLEditor.ReadColumn(TSEtable.Name.ToString(), TSEtable.Columns[j].Name.ToString(), wheres);

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