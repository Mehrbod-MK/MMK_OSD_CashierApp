﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MMK_OSD_CashierApp.Models;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using Org.BouncyCastle.Asn1.Mozilla;
using Org.BouncyCastle.Security.Certificates;

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

        public const string DB_TABLE_NAME_USERS = @"users";
        public const string DB_TABLE_NAME_PRODUCTS = @"products";
        public const string DB_TABLE_NAME_PURCHASES = @"purchases";
        public const string DB_TABLE_NAME_PARAMETERS = @"parameters";

        public const string DB_QUERY_USER_OK = @"DB_QUERY_USER_OK";
        public const string DB_QUERY_ERROR_USER_BAD_CREDENTIALS = @"DB_QUERY_ERROR_USER_BAD_CREDENTIALS";
        public const string DB_QUERY_ERROR_RESTRICTED_ACCESS = @"DB_QUERY_ERROR_RESTRICTED_ACCESS";

        public const string DB_PARAMETER_MAX_DISCOUNT_PERCENT = @"MAX_DISCOUNT_PERCENT";

        #endregion

        #region DB_Private_Variables

        private string server = string.Empty;
        private int port = -1;
        private string schema = string.Empty;
        private string username = string.Empty;
        private SecureString? password = null;

        private MySqlConnection? lastConnection = null;

        #endregion

        #region DBMgr_Generics

        public static T? ConvertFromDBVal<T>(object obj)
        {
            if (obj == null || obj == DBNull.Value)
            {
                return default(T); // returns the default value for the type
            }
            else
            {
                return (T)obj;
            }
        }

        #endregion

        #region DB_Public_Enumerations

        public enum DBResultEnum
        {
            DB_UNKNOWN = -1,

            DB_OK,
            DB_ERROR,

            DB_NOTIFY_NO_DBADMIN_USER,

            DB_ROLLBACKED_TRANSACTION,
        }

        [Flags]
        public enum DB_Roles : uint
        {
            DB_ROLE_Unknown = 0,

            DB_ROLE_Customer =      1 << 0,
            DB_ROLE_Cashier =       1 << 1,
            DB_ROLE_FundManager =   1 << 2,
            DB_ROLE_InventoryManager =  1 << 3,
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

        #region DB_Public_Methods

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
        public async Task<DBResult> sql_TestRootConnection(SecureString? knownRootPassword = null)
        {
            try
            {
                using (var connection = new MySqlConnection(get_ConnectionString(isRootConnection: true, 
                    rootPassword: knownRootPassword,
                    noSchema: true)))
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

        public async Task<DBResult> sql_Execute_NonQuery(string nonQueryCommand)
        {
            try
            {
                int rowsAffected = -1;

                using (MySqlConnection connection = new MySqlConnection(get_RecentConnectionString()))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    using (var executeTransaction = await connection.BeginTransactionAsync())
                    {
                        executeTransaction.ConfigureAwait(false);

                        using (MySqlCommand command = new(nonQueryCommand, connection, executeTransaction))
                        {
                            command.ConfigureAwait(false);
                            rowsAffected = await command.ExecuteNonQueryAsync();

                            try
                            {
                                await executeTransaction.CommitAsync();
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    await executeTransaction.RollbackAsync();

                                    return new DBResult()
                                    {
                                        result = DBResultEnum.DB_ROLLBACKED_TRANSACTION,
                                        returnValue = null,
                                    };
                                }
                                catch (Exception ex)
                                {
                                    return new DBResult()
                                    {
                                        result = DBResultEnum.DB_ERROR,
                                        returnValue = ex
                                    };
                                }
                            }
                        }
                    }
                }

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = rowsAffected,
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

        public async Task<DBResult> sql_End_Query(MySqlDataReader? dataReaderToClose)
        {
            try
            {
                dataReaderToClose?.ConfigureAwait(false);

                // Close and dispose query.
                if(dataReaderToClose != null)
                {
                    await dataReaderToClose.CloseAsync();
                    await dataReaderToClose.DisposeAsync();
                }

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

        public async Task<DBResult> db_Get_Product(uint productCode)
        {
            try
            {
                MySqlDataReader? dbResult_QueryProduct = 
                _THROW_DBRESULT<MySqlDataReader?>(await sql_Execute_Query($"SELECT * FROM  {schema}.{DB_TABLE_NAME_PRODUCTS} WHERE ProductID = {productCode};"));

                Product? product = null;

                if (dbResult_QueryProduct != null && dbResult_QueryProduct.HasRows)
                {
                    await dbResult_QueryProduct.ReadAsync();
                    product = new Product()
                    {
                        ProductID = (uint)dbResult_QueryProduct["ProductID"],
                        ProductName = (string)dbResult_QueryProduct["ProductName"],
                        Price = (ulong)dbResult_QueryProduct["Price"],
                        Vendor = ConvertFromDBVal<string?>(dbResult_QueryProduct["Vendor"]),
                        DateTimeSubmitted = dbResult_QueryProduct.GetDateTime("DateSubmitted"),
                        Quantity = (uint)dbResult_QueryProduct["Quantity"],
                    };

                    // Set thumbnail image.
                    string? thumbImagePath = ConvertFromDBVal<string?>(dbResult_QueryProduct["ThumbImagePath"]);
                    if(thumbImagePath != null)
                    {
                        product.ThumbImagePath = Path.Combine(Environment.CurrentDirectory, thumbImagePath);
                    }
                    else
                    {
                        product.ThumbImagePath = "../Resources/productIcon.png";
                    }
                }

                await sql_End_Query(dbResult_QueryProduct);

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = product,
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

        public async Task<DBResult> db_Get_User(string nationalID)
        {
            try
            {
                MySqlDataReader? dbResult_QueryUser =
                _THROW_DBRESULT<MySqlDataReader?>(await sql_Execute_Query($"SELECT * FROM  {schema}.{DB_TABLE_NAME_USERS} WHERE NationalID = \'{nationalID}\';"));

                User? user = null;

                if (dbResult_QueryUser != null && dbResult_QueryUser.HasRows)
                {
                    await dbResult_QueryUser.ReadAsync();
                    user = new User()
                    {
                        NationalID = (string)dbResult_QueryUser["NationalID"],
                        LoginPassword = (string)dbResult_QueryUser["LoginPassword"],
                        OptionalUserName = ConvertFromDBVal<string?>(dbResult_QueryUser["OptionalUsername"]),
                        FirstName = ConvertFromDBVal<string?>(dbResult_QueryUser["FirstName"]),
                        LastName = ConvertFromDBVal<string?>(dbResult_QueryUser["LastName"]),
                        RoleFlags = (uint)dbResult_QueryUser["RoleFlags"],
                        Email = ConvertFromDBVal<string?>(dbResult_QueryUser["Email"]),
                        RegisterDateTime = dbResult_QueryUser.GetDateTime("RegisterDateTime"),
                        LastLoginDateTime = (await dbResult_QueryUser.IsDBNullAsync("LastLoginDateTime")) 
                                        ? null 
                                        : dbResult_QueryUser.GetDateTime("LastLoginDateTime"),
                    };
                }

                await sql_End_Query(dbResult_QueryUser);

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = user,
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

        /// <summary>
        /// Checks if a user with a specific NationalID exists in DB.
        /// </summary>
        /// <param name="nationalID">User's national ID (Primay Key).</param>
        /// <returns></returns>
        /// <exception cref="MySqlException">Occurs when a DB Error happens.</exception>
        protected async Task<DBResult> db_Check_User_Exists(string nationalID, DB_Roles rolesToCheck)
        {
            var queryUserResult = _THROW_DBRESULT<User?>(
                await db_Get_User(nationalID)
                );

            if (queryUserResult == null)
                return new DBResult() { result = DBResultEnum.DB_OK, returnValue = null };

            if((queryUserResult.RoleFlags |= (uint)rolesToCheck) == 0)
            {
                return new DBResult() { result = DBResultEnum.DB_OK, returnValue = null };
            }

            return new DBResult()
            {
                result = DBResultEnum.DB_OK,
                returnValue = queryUserResult
            };
        }

        public DBResult db_Register_User(string nationalID, DB_Roles desiredRoles)
        {
            try
            {
                var query_CheckUserExists = _THROW_DBRESULT<User?>
                    (Task.Run(() => db_Check_User_Exists(nationalID, desiredRoles)).Result);

                // User was found, return a null user as a result.
                if(query_CheckUserExists != null)
                {
                    return new DBResult()
                    {
                        result = DBResultEnum.DB_OK,
                        returnValue = null
                    };
                }

                // User was not found, create a new one.
                string hashedPswrd = Hash(nationalID);
                DateTime dtNow = DateTime.Now;
                var writeQuery_RegisterUser = _THROW_DBRESULT<int>
                    (Task.Run(() => sql_Execute_NonQuery($"" +
                    $"INSERT INTO {schema}.{DB_TABLE_NAME_USERS} (NationalID, LoginPassword, RoleFlags, RegisterDateTime) VALUES" +
                    $"(\'{nationalID}\', \'{hashedPswrd}\', {(uint)DB_Roles.DB_ROLE_Customer}, \'{Convert_FromDateTime_ToSQLDateTimeString(dtNow)}\');")).Result);

                // Return the newly submitted user object.
                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = new User() { NationalID = nationalID, LoginPassword = hashedPswrd, RoleFlags = (uint)DB_Roles.DB_ROLE_Customer }
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

        public async Task<DBResult> db_Get_Products(string conditionalQuery)
        {
            try
            {
                MySqlDataReader? dbResult_QueryProduct =
                _THROW_DBRESULT<MySqlDataReader?>(await sql_Execute_Query(conditionalQuery));

                List<Product> foundProducts = new List<Product>();

                if (dbResult_QueryProduct != null && dbResult_QueryProduct.HasRows)
                {
                    while (await dbResult_QueryProduct.ReadAsync())
                    {
                        Product product;

                        product = new Product()
                        {
                            ProductID = (uint)dbResult_QueryProduct["ProductID"],
                            ProductName = (string)dbResult_QueryProduct["ProductName"],
                            Price = (ulong)dbResult_QueryProduct["Price"],
                            Vendor = ConvertFromDBVal<string?>(dbResult_QueryProduct["Vendor"]),
                            DateTimeSubmitted = dbResult_QueryProduct.GetDateTime("DateSubmitted"),
                            Quantity = (uint)dbResult_QueryProduct["Quantity"],
                        };

                        // Set thumbnail image.
                        string? thumbImagePath = ConvertFromDBVal<string?>(dbResult_QueryProduct["ThumbImagePath"]);
                        if (thumbImagePath != null)
                        {
                            product.ThumbImagePath = Path.Combine(Environment.CurrentDirectory, thumbImagePath);
                        }
                        else
                        {
                            // product.ThumbImagePath = "../Resources/productIcon.png";
                            product.ThumbImagePath = string.Empty;
                        }

                        foundProducts.Add(product);
                    }
                }

                await sql_End_Query(dbResult_QueryProduct);

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = foundProducts,
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

        public async Task<DBResult> db_Delete_Products(List<Product> productsToDelete)
        {
            try
            {
                using(var connection = new MySqlConnection(get_RecentConnectionString()))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    var transaction = await connection.BeginTransactionAsync();
                    transaction.ConfigureAwait(false);

                    try
                    {
                        foreach (var product in productsToDelete)
                        {
                            using (var command = new MySqlCommand($"DELETE FROM {DB_TABLE_NAME_PRODUCTS} WHERE ProductID = {product.ProductID};", connection, transaction))
                            {
                                MessageBox.Show(command.CommandText);
                                command.ConfigureAwait(false);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await transaction.CommitAsync();
                    }
                    catch(Exception ex)
                    {
                        await transaction.RollbackAsync();

                        return new DBResult()
                        {
                            result = DBResultEnum.DB_ROLLBACKED_TRANSACTION,
                            returnValue = ex,
                        };
                    }
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

        public async Task<DBResult> db_Update_Products(List<Product> productsToUpdate)
        {
            try
            {
                using (var connection = new MySqlConnection(get_RecentConnectionString()))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    var transaction = await connection.BeginTransactionAsync();
                    transaction.ConfigureAwait(false);

                    try
                    {
                        foreach (var product in productsToUpdate)
                        {
                            using (var command = new MySqlCommand($"UPDATE {DB_TABLE_NAME_PRODUCTS} " +
                                $"SET " +
                                $"ProductID = {product.ProductID}," +
                                $"ProductName = \'{product.ProductName}\'," +
                                $"Price = {product.Price}," +
                                $"Vendor = \'{product.Vendor}\'," +
                                $"DateSubmitted = \'{Convert_FromDateTime_ToSQLDateTimeString(product.DateTimeSubmitted)}\'," +
                                $"Quantity = \'{product.Quantity}\' " +
                                // $"ThumbImagePath = {(string.IsNullOrEmpty(product.ThumbImagePath) ? "NULL" : $"\'{Path.Combine(product.ThumbImagePath)}\'")} " +
                                $"WHERE ProductID = {product.ProductID};", connection, transaction))
                            {
                                // MessageBox.Show(command.CommandText);
                                command.ConfigureAwait(false);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        return new DBResult()
                        {
                            result = DBResultEnum.DB_ROLLBACKED_TRANSACTION,
                            returnValue = ex,
                        };
                    }
                }

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = true,
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

        public DBResult db_Set_Parameter_Float_SYNC(string param, float? value)
        {
            try
            {
                using(var connection = new MySqlConnection(get_RecentConnectionString()))
                {
                    connection.Open();

                    var transaction = connection.BeginTransaction();

                    // Check if parameter exists first.
                    var dbResult_getCommand = db_Get_Parameter_Float_SYNC(param);
                    if(dbResult_getCommand.result == DBResultEnum.DB_OK)
                    {
                        // Parameter existed, just update it.
                        using(var command = new MySqlCommand($"UPDATE {DB_TABLE_NAME_PARAMETERS} SET Value = \'{(value == null ? "NULL" : value)}\' WHERE Parameter = \'{param}\'", connection))
                        {
                            command.ExecuteNonQuery();

                            try
                            {
                                transaction.Commit();
                            }
                            catch(Exception ex)
                            {
                                transaction.Rollback();

                                return new DBResult()
                                {
                                    result = DBResultEnum.DB_ROLLBACKED_TRANSACTION,
                                    returnValue = ex,
                                };
                            }
                        }
                    }
                    else
                    {
                        // Command not existed, add a new record.
                        using (var command = new MySqlCommand($"INSERT INTO {DB_TABLE_NAME_PARAMETERS} VALUES (\'{param}\', \'{(value == null ? "NULL" : value)}\');"))
                        {
                            command.ExecuteNonQuery();

                            try
                            {
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();

                                return new DBResult()
                                {
                                    result = DBResultEnum.DB_ROLLBACKED_TRANSACTION,
                                    returnValue = ex,
                                };
                            }
                        }
                    }
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

        public DBResult db_Get_Parameter_Float_SYNC(string param)
        {
            float? value = null;

            try
            {
                using (var connection = new MySqlConnection(get_RecentConnectionString()))
                {
                    connection.Open();

                    using(var command = new MySqlCommand($"SELECT Value FROM {DB_TABLE_NAME_PARAMETERS} WHERE Parameter = \'{param}\';", connection))
                    {
                        var reader = command.ExecuteReader();

                        if (!reader.HasRows)
                            throw new ArgumentException($"پارامتر {param} در پایگاه داده یافت نشد!");

                        reader.Read();

                        if (reader.IsDBNull("Value"))
                            value = null;
                        else
                            value = float.Parse((string)reader["Value"]);
                    }
                }

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = value
                };
            }
            catch (Exception ex)
            {
                return new DBResult()
                {
                    result = DBResultEnum.DB_ERROR,
                    returnValue = ex
                };
            }
        }

        public DBResult db_Get_MostRecent_Purchase_Payment_SYNC(string customer_NationalID)
        {
            ulong? payment = null;

            try
            {
                using(var connection = new MySqlConnection(get_RecentConnectionString()))
                {
                    connection.Open();

                    using (var command = new MySqlCommand($"" +
                        $"SELECT * FROM {DB_TABLE_NAME_PURCHASES} " +
                        $"WHERE Customer_NationalID = \'{customer_NationalID}\' " +
                        $"ORDER BY DateTimeSubmitted DESC " +
                        $"LIMIT 1", connection))
                    {
                        var reader = command.ExecuteReader();

                        if (!reader.HasRows)
                            payment = null;
                        else
                        {
                            reader.Read();
                            payment = (ulong)reader["Total_Payment"];
                        }
                    }
                }

                return new DBResult()
                {
                    result = DBResultEnum.DB_OK,
                    returnValue = payment,
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

        public async Task db_Initialize()
        {
            // Create MMK_OSD_CashierApp database.
            await sql_Execute_NonQuery($"CREATE DATABASE {schema};");

            // Create Table: Users
            await sql_Execute_NonQuery($"CREATE TABLE {schema}.{DB_TABLE_NAME_USERS}(" +
                $"NationalID VARCHAR(100) PRIMARY KEY NOT NULL," +
                $"LoginPassword VARCHAR(1024) NOT NULL," +
                $"OptionalUsername VARCHAR(10)," +
                $"FirstName VARCHAR(255)," +
                $"LastName VARCHAR(255)," +
                $"RoleFlags INTEGER UNSIGNED NOT NULL," +
                $"Email VARCHAR(100)," +
                $"RegisterDateTime DATETIME NOT NULL," +
                $"LastLoginDateTime DATETIME" +
                $");");

            // Create Table: Products
            await sql_Execute_NonQuery($"CREATE TABLE {schema}.{DB_TABLE_NAME_PRODUCTS}(" +
                $"ProductID INTEGER UNSIGNED PRIMARY KEY NOT NULL AUTO_INCREMENT," +
                $"ProductName VARCHAR(255) NOT NULL," +
                $"Price BIGINT NOT NULL," +
                $"Vendor VARCHAR(100)," +
                $"DateSubmitted DATETIME NOT NULL," +
                $"Quantity INTEGER UNSIGNED NOT NULL," +
                $"ThumbImagePath VARCHAR(255)" +
                $");");

            // Create Table: Purchases
            await sql_Execute_NonQuery($"CREATE TABLE {schema}.{DB_TABLE_NAME_PURCHASES}(" +
                $"PurchaseID INTEGER UNSIGNED PRIMARY KEY NOT NULL AUTO_INCREMENT," +
                $"Customer_NationalID VARCHAR(100) NOT NULL," +
                $"DateTimeSubmitted DATETIME NOT NULL," +
                $"SubmittedBy_NationalID VARCHAR(100) NOT NULL," +
                $"Num_ProductsPurchased INTEGER UNSIGNED NOT NULL," +
                $"Total_Price BIGINT UNSIGNED NOT NULL," +
                $"Total_Discount BIGINT UNSIGNED NOT NULL," +
                $"Total_Payment BIGINT UNSIGNED NOT NULL," +
                $"FOREIGN KEY(Customer_NationalID) REFERENCES {DB.DB_TABLE_NAME_USERS}(NationalID) ON UPDATE CASCADE ON DELETE CASCADE," +
                $"FOREIGN KEY(SubmittedBy_NationalID) REFERENCES {DB.DB_TABLE_NAME_USERS}(NationalID) ON UPDATE CASCADE ON DELETE CASCADE," +
                $");");

            // Create Table: Paramters
            await sql_Execute_NonQuery($"CREATE TABLE {schema}.{DB_TABLE_NAME_PARAMETERS}(" +
                $"Parameter VARCHAR(100) PRIMARY KEY NOT NULL," +
                $"Value VARCHAR(100)" +
                $");");

            // Assign default parameters and values.
            await sql_Execute_NonQuery($"INSERT INTO {schema}.{DB_TABLE_NAME_PARAMETERS} VALUES" +
                $"(\'{DB_PARAMETER_MAX_DISCOUNT_PERCENT}\', 10);");
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

        public static string Convert_FromDateTime_ToSQLDateTimeString(DateTime dt)
            => $"{dt.Year:0000}-{dt.Month:00}-{dt.Day:00} {dt.Hour:00}:{dt.Minute:00}:{dt.Second:00}";

        public static T? _THROW_DBRESULT<T>(DBResult dBResult)
        {
            try
            {
                if (dBResult.returnValue == null)
                    return default;
                    // throw new NullReferenceException("خطای بازگشت نتیجه از پایگاه داده. لطفاً با طراح سامانه تماس حاصل فرمایید.");

                if (dBResult.result == DBResultEnum.DB_ERROR)
                    throw (Exception)dBResult.returnValue;

                return (T?)dBResult.returnValue;
            }
            catch(Exception) 
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
            bool isRootConnection = false,
            bool noSchema = false)
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
                        $"{(noSchema ? string.Empty : $"DATABASE={schema};")}" +
                        $"UID={username};" +
                        $"PASSWORD={SecureStringToString(password)};";
                else
                    return
                        $"SERVER={server};" +
                        $"PORT={port};" +
                        $"{(noSchema ? string.Empty : $"DATABASE={schema};")}" +
                        $"UID={username};";
            }
            else
            {
                if (rootPassword == null)
                {
                    this.server = server;
                    this.port = port;
                    this.username = DB_ROOT_USER;
                    this.schema = schema;

                    return
                    $"SERVER={server};" +
                    $"PORT={port};" +
                    $"{(noSchema ? string.Empty : $"DATABASE={schema};")}" +
                    $"UID={DB_ROOT_USER}";
                }
                else
                {
                    this.server = server;
                    this.port = port;
                    this.schema = schema;
                    this.username = DB_ROOT_USER;
                    this.password = rootPassword;

                    return
                       $"SERVER={server};" +
                       $"PORT={port};" +
                       $"UID={DB_ROOT_USER};" +
                       $"{(noSchema ? string.Empty : $"DATABASE={schema};")}" +
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
