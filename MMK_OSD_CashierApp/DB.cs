using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace MMK_OSD_CashierApp
{
    public class DB
    {
        #region DB_Public_Constants

        public const string DB_DEFAULT_SERVER = "localhost";
        public const int DB_DEFAULT_PORT = 3306;
        public const string DB_DEFAULT_APP_SCHEMA = "mmk_osd_cashierapp";
        public const string DB_ROOT_USER = "root";
        public const string DB_DEFAULT_USERNAME = "MehrbodMK";

        #endregion

        #region DB_Private_Variables

        private string server = string.Empty;
        private int port = -1;
        private string schema = string.Empty;
        private string username = string.Empty, password = string.Empty;

        #endregion

        #region DB_Public_Methods

        #region DB_Public_Enumerations

        public enum DBResultEnum
        {
            DB_UNKNOWN = -1,

            DB_OK,
            DB_ERROR,

            DB_NOTIFY_NO_DBADMIN_USER,
        }

        #endregion

        #region DB_Public_Structures

        public class DBResult
        {
            public DBResultEnum result = DBResultEnum.DB_UNKNOWN;
            public object? returnValue = -1;
        }

        #endregion

        /// <summary>
        /// Creates an initial instance of DB class.
        /// </summary>
        public DB(string server = DB_DEFAULT_SERVER, int port = DB_DEFAULT_PORT)
        {
            this.server = server;
            this.port = port;
        }

        /// <summary>
        /// Tests a password-less connection to MySQL's ROOT user for application to use.
        /// </summary>
        /// <returns>This task returns a <see cref="DBResult"/> structure.</returns>
        public async Task<DBResult> sql_TestRootConnection()
        {
            try
            {
                using (var connection = new MySqlConnection(get_ConnectionString(isRootConnection: true)))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();
                }

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = true,
                };
            }
            catch(Exception ex)
            {
                return new DBResult()
                {
                    result = DBResultEnum.DB_ERROR,
                    returnValue = ex,
                };
            }
        }

        public async Task<DBResult> sql_Execute_Scalar<S>(string sql_ConnectionString,
            string scalarCommand)
        {
            try
            {
                S? resultScalar = default(S);

                using (var connection = new MySqlConnection(sql_ConnectionString))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    using (MySqlCommand command = new MySqlCommand(scalarCommand, connection))
                    {
                        command.ConfigureAwait(false);

                        resultScalar = (S?)await command.ExecuteScalarAsync();
                    }
                }

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = resultScalar,
                };
            }
            catch(Exception ex)
            {
                return new DBResult()
                {
                    result = DBResultEnum.DB_ERROR,
                    returnValue = ex,
                };
            }
        }

        #endregion

        #region DB_Private_Methods

        private string get_ConnectionString(string server = DB_DEFAULT_SERVER, int port = DB_DEFAULT_PORT,
            string schema = DB_DEFAULT_APP_SCHEMA,
            string username = DB_DEFAULT_USERNAME, string password = "",
            bool isRootConnection = false)
        {
            if (!isRootConnection)
                return
                    $"SERVER={server};" +
                    $"PORT={port};" +
                    $"DATABASE={schema};" +
                    $"UID={username};" +
                    $"PASSWORD={password};";
            else
                return
                    $"SERVER={server};" +
                    $"PORT={port};" +
                    $"UID={DB_ROOT_USER}";
        }

        private async Task<DBResult> establish_SQLConnection(
            )
        {
            try
            {

                var sqlConnection = new MySqlConnection();

                // Open the connection.
                sqlConnection.ConfigureAwait(false);
                await sqlConnection.OpenAsync();

                return new DBResult
                {
                    returnValue = sqlConnection,
                    result = DBResultEnum.DB_OK,
                };
            }
            catch(Exception ex)
            {
                return new DBResult
                {
                    returnValue = ex,
                    result = DBResultEnum.DB_ERROR,
                };
            }
        }

        #endregion
    }
}
