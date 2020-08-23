﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BankApplicationsWinForm.Services
{
    public static class DataBaseService
    {
        // получаем строку подключения
        static string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        /// <summary>
        /// Создать БД если она отсутствует
        /// </summary>
        /// <param name="script"></param>
        /// <param name="conStr"></param>
        /// <returns></returns>
        public static bool CheckCreateDB(string conStr)
        {
            using (SqlConnection connection = new SqlConnection(conStr))
            {
                var dt = ExecSelect($"SELECT * FROM sys.databases", "name = @name", "name", "BankApp", "sys.databases", conStr);

                if (dt.Rows.Count > 0)
                {
                    Service.LogWrite("БД BankApp найдена!");
                    return true;
                }
                else Service.LogWrite("БД BankApp не сущесвует!");

                // TODO: Сделать проверку на наличие папки создания ДБ - папки нету - ОШИБКА! 
                var str = "CREATE DATABASE [BankApp] CONTAINMENT = NONE  ON PRIMARY " +
                    "(NAME = N'BankApp', " +
                    "FILENAME = 'C:\\SQL INSTAL\\Microsoft SQL Server\\MSSQL12.SQLEXPRESS\\MSSQL\\DATA\\BankApp.mdf' , " +
                    "SIZE = 29888KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB ) " +
                    "LOG ON (NAME = N'BankApp_log', FILENAME = 'C:\\SQL INSTAL\\Microsoft SQL Server\\MSSQL12.SQLEXPRESS\\MSSQL\\DATA\\BankApp_log.ldf' , " +
                    "SIZE = 48384KB , " +
                    "MAXSIZE = 2048GB , " +
                    "FILEGROWTH = 10 %)";

                connection.Open();

                SqlCommand command = new SqlCommand(str, connection);

                command.ExecuteNonQuery();

                dt = ExecSelect($"SELECT * FROM sys.databases", "name = @name", "name", "BankApp", "sys.databases", conStr);

                if (dt.Rows.Count > 0)
                {
                    Service.LogWrite("БД BankApp успешно создана");
                    connection.Close();

                    SqlConnection con = new SqlConnection(connectionString);
                    string query =
                    @"CREATE TABLE [dbo].[tbUsers](
	                        [User_ID] [int] IDENTITY(1,1) NOT NULL,
	                        [Gender] [bit] NULL,
	                        [DateOfBirth] [datetime] NULL,
	                        [Password] [varchar](max) NOT NULL,
	                        [Login] [varchar](50) NOT NULL,
	                        [DemandData] [varchar](max) NULL,
	                        [DepositData] [varchar](max) NULL,
	                        [FName] [varchar](50) NOT NULL DEFAULT (''),
	                        [LName] [varchar](50) NOT NULL DEFAULT (''),
                        PRIMARY KEY CLUSTERED 
                        (
	                        [User_ID] ASC
                        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

                        CREATE TABLE [dbo].[tbFiles](
	                        [ID] [int] IDENTITY(1,1) NOT NULL,
	                        [User_ID] [int] NOT NULL,
	                        [Image] [varbinary](max) NULL,
                        PRIMARY KEY CLUSTERED 
                        (
	                        [ID] ASC
                        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

                        ALTER TABLE [dbo].[tbFiles]  WITH CHECK ADD  CONSTRAINT [FK_tbFiles_tbUsers] FOREIGN KEY([User_ID])
                        REFERENCES [dbo].[tbUsers] ([User_ID])";

                    SqlCommand cmd = new SqlCommand(query, con);
                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Service.LogWrite("Ошибка создания таблиц в БД BankApp - " + e.Message);
                    }
                    finally
                    {
                        con.Close();
                    }
                    Service.LogWrite("БД BankApp заполнена таблицами, всё ОК!");
                    return true;
                }
                else
                {
                    Service.LogWrite("Ошибка создания БД!");
                    return false;
                }
            }
        }

        public static DataTable ExecSelect(string sqlSelect, string sqlConditions, string sqlParam, string nameParam, string nameTable, string castumConnectionString)
        {
            SqlConnection connection = new SqlConnection(castumConnectionString);
            SqlDataAdapter da;

            DataSet tempDataset = new DataSet("temp");
            try
            {
                connection.Open();
                da = new SqlDataAdapter();

                string sqlExpression = sqlSelect + $" WHERE {sqlConditions}";

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.Parameters.AddWithValue(sqlParam, nameParam);   // добавление параметра в коллекцию параметров команды
                da.SelectCommand = command;

                da.Fill(tempDataset, nameTable);
            }
            catch (Exception e)
            {
                Service.LogWrite($"Нет доступа к данным! Проверьте настройки! : {e.ToString()}");
                return null;
            }
            finally
            {
                connection.Close();
            }
            return tempDataset.Tables[nameTable];

        }

        /// <summary>
        /// Обновить данные в базе
        /// </summary>
        /// <param name="sqlConditions">условие выборки</param>
        /// <param name="sqlParam">"Login"</param>
        /// <param name="nameParam">"Евгений"</param>
        /// <param name="nameTable">Имя таблицы "tbUsers"</param>
        /// <param name="data">Данные для сохранения</param>
        /// <param name="userId"></param>
        /// <param name="nameColumn">Имя столбца</param>
        /// <returns></returns>
        public static bool ExecUpdate(string sqlConditions, string sqlParam, string nameParam, string nameTable, string setConditions)
        {
            var dt = ExecSelect($"SELECT * FROM {nameTable}", sqlConditions, sqlParam, nameParam, nameTable);

            if (dt.Rows.Count != 0)
            {
                string sqlExpression = $"UPDATE {nameTable} {setConditions} WHERE User_ID = {nameParam}";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(sqlExpression, connection);

                    int rowAffected = command.ExecuteNonQuery();

                    if (rowAffected > 0)
                        return true;
                }

                return true;
            }
            else
            {
                //string sqlExpression = $"INSERT tbUsers VALUES ('{FIO}', 'true', NULL, '{password}', '{login}')";
                Service.LogWrite($"Отсутствует данный пользователь");
                MessageBox.Show($"Отсутствует данный пользователь!", "Ошибка входа");
                return false;
            }

        }

        public static bool ExecInsert(string sqlExpression)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sqlExpression, connection);

                int rowAffected = command.ExecuteNonQuery();
                if (rowAffected > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Получить данные из базы
        /// </summary>
        /// <param name="sqlSelect">Выражения выборки</param>
        /// <param name="sqlConditions">Условия выборки</param>
        /// <param name="sqlParam"></param>
        /// <param name="nameParam"></param>
        /// <param name="nameTable"></param>
        /// <returns>Таблицу результаа запроса</returns>
        public static DataTable ExecSelect(string sqlSelect, string sqlConditions, string sqlParam, string nameParam, string nameTable)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            SqlDataAdapter da;

            DataSet tempDataset = new DataSet("temp");
            try
            {
                connection.Open();
                da = new SqlDataAdapter();

                string sqlExpression = sqlSelect + $" WHERE {sqlConditions}";

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.Parameters.AddWithValue(sqlParam, nameParam);   // добавление параметра в коллекцию параметров команды
                da.SelectCommand = command;

                da.Fill(tempDataset, nameTable);
            }
            catch (Exception e)
            {
                Service.LogWrite($"Нет доступа к данным! Проверьте настройки! : {e.ToString()}");
                return null;
            }
            finally
            {
                connection.Close();
            }
            return tempDataset.Tables[nameTable];

            #region OldRealization
            //SqlDataReader reader = command.ExecuteReader();

            //if (reader.HasRows) // если есть данные
            //{
            //    // выводим названия столбцов
            //    Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", reader.GetName(0), reader.GetName(1), reader.GetName(2), reader.GetName(3), reader.GetName(4));

            //    while (reader.Read()) // построчно считываем данные
            //    {
            //        object id = reader.GetValue(0);
            //        object FIO = reader.GetValue(1);
            //        object genderge = reader.GetValue(2);
            //        object dateOfBirthd = reader.GetValue(3);
            //        login = reader.GetValue(4);
            //    }
            //}
            //reader.Close(); 
            #endregion
        }

        public static DataTable ExecSelect(string sqlSelect, string nameTable)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            SqlDataAdapter da;

            DataSet tempDataset = new DataSet("temp");
            try
            {
                connection.Open();
                da = new SqlDataAdapter();

                string sqlExpression = sqlSelect;

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                da.SelectCommand = command;
                da.Fill(tempDataset, nameTable);
            }
            catch (Exception e)
            {
                Service.LogWrite($"Нет доступа к данным! Проверьте настройки! : {e.ToString()}");
                return null;
            }
            finally
            {
                connection.Close();
            }
            return tempDataset.Tables[nameTable];
        }
    }
}
