using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;

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

        public const string DB_TABLE_NAME_USERS = @"mmk_osd_cashierapp.users";

        public const string DB_QUERY_ERROR_USER_BAD_CREDENTIALS = @"DB_QUERY_ERROR_USER_BAD_CREDENTIALS";

        #endregion

        #region DB_Private_Variables

        private string server = string.Empty;
        private int port = -1;
        private string schema = string.Empty;
        private string username = string.Empty;
        private SecureString? password = null;

        private MySqlConnection? lastConnection = null;

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

        [Flags]
        public enum DB_Roles : uint
        {
            DB_ROLE_Unknown = 0,

            DB_ROLE_Customer =      1 << 0,
            DB_ROLE_Cashier =       1 << 1,
            DB_ROLE_FundManager =   1 << 2,
            DB_ROLE_StoreManager =  1 << 3,
            DB_ROLE_DBA =           1 << 4,
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
        public async Task<DBResult> sql_TestRootConnection(string? knownRootPassword = null)
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

        public async Task<DBResult> sql_Execute_Scalar<S>(string scalarCommand)
        {
            try
            {
                S? resultScalar = default(S);

                using (var connection = new MySqlConnection(get_RecentConnectionString()))
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

        public async Task<DBResult> sql_Execute_Query(string sqlQueryCommand)
        {
            try
            {
                MySqlDataReader? resultReader = null;

                lastConnection = new MySqlConnection(get_RecentConnectionString());

                // using (var connection = new MySqlConnection(get_RecentConnectionString()))
                // {
                    lastConnection.ConfigureAwait(false);
                    await lastConnection.OpenAsync();

                    using (MySqlCommand command = new MySqlCommand(sqlQueryCommand, lastConnection))
                    {
                        command.ConfigureAwait(false);

                        resultReader = command.ExecuteReader();
                        resultReader.ConfigureAwait(false);
                    }
                // }

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = resultReader,
                };
            }
            catch (Exception ex)
            {
                return new DBResult()
                {
                    result = DBResultEnum.DB_ERROR,
                    returnValue = ex,
                };
            }
        }

        public async Task<DBResult> sql_End_Query(MySqlDataReader dataReaderToClose)
        {
            try
            {
                dataReaderToClose.ConfigureAwait(false);

                // Close and dispose query.
                await dataReaderToClose.CloseAsync();
                await dataReaderToClose.DisposeAsync();

                // Close and dispoase recent connection.
                await this.sql_End_Connection(this.lastConnection);

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

        public static string SecureStringToString(SecureString value)
        {
            IntPtr bstr = Marshal.SecureStringToBSTR(value);

            try
            {
                return Marshal.PtrToStringBSTR(bstr);
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        public static string Hash(string input)
            => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(input)));

        public static object _THROW_DBRESULT(DBResult dBResult)
        {
            try
            {
                if (dBResult.returnValue == null)
                    throw new NullReferenceException("خطای بازگشت نتیجه از پایگاه داده. لطفاً با طراح سامانه تماس حاصل فرمایید.");

                if (dBResult.result == DBResultEnum.DB_ERROR)
                    throw (Exception)dBResult.returnValue;

                return (object)dBResult.returnValue;
            }
            catch(Exception ex) 
            {
                throw;
            }
        }

        #endregion

        #region DB_Private_Methods

        private string get_RecentConnectionString()
        {
            if (this.password == null)
            {
                return
                    $"SERVER={server};" +
                    $"PORT={port};" +
                    $"DATABASE={schema};" +
                    $"UID={username};";
            }
            else
            {
                return
                    $"SERVER={server};" +
                    $"PORT={port};" +
                    $"DATABASE={schema};" +
                    $"UID={username};" +
                    $"PASSWORD={SecureStringToString(password)};";
            }
        }

        private string get_ConnectionString(string server = DB_DEFAULT_SERVER, int port = DB_DEFAULT_PORT,
            string schema = DB_DEFAULT_APP_SCHEMA,
            string username = DB_DEFAULT_USERNAME, SecureString? password = null, SecureString? rootPassword = null,
            bool isRootConnection = false)
        {
            if (!isRootConnection)
            {
                this.server = server;
                this.port = port;
                this.schema = schema;
                this.username = username;
                this.password = password;

                if (password != null)
                    return
                        $"SERVER={server};" +
                        $"PORT={port};" +
                        $"DATABASE={schema};" +
                        $"UID={username};" +
                        $"PASSWORD={SecureStringToString(password)};";
                else
                    return
                        $"SERVER={server};" +
                        $"PORT={port};" +
                        $"DATABASE={schema};" +
                        $"UID={username};";
            }
            else
            {
                if (rootPassword == null)
                {
                    this.server = server;
                    this.port = port;
                    this.username = DB_ROOT_USER;

                    return
                    $"SERVER={server};" +
                    $"PORT={port};" +
                    $"UID={DB_ROOT_USER}";
                }
                else
                {
                    this.server = server;
                    this.port = port;
                    this.username = DB_ROOT_USER;
                    this.password = rootPassword;

                    return
                       $"SERVER={server};" +
                       $"PORT={port};" +
                       $"UID={DB_ROOT_USER}" +
                       $"PASSWORD={SecureStringToString(rootPassword)}";
                }
            }
        }

        private async Task<DBResult> establish_SQLConnection(
            )
        {
            try
            {
                var sqlConnection = new MySqlConnection(get_RecentConnectionString());

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

        private async Task<DBResult> sql_End_Connection(MySqlConnection? mySqlConnection)
        {
            try
            {
                if (mySqlConnection != null && !mySqlConnection.IsDisposed)
                {
                    mySqlConnection.ConfigureAwait(false);

                    await mySqlConnection.CloseAsync();
                    await mySqlConnection.DisposeAsync();
                }

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = true
                };
            }
            catch(Exception ex)
            {
                return new DBResult()
                {
                    result = DBResultEnum.DB_ERROR,
                    returnValue = ex
                };
            }
        }

        #endregion
    }
}
