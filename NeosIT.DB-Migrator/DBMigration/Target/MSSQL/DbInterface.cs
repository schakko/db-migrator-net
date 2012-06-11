﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NeosIT.DB_Migrator.DBMigration.Target.MSSQL
{
    public class FilterException : Exception
    {
    }

    public class DbInterface : IDbInterface
    {
        private const string SqlMajorCol = "major";
        private const string SqlMinorCol = "minor";

        private const string SqlLatestMigration =
            "SELECT TOP 1 " + SqlMajorCol + ", " + SqlMinorCol + " FROM migrations ORDER BY " + SqlMajorCol + " DESC, " +
            SqlMinorCol + " DESC";

        private const string SqlCreateMigration =
            "CREATE TABLE migrations(id INT NOT NULL PRIMARY KEY IDENTITY, installed_on DATETIME NOT NULL DEFAULT getDate(), " +
            SqlMajorCol + " char(8), " + SqlMinorCol + " char(8), filename nvarchar(max))";

        #region IDbInterface Members

        public IExecutor Executor { get; set; }

        public Version FindLatestMigration()
        {
            try
            {
                IList<string> lines = Executor.ExecCommand(SqlLatestMigration).Split(new[] {'\n',});
                string major = "0";
                string minor = "0";

                if (lines.Count >= 5)
                {
                    if (!Regex.Match(lines[0], @"\s+" + SqlMajorCol + @"\s+" + SqlMinorCol).Success)
                    {
                        throw new FilterException();
                    }

                    MatchCollection matches = Regex.Matches(lines[2], @"\s*(\d*)\s(\d+)");

                    if (2 == matches.Count)
                    {
                        major = matches[0].Value.Trim();
                        minor = matches[1].Value.Trim();
                    }
                }

                var r = new Version {Major = major, Minor = minor,};

                return r;
            }
            catch (Exception e)
            {
                Console.WriteLine("[error] could not retrieve latest revision from database: {0}", e.Message);

                if (e is FilterException)
                {
                    throw new Exception("Could not filter output");
                }

                Console.WriteLine("[create] trying to create migration table ...");

                try
                {
                    Executor.ExecCommand(SqlCreateMigration);
                    Console.WriteLine("[create] migrations table successfully created");

                    return FindLatestMigration();
                }
                catch (Exception eCreate)
                {
                    throw new Exception(string.Format("Could not create migrations table: {0}", eCreate.Message));
                }
            }
        }

        #endregion
    }
}